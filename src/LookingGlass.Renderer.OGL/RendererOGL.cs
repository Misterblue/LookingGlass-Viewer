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
    public HeightmapLookupValue[] LookupHeightTable;
    public OMV.TerrainPatch[,] Heightmap;

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
        ModuleParams.AddDefaultParameter(m_moduleName + "InputSystem.Name", "WindowUI",
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
            }
        }
        return;
    }

    private void CreateNewPrim(LLEntityBase ent) {
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

        // FIXME: Handle sculpted prims by calling Render.Plugin.GenerateFacetedSculptMesh() instead
        if (m_meshMaker == null) {
            m_meshMaker = new Renderer.Mesher.MeshmerizerR();
            m_meshMaker.ShouldScaleMesh = true;
        }
        render.Mesh = m_meshMaker.GenerateFacetedMesh(prim, OMVR.DetailLevel.High);

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

            // Texture for this face
            if (teFace.TextureID != OMV.UUID.Zero &&
                        teFace.TextureID != OMV.Primitive.TextureEntry.WHITE_TEXTURE) {
                lock (Textures) {
                    if (!Textures.ContainsKey(teFace.TextureID)) {
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
        EntityName entName = new EntityName(textureEntityName);
        OMV.UUID id = new OMV.UUID(entName.ExtractEntityFromEntityName());

        TextureInfo info;
        if (!Textures.TryGetValue(id, out info)) {
            // Put initial info into the texture list to say it is in the cache
            // The id of zero will say that the mipmaps need to be generated before the texture is used
            Textures.Add(id, new TextureInfo(0, hasTransparancy));
        }


        try {
            /*
            // Load the image off the disk
            if (success) {
                //ImageDownload download = TextureDownloader.GetTextureToRender(id);
                if (OpenJPEG.DecodeToImage(asset.AssetData, out imgData)) {
                    raw = imgData.ExportRaw();

                    if ((imgData.Channels & OMVI.ManagedImage.ImageChannels.Alpha) != 0)
                        alpha = true;
                }
                else {
                    success = false;
                    m_log.Log(LogLevel.DRENDER, "Failed to decode texture {0}", textureEntityName);
                }
            }

            // Make sure the OpenGL commands run on the main thread
            BeginInvoke(
                   (MethodInvoker)delegate() {
                       if (success) {
                           int textureID = 0;

                           try {
                               Gl.glGenTextures(1, out textureID);
                               Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureID);

                               Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR_MIPMAP_NEAREST); //Gl.GL_NEAREST);
                               Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
                               Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_REPEAT);
                               Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_REPEAT);
                               Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_GENERATE_MIPMAP, Gl.GL_TRUE); //Gl.GL_FALSE);

                               //Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, bitmap.Width, bitmap.Height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE,
                               //    bitmapData.Scan0);
                               //int error = Gl.glGetError();

                               int error = Glu.gluBuild2DMipmaps(Gl.GL_TEXTURE_2D, Gl.GL_RGBA, imgData.Width, imgData.Height, Gl.GL_BGRA,
                                   Gl.GL_UNSIGNED_BYTE, raw);

                               if (error == 0) {
                                   Textures[id] = new TextureInfo(textureID, alpha);
                                   m_log.Log(LogLevel.DRENDERDETAIL, "Created OpenGL texture for {0}", id);
                               }
                               else {
                                   Textures[id] = new TextureInfo(0, false);
                                   m_log.Log(LogLevel.DRENDER, "Error creating OpenGL texture: {0}", error);
                               }
                           }
                           catch (Exception ex) {
                               Console.WriteLine(ex);
                           }
                       }
                   });
            */
        }
        catch (Exception ex) {
            m_log.Log(LogLevel.DRENDER, "EXCEPTION decoding texture: {0}", ex);
        }
    }

    public void RenderUpdate(IEntity ent, UpdateCodes what) {
        return;
    }
    public void UnRender(IEntity ent) {
        return;
    }

    // tell the renderer about the camera position
    public void UpdateCamera(CameraControl cam) {
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
        return;
    }

    // something about the terrain has changed, do some updating
    public void UpdateTerrain(RegionContextBase wcontext) {
        return;
    }
    #endregion IRenderProvider
}
}
