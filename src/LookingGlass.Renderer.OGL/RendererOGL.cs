/* Copyright (c) Robert Adams
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * The name of the copyright holder may not be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Text;
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using LookingGlass.Renderer;
using LookingGlass.World;
using LookingGlass.World.LL;
using OMV = OpenMetaverse;
using OMVR = OpenMetaverse.Rendering;
using OMVI = OpenMetaverse.Imaging;

namespace LookingGlass.Renderer.OGL {
    /// <summary>
    /// A renderer that renders straight to OpenGL
    /// </summary>
public class RendererOGL : IModule, IRenderProvider {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    public Camera Camera;
    public Dictionary<uint, OMV.Primitive> RenderFoliageList = new Dictionary<uint, OMV.Primitive>();
    public Dictionary<uint, RenderablePrim> RenderPrimList = new Dictionary<uint, RenderablePrim>();

    private Mesher.MeshmerizerR m_meshMaker = null;
    
    // Textures
    public Dictionary<OMV.UUID, TextureInfo> Textures = new Dictionary<OMV.UUID, TextureInfo>();

    //Terrain
    public float MaxHeight = 0.1f;
    public OMV.TerrainPatch[,] Heightmap;
    RegionContextBase m_focusRegion = null;

    public bool m_wireFrame = false;

    #region IModule
    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected LookingGlassBase m_lgb = null;
    public LookingGlassBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    public RendererOGL() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }

    // IModule.OnLoad
    public virtual void OnLoad(string modName, LookingGlassBase lgbase) {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;
        ModuleParams.AddDefaultParameter(m_moduleName + ".InputSystem.Name", "WindowUI",
                    "Name of the input module");
    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".AfterAllModulesLoaded()");
        // Load the input system we're supposed to be using
        // The input system is a module tha we get given the name of. Go find it and link it in.
        String uiModule = ModuleParams.ParamString(m_moduleName + ".InputSystem.Name");
        if (uiModule != null && uiModule.Length > 0) {
            try {
                m_log.Log(LogLevel.DRENDER, "Loading UI processor '{0}'", uiModule);
                m_userInterface = (IUserInterfaceProvider)ModuleManager.Instance.Module(uiModule);
                if (m_userInterface == null) {
                    m_log.Log(LogLevel.DBADERROR, "FATAL: Could not find user interface class {0}", uiModule);
                    return false;
                }
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "FATAL: Could not load user interface class {0}: {1}", uiModule, e.ToString());
                return false;
            }
        }
        else {
            m_log.Log(LogLevel.DBADERROR, "Using null user interfare");
            m_userInterface = new UserInterfaceNull();
        }

        return true;
    }

    // IModule.Start
    public virtual void Start() {
        return;
    }

    // IModule.Stop
    public virtual void Stop() {
        return;
    }

    // IModule.PrepareForUnload
    public virtual bool PrepareForUnload() {
        return false;
    }
    #endregion IModule

    #region IRenderProvider
    IUserInterfaceProvider m_userInterface = null;
    public IUserInterfaceProvider UserInterface { 
        get { return m_userInterface; } 
    }

    // entry for main thread for rendering. Return false if you don't need it.
    public bool RendererThread() {
        return false;
    }
    // entry for rendering one frame. An alternate to the above thread method
    public bool RenderOneFrame(bool pump, int len) {
        return true;
    }

    //=================================================================
    // Set the entity to be rendered
    public void Render(IEntity ent) {
        if (ent is LLEntityBase) {
            lock (ent) {
                CreateNewPrim((LLEntityBase)ent);
            }
        }
        return;
    }

    private void CreateNewPrim(LLEntityBase ent) {
        m_log.Log(LogLevel.DRENDERDETAIL, "Create new prim {0}", ent.Name.Name);
        OMV.Primitive prim = ent.Prim;
        if (prim.PrimData.PCode == OMV.PCode.Grass 
                    || prim.PrimData.PCode == OMV.PCode.Tree 
                    || prim.PrimData.PCode == OMV.PCode.NewTree) {
            lock (RenderFoliageList)
                RenderFoliageList[prim.LocalID] = prim;
            return;
        }

        RenderablePrim render = new RenderablePrim();
        render.Prim = prim;
        render.acontext = ent.AssetContext;

        if (m_meshMaker == null) {
            m_meshMaker = new Renderer.Mesher.MeshmerizerR();
            m_meshMaker.ShouldScaleMesh = false;
        }

        if (prim.Sculpt != null) {
            EntityNameLL textureEnt = EntityNameLL.ConvertTextureWorldIDToEntityName(ent.AssetContext, prim.Sculpt.SculptTexture);
            System.Drawing.Bitmap textureBitmap = ent.AssetContext.GetTexture(textureEnt);
            if (textureBitmap == null) {
                // the texture is not available. Request it.
                // Note that we just call this routine again when it is available. Hope it's not recursive
                ent.AssetContext.DoTextureLoad(textureEnt, AssetContextBase.AssetType.SculptieTexture, 
                            delegate(string name, bool trans) {
                                CreateNewPrim(ent);
                                return; 
                            }
                );
            }
            render.Mesh = m_meshMaker.GenerateSculptMesh(textureBitmap, prim, OMVR.DetailLevel.Medium);
            textureBitmap.Dispose();
        }
        else {
            render.Mesh = m_meshMaker.GenerateFacetedMesh(prim, OMVR.DetailLevel.High);
        }

        // Create a FaceData struct for each face that stores the 3D data
        // in a Tao.OpenGL friendly format
        for (int j = 0; j < render.Mesh.Faces.Count; j++) {
            OMVR.Face face = render.Mesh.Faces[j];
            FaceData data = new FaceData();

            // Vertices for this face
            data.Vertices = new float[face.Vertices.Count * 3];
            for (int k = 0; k < face.Vertices.Count; k++) {
                data.Vertices[k * 3 + 0] = face.Vertices[k].Position.X;
                data.Vertices[k * 3 + 1] = face.Vertices[k].Position.Y;
                data.Vertices[k * 3 + 2] = face.Vertices[k].Position.Z;
            }

            // Indices for this face
            data.Indices = face.Indices.ToArray();

            // Texture transform for this face
            OMV.Primitive.TextureEntryFace teFace = prim.Textures.GetFace((uint)j);
            m_meshMaker.TransformTexCoords(face.Vertices, face.Center, teFace);

            // Texcoords for this face
            data.TexCoords = new float[face.Vertices.Count * 2];
            for (int k = 0; k < face.Vertices.Count; k++) {
                data.TexCoords[k * 2 + 0] = face.Vertices[k].TexCoord.X;
                data.TexCoords[k * 2 + 1] = face.Vertices[k].TexCoord.Y;
            }

            data.Normals = new float[face.Vertices.Count * 3];
            for (int k = 0; k < face.Vertices.Count; k++) {
                data.Normals[k * 3 + 0] = face.Vertices[k].Normal.X;
                data.Normals[k * 3 + 1] = face.Vertices[k].Normal.Y;
                data.Normals[k * 3 + 2] = face.Vertices[k].Normal.Z;
            }


            // m_log.Log(LogLevel.DRENDERDETAIL, "CreateNewPrim: v={0}, i={1}, t={2}",
            //     data.Vertices.GetLength(0), data.Indices.GetLength(0), data.TexCoords.GetLength(0));

            // Texture for this face
            if (teFace.TextureID != OMV.UUID.Zero &&
                        teFace.TextureID != OMV.Primitive.TextureEntry.WHITE_TEXTURE) {
                lock (Textures) {
                    if (!Textures.ContainsKey(teFace.TextureID)) {
                        // temporarily add the entry to the table so we don't request it multiple times
                        Textures.Add(teFace.TextureID, new TextureInfo(0, true));
                        // We haven't constructed this image in OpenGL yet, get ahold of it
                        AssetContextBase.RequestTextureLoad(
                            EntityNameLL.ConvertTextureWorldIDToEntityName(ent.AssetContext, teFace.TextureID),
                            AssetContextBase.AssetType.Texture, 
                            OnTextureDownloadFinished);
                    }
                }
            }

            // Set the UserData for this face to our FaceData struct
            face.UserData = data;
            render.Mesh.Faces[j] = face;
        }

        lock (RenderPrimList) RenderPrimList[prim.LocalID] = render;
    }

    private void OnTextureDownloadFinished(string textureEntityName, bool hasTransparancy) {
        m_log.Log(LogLevel.DRENDERDETAIL, "OnTextureDownloadFinished {0}", textureEntityName);
        EntityName entName = new EntityName(textureEntityName);
        OMV.UUID id = new OMV.UUID(entName.ExtractEntityFromEntityName());

        TextureInfo info;
        lock (Textures) {
            if (!Textures.TryGetValue(id, out info)) {
                // The id of zero will say that the mipmaps need to be generated before the texture is used
                m_log.Log(LogLevel.DRENDERDETAIL, "Adding TextureInfo for {0}:{1}", entName.Name, id.ToString());
                info.Alpha = hasTransparancy;
            }
        }
    }

    public void RenderUpdate(IEntity ent, UpdateCodes what) {
        m_log.Log(LogLevel.DRENDERDETAIL, "RenderUpdate: {0} for {1}", ent.Name.Name, what);
        if (ent is LLEntityBase && ((what & UpdateCodes.New) != 0)) {
            lock (ent) {
                CreateNewPrim((LLEntityBase)ent);
            }
        }
        return;
    }
    public void UnRender(IEntity ent) {
        return;
    }

    // tell the renderer about the camera position
    public void UpdateCamera(CameraControl cam) {
        if (m_focusRegion != null) {
            Camera.Position.X = (float)(cam.GlobalPosition.X - m_focusRegion.GlobalPosition.X);
            Camera.Position.Y = (float)(cam.GlobalPosition.Y - m_focusRegion.GlobalPosition.Y);
            Camera.Position.Z = (float)(cam.GlobalPosition.Z - m_focusRegion.GlobalPosition.Z);
            OMV.Vector3 dir = new OMV.Vector3(1f, 0f, 0f);
            Camera.FocalPoint = (dir * cam.Heading) + Camera.Position;
        }
        return;
    }
    public void UpdateEnvironmentalLights(EntityLight sun, EntityLight moon) {
        return;
    }

    // Given the current mouse position, return a point in the world
    public OMV.Vector3d SelectPoint() {
        return new OMV.Vector3d(0d, 0d, 0d);
    }

    // rendering specific information for placing in  the view
    public void MapRegionIntoView(RegionContextBase rcontext) {
        return;
    }

    // Set one region as the focus of display
    public void SetFocusRegion(RegionContextBase rcontext) {
        m_focusRegion = rcontext;
        return;
    }

    // something about the terrain has changed, do some updating
    public void UpdateTerrain(RegionContextBase wcontext) {
        return;
    }
    #endregion IRenderProvider
}
}
