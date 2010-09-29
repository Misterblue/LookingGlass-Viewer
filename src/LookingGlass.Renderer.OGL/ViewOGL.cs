/* Copyright (c) 2010 Robert Adams
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Tao.OpenGl;
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using LookingGlass.World;
using LookingGlass.World.LL;
using OMV = OpenMetaverse;
using OMVR = OpenMetaverse.Rendering;


namespace LookingGlass.Renderer.OGL {
    public partial class ViewOGL : Form, IModule, IViewOGL {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    const uint TERRAIN_START = (uint)Int32.MaxValue + 1;

    private RendererOGL m_renderer;
    private System.Threading.Timer m_refreshTimer;
    private int m_framesPerSec;
    private int m_frameTimeMs;      // 1000/framesPerSec
    private int m_frameAllowanceMs; // maz(1000/framesPerSec - 30, 10) time allowed for frame plus extra work

    private IUserInterfaceProvider m_UILink = null;
    private bool m_MouseIn = false;     // true if mouse is over our window
    private bool m_keyDown = false;     // true if key is pressed
    private float m_MouseLastX = -3456f;
    private float m_MouseLastY = -3456f;

    private bool m_glControlLoaded = false;

    #region IModule
    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected LookingGlassBase m_lgb = null;
    public LookingGlassBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    public ViewOGL() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }

    // IModule.OnLoad
    public virtual void OnLoad(string modName, LookingGlassBase lgbase) {
        m_log.Log(LogLevel.DINIT, ModuleName + ".OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;

        m_lgb.AppParams.AddDefaultParameter("ViewOGL.Renderer.Name", "Renderer", "The renderer we will get UI from");
        m_lgb.AppParams.AddDefaultParameter("ViewOGL.FramesPerSec", "12", "The rate to throttle frame rendering");

        InitializeComponent();
        // glControl.InitializeContexts();
    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        m_log.Log(LogLevel.DINIT, ModuleName + ".AfterAllModulesLoaded()");

        try {
            // get a handle to the renderer module in LookingGlass
            string rendererName = m_lgb.AppParams.ParamString("ViewOGL.Renderer.Name");
            m_framesPerSec = Math.Min(100, Math.Max(1, m_lgb.AppParams.ParamInt("ViewOGL.FramesPerSec")));
            m_frameTimeMs = 1000 / m_framesPerSec;
            m_frameAllowanceMs = Math.Max(m_framesPerSec - 30, 10);
            m_renderer = (RendererOGL)m_lgb.ModManager.Module(rendererName);
            m_log.Log(LogLevel.DVIEWDETAIL, "Initialize. Connecting to renderer {0} at {1}fps",
                            m_renderer, m_framesPerSec);

        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Initialize. exception: {0}", e.ToString());
            throw new LookingGlassException("Exception initializing view");
        }

        return true;
    }

    // IModule.Start
    public virtual void Start() {
        this.Initialize();
        this.Visible = true;
        return;
    }

    // IModule.Stop
    public virtual void Stop() {
        this.Shutdown();
        return;
    }

    // IModule.PrepareForUnload
    public virtual bool PrepareForUnload() {
        return false;
    }
    #endregion IModule

    public void Initialize() {
        try {
            // the link to the renderer for display is also a link to the user interface routines
            m_UILink = m_renderer.UserInterface;

            
            m_refreshTimer = new System.Threading.Timer(delegate(Object param) {
                this.glControl.Invalidate();
                }, null, 2000, m_frameTimeMs);
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Initialize. exception: {0}", e.ToString());
            throw new LookingGlassException("Exception initializing view");
        }
    }

    public void Shutdown() {
        if (this.InvokeRequired) {
            BeginInvoke((MethodInvoker)delegate() { this.Close(); });
        }
        else {
            this.Close();
        }
        if (m_refreshTimer != null) {
            m_refreshTimer.Dispose();
        }
    }

    private void InitOpenGL() {
        GL.ClearColor(0f, 0f, 0f, 0f);

        GL.ClearDepth(1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Lequal);

        GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
        GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)All.Modulate);
    }

    private void InitLighting() {
        GL.ShadeModel(ShadingModel.Smooth);
        GL.Enable(EnableCap.Lighting);

        OMV.Vector3 ambientSpec = ModuleParams.ParamVector3(m_renderer.m_moduleName + ".OGL.Global.Ambient");
        float[] globalAmbient = { ambientSpec.X, ambientSpec.Y, ambientSpec.Z, 1.0f };
        GL.LightModel(LightModelParameter.LightModelAmbient, globalAmbient);

        // set up the sun (Light0)
        GL.Enable(EnableCap.Light0);
        OMV.Vector3 sunSpec = ModuleParams.ParamVector3(m_renderer.m_moduleName + ".OGL.Sun.Ambient");
        float[] sunAmbient = { sunSpec.X, sunSpec.Y, sunSpec.Z, 1.0f };
        sunSpec = ModuleParams.ParamVector3(m_renderer.m_moduleName + ".OGL.Sun.Diffuse");
        float[] sunDiffuse = { sunSpec.X, sunSpec.Y, sunSpec.Z, 1.0f };
        sunSpec = ModuleParams.ParamVector3(m_renderer.m_moduleName + ".OGL.Sun.Specular");
        float[] sunSpecular = { sunSpec.X, sunSpec.Y, sunSpec.Z, 1.0f };
        GL.Light(LightName.Light0, LightParameter.Ambient, sunAmbient);
        GL.Light(LightName.Light0, LightParameter.Diffuse, sunDiffuse);
        GL.Light(LightName.Light0, LightParameter.Specular, sunSpecular);
    }

    private void InitCamera() {
        m_renderer.Camera = new Camera();
        m_renderer.Camera.Position = new OMV.Vector3(128f, -192f, 90f);
        m_renderer.Camera.FocalPoint = new OMV.Vector3(128f, 128f, 0f);
        m_renderer.Camera.Zoom = 1.0d;
        // m_renderer.Camera.Far = 512.0d;
        m_renderer.Camera.Far = (double)ModuleParams.ParamFloat(m_renderer.m_moduleName + ".OGL.Camera.Far");
    }

    #region GLControl Callbacks
    private class FakeScrollBar {
        private int m_value = 50;
        public int Value { get { return m_value; } set { m_value = value; } }
    }
    private class FakeComboBox {
        private int m_selectedIndex = 0;
        public int SelectedIndex { get { return m_selectedIndex; } set { m_selectedIndex = value; } }
    }
    FakeScrollBar scrollZoom = new FakeScrollBar();
    FakeScrollBar scrollRoll = new FakeScrollBar();
    FakeScrollBar scrollPitch = new FakeScrollBar();
    FakeScrollBar scrollYaw = new FakeScrollBar();
    FakeComboBox cboPrim = new FakeComboBox();
    FakeComboBox cboFace = new FakeComboBox();

    private void GLWindow_Paint(object sender, PaintEventArgs e) {
        if (!m_glControlLoaded) return;
        RenderScene();
    }

    private void GLWindow_Resize(object sender, EventArgs e) {
        if (!m_glControlLoaded) return;
        GL.ClearColor(0.39f, 0.58f, 0.93f, 1.0f);

        GL.Viewport(0, 0, glControl.Width, glControl.Height);

        GL.PushMatrix();
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadIdentity();

        Glu.gluPerspective(50.0d, 1.0d, 0.1d, 256d);

        GL.MatrixMode(MatrixMode.Modelview);
        GL.PopMatrix();
    }

    private void GLWindow_Load(object sender, EventArgs e) {
        if (m_glControlLoaded) return;
        m_glControlLoaded = true;
        m_log.Log(LogLevel.DRENDERDETAIL, "GLWindow_Load");

        InitOpenGL();
        InitLighting();
        InitCamera();

        // Call the resizing function which sets up the GL drawing window
        // and will also invalidate the GL control
        GLWindow_Resize(null, null);

        this.Visible = true;
        return;
    }

    #endregion GLControl Callbacks

    private int tickOfLastFrame = Utilities.TickCount();
    private void RenderScene() {
        int tickBasis = Utilities.TickCountSubtract(tickOfLastFrame);
        tickOfLastFrame = Utilities.TickCount();
        float timeSinceLastFrame = (float)tickBasis / 1000f;    // seconds since last frame
        try {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.LoadIdentity();

            // Setup wireframe or solid fill drawing mode
            // GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
            GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);

            // Position the camera
            Glu.gluLookAt(
                m_renderer.Camera.Position.X, m_renderer.Camera.Position.Y, m_renderer.Camera.Position.Z,
                m_renderer.Camera.FocalPoint.X, m_renderer.Camera.FocalPoint.Y, m_renderer.Camera.FocalPoint.Z,
                0f, 0f, 1f);

            RenderSkybox();

            float[] sunPosition = { 500.0f, 500.0f, 500.0f, 1.0f };
            GL.Light(LightName.Light0, LightParameter.Position, sunPosition);

            foreach (RegionContextBase rcontext in m_renderer.m_trackedRegions) {
                RegionRenderInfo rri;
                if (rcontext.TryGet<RegionRenderInfo>(out rri)) {
                    GL.PushMatrix();

                    RenderTerrain(rcontext, rri);
                    RenderOcean(rcontext, rri);
                    // RenderAnimations(timeSinceLastFrame, rcontext, rri);
                    RenderPrims(rcontext, rri);
                    RenderAvatars(rcontext, rri);

                    GL.PopMatrix();
                }
            }

            GL.DisableClientState(ArrayCap.NormalArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
            GL.DisableClientState(ArrayCap.VertexArray);

            GL.Flush();
            glControl.SwapBuffers();
        }
        catch (Exception e ) {
            m_log.Log(LogLevel.DBADERROR, "Exception rendering frame: {0}", e);
        }
    }

    static readonly float[] SkyboxVerts = {
              00.0f, 10.0f, 00.0f,
              10.0f, 10.0f, 00.0f,
              10.0f, 00.0f, 00.0f,
              00.0f, 00.0f, 00.0f,
              10.0f, 10.0f, 10.0f,
              00.0f, 10.0f, 10.0f,
              00.0f, 00.0f, 10.0f,
              10.0f, 00.0f, 10.0f,
    };
    static readonly ushort[] SkyboxIndices = {
                0, 1, 2,    // top
                0, 2, 3,
                4, 5, 6,    // bottom
                4, 6, 7,
                5, 0, 3,    // sides
                5, 3, 6,
                1, 0, 5,    // side 2
                1, 5, 4,
                7, 1, 4,    // side 3
                7, 2, 1,
                3, 2, 7,    // side 4
                3, 7, 6
    };

    static readonly string[] SkyboxTextures = {
              "Preload/00000000-0000-0000-9999-9999000000UP",
              "Preload/00000000-0000-0000-9999-9999000000DN",
              "Preload/00000000-0000-0000-9999-9999000000BK",
              "Preload/00000000-0000-0000-9999-9999000000LF",
              "Preload/00000000-0000-0000-9999-9999000000FR",
              "Preload/00000000-0000-0000-9999-9999000000RT",
                                                  };

    private void RenderSkybox() {
        /*
        GL.Translate(0f, 0f, 0f);
        GL.Disable(EnableCap.DepthTest);
         */

    }

    private void RenderTerrain(RegionContextBase rcontext, RegionRenderInfo rri) {
        GL.PushMatrix();

        // if the terrain has changed, 
        if (rri.refreshTerrain) {
            rri.refreshTerrain = false;
            UpdateRegionTerrainMesh(rcontext, rri);
        }

        // apply region offset
        GL.MultMatrix(Math3D.CreateTranslationMatrix(CalcRegionOffset(rcontext)));

        // everything built. Display the terrain
        GL.EnableClientState(ArrayCap.VertexArray);
        GL.EnableClientState(ArrayCap.TextureCoordArray);
        GL.EnableClientState(ArrayCap.NormalArray);

        GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, rri.terrainTexCoord);
        GL.VertexPointer(3, VertexPointerType.Float, 0, rri.terrainVertices);
        GL.NormalPointer(NormalPointerType.Float, 0, rri.terrainNormal);
        GL.DrawElements(BeginMode.Quads, rri.terrainIndices.Length, DrawElementsType.UnsignedShort, rri.terrainIndices);

        GL.PopMatrix();
    }

    private void UpdateRegionTerrainMesh(RegionContextBase rcontext, RegionRenderInfo rri) {
        TerrainInfoBase ti = rcontext.TerrainInfo;
        rri.terrainVertices = new float[3 * ti.HeightMapLength * ti.HeightMapWidth];
        rri.terrainTexCoord = new float[2 * ti.HeightMapLength * ti.HeightMapWidth];
        rri.terrainNormal = new float[3 * ti.HeightMapLength * ti.HeightMapWidth];
        int nextVert = 0;
        int nextTex = 0;
        int nextNorm = 0;
        for (int xx=0; xx < ti.HeightMapLength; xx++) {
            for (int yy = 0; yy < ti.HeightMapWidth; yy++ ) {
                rri.terrainVertices[nextVert + 0] = (float)xx / ti.HeightMapLength * rcontext.Size.X;
                rri.terrainVertices[nextVert + 1] = (float)yy / ti.HeightMapWidth * rcontext.Size.Y;
                rri.terrainVertices[nextVert + 2] = ti.HeightMap[xx, yy];
                nextVert += 3;
                rri.terrainTexCoord[nextTex + 0] = xx / ti.HeightMapLength;
                rri.terrainTexCoord[nextTex + 1] = yy / ti.HeightMapWidth;
                nextTex += 2;
                rri.terrainNormal[nextNorm + 0] = 0f;   // simple normal pointing up
                rri.terrainNormal[nextNorm + 1] = 1f;
                rri.terrainNormal[nextNorm + 2] = 0f;
                nextNorm += 3;
            }
        }
        // Create the quads which make up the terrain
        rri.terrainIndices = new UInt16[4 * ti.HeightMapLength * ti.HeightMapWidth];
        int nextInd = 0;
        for (int xx=0; xx < ti.HeightMapLength-1; xx++) {
            for (int yy = 0; yy < ti.HeightMapWidth-1; yy++ ) {
                rri.terrainIndices[nextInd + 0] = (UInt16)((yy + 0) + (xx + 0)* ti.HeightMapLength);
                rri.terrainIndices[nextInd + 1] = (UInt16)((yy + 1) + (xx + 0)* ti.HeightMapLength);
                rri.terrainIndices[nextInd + 2] = (UInt16)((yy + 1) + (xx + 1)* ti.HeightMapLength);
                rri.terrainIndices[nextInd + 3] = (UInt16)((yy + 0) + (xx + 1)* ti.HeightMapLength);
                nextInd += 4;
            }
        }
        rri.terrainWidth = ti.HeightMapWidth;
        rri.terrainLength = ti.HeightMapLength;

        // Calculate normals
        // Take three corners of each quad and calculate the normal for the vector
        //   a--b--e--...
        //   |  |  |
        //   d--c--h--...
        // The triangle a-b-d calculates the normal for a, etc
        int nextQuad = 0;
        int nextNrm = 0;
        for (int xx=0; xx < ti.HeightMapLength-1; xx++) {
            for (int yy = 0; yy < ti.HeightMapWidth-1; yy++ ) {
                OMV.Vector3 aa, bb, cc;
                int offset = rri.terrainIndices[nextQuad + 0] * 3;
                aa.X = rri.terrainVertices[offset + 0];
                aa.Y = rri.terrainVertices[offset + 1];
                aa.Z = rri.terrainVertices[offset + 2];
                offset = rri.terrainIndices[nextQuad + 1] * 3;
                bb.X = rri.terrainVertices[offset + 0];
                bb.Y = rri.terrainVertices[offset + 1];
                bb.Z = rri.terrainVertices[offset + 2];
                offset = rri.terrainIndices[nextQuad + 3] * 3;
                cc.X = rri.terrainVertices[offset + 0];
                cc.Y = rri.terrainVertices[offset + 1];
                cc.Z = rri.terrainVertices[offset + 2];
                OMV.Vector3 mm = aa - bb;
                OMV.Vector3 nn = aa - cc;
                OMV.Vector3 theNormal = OMV.Vector3.Cross(mm, nn);
                theNormal.Normalize();
                rri.terrainNormal[nextNrm + 0] = theNormal.X;   // simple normal pointing up
                rri.terrainNormal[nextNrm + 1] = theNormal.Y;
                rri.terrainNormal[nextNrm + 2] = theNormal.Z;
                nextNrm += 3;
                nextQuad += 4;
            }
            rri.terrainNormal[nextNrm + 0] = 1.0f;
            rri.terrainNormal[nextNrm + 1] = 0.0f;
            rri.terrainNormal[nextNrm + 2] = 0.0f;
            nextNrm += 3;
        }
    }

    private void RenderOcean(RegionContextBase rcontext, RegionRenderInfo rri) {
        GL.PushMatrix();

        // apply region offset
        GL.MultMatrix(Math3D.CreateTranslationMatrix(CalcRegionOffset(rcontext)));

        GL.PopMatrix();
        return;
    }

    //int[] CubeMapDefines = new int[]
    //{
    //    Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_X_ARB,
    //    Gl.GL_TEXTURE_CUBE_MAP_NEGATIVE_X_ARB,
    //    Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_Y_ARB,
    //    Gl.GL_TEXTURE_CUBE_MAP_NEGATIVE_Y_ARB,
    //    Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_Z_ARB,
    //    Gl.GL_TEXTURE_CUBE_MAP_NEGATIVE_Z_ARB
    //};

    private void RenderPrims(RegionContextBase rcontext, RegionRenderInfo rri) {
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Texture2D);

        lock (rri.renderPrimList) {
            bool firstPass = true;
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);

        StartRender:

            foreach (RenderablePrim rp in rri.renderPrimList.Values) {
                RenderablePrim prp = RenderablePrim.Empty;
                OMV.Primitive prim = rp.Prim;

                if (prim.ParentID != 0) {
                    // Get the parent reference
                    if (!rri.renderPrimList.TryGetValue(prim.ParentID, out prp)) {
                        // Can't render a child with no parent prim, skip it
                        continue;
                    }
                }

                GL.PushName(prim.LocalID);
                GL.PushMatrix();

                if (prim.ParentID != 0) {
                    // Apply parent translation and rotation
                    GL.MultMatrix(Math3D.CreateTranslationMatrix(prp.Position));
                    GL.MultMatrix(Math3D.CreateRotationMatrix(prp.Rotation));
                }

                // Apply prim translation and rotation
                GL.MultMatrix(Math3D.CreateTranslationMatrix(rp.Position));
                // apply region offset for multiple regions
                GL.MultMatrix(Math3D.CreateTranslationMatrix(CalcRegionOffset(rp.rcontext)));
                GL.MultMatrix(Math3D.CreateRotationMatrix(rp.Rotation));

                // Scale the prim
                GL.Scale(prim.Scale.X, prim.Scale.Y, prim.Scale.Z);

                // Draw the prim faces
                for (int j = 0; j < rp.Mesh.Faces.Count; j++) {
                    OMVR.Face face = rp.Mesh.Faces[j];
                    FaceData data = (FaceData)face.UserData;
                    OMV.Color4 color = face.TextureFace.RGBA;
                    bool alpha = false;
                    int textureID = 0;

                    if (color.A < 1.0f)
                        alpha = true;

                    TextureInfo info;
                    if (face.TextureFace.TextureID != OMV.UUID.Zero
                                && face.TextureFace.TextureID != OMV.Primitive.TextureEntry.WHITE_TEXTURE
                                && m_renderer.Textures.TryGetValue(face.TextureFace.TextureID, out info)) {
                        if (info.Alpha) alpha = true;

                        textureID = info.ID;
                        // if textureID has not been set, need to generate the mipmaps
                        if (textureID == 0) {
                            GenerateMipMaps(rp.acontext, face.TextureFace.TextureID, out textureID);
                            info.ID = textureID;
                        }

                        // Enable texturing for this face
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    }
                    else {
                        if (face.TextureFace.TextureID == OMV.Primitive.TextureEntry.WHITE_TEXTURE ||
                                        face.TextureFace.TextureID == OMV.UUID.Zero) {
                            GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
                        }
                        else {
                            GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
                        }
                    }

                    if (firstPass && !alpha || !firstPass && alpha) {
                        // GL.Color4(color.R, color.G, color.B, color.A);
                        float[] matDiffuse = { color.R, color.G, color.B, color.A };
                        GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, matDiffuse);

                        // Bind the texture
                        if (textureID != 0) {
                            GL.Enable(EnableCap.Texture2D);
                            GL.BindTexture(TextureTarget.Texture2D, textureID);
                        }
                        else {
                            GL.Disable(EnableCap.Texture2D);
                        }

                        // GL.Enable(EnableCap.Normalize);

                        GL.EnableClientState(ArrayCap.TextureCoordArray);
                        GL.EnableClientState(ArrayCap.VertexArray);
                        GL.EnableClientState(ArrayCap.NormalArray);

                        GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, data.TexCoords);
                        GL.VertexPointer(3, VertexPointerType.Float, 0, data.Vertices);
                        GL.NormalPointer(NormalPointerType.Float, 0, data.Normals);
                        GL.DrawElements(BeginMode.Triangles, data.Indices.Length, DrawElementsType.UnsignedShort, data.Indices);
                    }
                }

                GL.PopMatrix();
                GL.PopName();
            }

            if (firstPass) {
                firstPass = false;
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                // GL.Disable(EnableCap.DepthTest);

                goto StartRender;
            }
        }

        // GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Texture2D);
    }

    private void GenerateMipMaps(AssetContextBase acontext, OMV.UUID textureUUID, out int textureID) {
        EntityNameLL textureEntityName = EntityNameLL.ConvertTextureWorldIDToEntityName(acontext, textureUUID);

        // see if the cache file exists -- if not, we don't get a texture this frame
        if (!acontext.CheckIfCached(textureEntityName)) {
            textureID = 0;
            return;
        }

        int id = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, id);

        try {
            using (Bitmap bmp = acontext.GetTexture(textureEntityName)) {
                BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0,
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

                bmp.UnlockBits(bmp_data);
            }

            // We haven't uploaded mipmaps, so disable mipmapping (otherwise the texture will not appear).
            // On newer video cards, we can use GL.GenerateMipmaps() or GL.Ext.GenerateMipmaps() to create
            // mipmaps automatically. In that case, use TextureMinFilter.LinearMipmapLinear to enable them.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Failed binding texture id={0}, uuid={1}: {2}", id, textureUUID, e);
            textureID = 0;
        }

        textureID = id;
        return;
    }

        /// <summary>
        /// When multiple regions are displayed, there is a focus region (the one the main avatar
        /// is in) and other regions that are offset from that focus region. Here we come up with
        /// that offset.
        /// </summary>
        /// <param name="rcontext"></param>
        /// <returns></returns>
    private OMV.Vector3 CalcRegionOffset(RegionContextBase rcontext) {
        if (rcontext == m_renderer.m_focusRegion) return OMV.Vector3.Zero;
        OMV.Vector3 ret = new OMV.Vector3();
        ret.X = (float)(m_renderer.m_focusRegion.GlobalPosition.X - rcontext.GlobalPosition.X);
        ret.Y = (float)(m_renderer.m_focusRegion.GlobalPosition.Y - rcontext.GlobalPosition.Y);
        ret.Z = (float)(m_renderer.m_focusRegion.GlobalPosition.Z - rcontext.GlobalPosition.Z);
        return ret;
    }

    private void RenderAvatars(RegionContextBase rcontext, RegionRenderInfo rri)
    {
        OMV.Vector3 avatarColor = ModuleParams.ParamVector3(m_renderer.m_moduleName + ".OGL.Avatar.Color");
        float avatarTransparancy = ModuleParams.ParamFloat(m_renderer.m_moduleName + ".OGL.Avatar.Transparancy");
        float[] m_avatarMaterialColor = {avatarColor.X, avatarColor.Y, avatarColor.Z, avatarTransparancy};

        lock (rri.renderAvatarList) {
            foreach (RenderableAvatar rav in rri.renderAvatarList.Values) {
                GL.PushMatrix();
                GL.Translate(rav.avatar.RegionPosition.X, 
                    rav.avatar.RegionPosition.Y, 
                    rav.avatar.RegionPosition.Z);

                GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Diffuse, m_avatarMaterialColor);
                GL.Disable(EnableCap.Texture2D);
                Glu.GLUquadric quad = Glu.gluNewQuadric();
                Glu.gluSphere(quad, 0.5d, 10, 10);
                Glu.gluDeleteQuadric(quad);

                GL.PopMatrix();
            }
        }
    }

        /*
    private OMV.Vector3 ParamVector3(string paramName) {
        OMV.Vector3 ret = OMV.Vector3.Zero;
        string parm = ModuleParams.ParamString(paramName);
        if (OMV.Vector3.TryParse(parm, out ret)) {
            return ret;
        }
        return OMV.Vector3.Zero;
    }
         */

    private void GLWindow_MouseDown(object sender, MouseEventArgs e) {
        if (m_UILink != null && m_MouseIn) {
            int butn = ConvertMouseButtonCode(e.Button);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.MouseButtonDown, butn, 0f, 0f);
        }
    }

    private void GLWindow_MouseMove(object sender, MouseEventArgs e) {
        if (m_UILink != null && m_MouseIn) {
            // ReceiveUserIO wants relative mouse movement. Convert abs to rel
            int butn = ConvertMouseButtonCode(e.Button);
            if (m_MouseLastX == -3456f) m_MouseLastX = e.X;
            if (m_MouseLastY == -3456f) m_MouseLastY = e.Y;
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.MouseMove, butn,
                            e.X - m_MouseLastX, e.Y - m_MouseLastY);
            m_MouseLastX = e.X;
            m_MouseLastY = e.Y;
        }
    }

    private void GLWindow_MouseLeave(object sender, EventArgs e) {
        m_MouseIn = false;
    }

    private void GLWindow_MouseEnter(object sender, EventArgs e) {
        m_MouseIn = true;
    }

    private void GLWindow_MouseUp(object sender, MouseEventArgs e) {
        if (m_UILink != null) {
            int butn = ConvertMouseButtonCode(e.Button);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.MouseButtonUp, butn, 0f, 0f);
        }
    }

    private void GLWindow_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) {
        if (m_UILink != null) {
            m_keyDown = true;
            // LogManager.Log.Log(LogLevel.DVIEWDETAIL, "ViewWindow.GLWindow_KeyDown: k={0}", e.KeyCode);
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.KeyPress, (int)e.KeyCode, 0f, 0f);
        }
    }

    private void GLWindow_KeyDown(object sender, KeyEventArgs e) {

    }

    private void GLWindow_KeyUp(object sender, KeyEventArgs e) {
        if (m_UILink != null) {
            // LogManager.Log.Log(LogLevel.DVIEWDETAIL, "ViewWindow.GLWindow_KeyUp: k={0}", e.KeyCode);
            // debug == we're only getting key ups
            if (!m_keyDown) {
                // for some reason, some keys don't give us a down to go with the up
                m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.KeyPress, (int)e.KeyCode, 0f, 0f);
            }
            m_UILink.ReceiveUserIO(ReceiveUserIOInputEventTypeCode.KeyRelease, (int)e.KeyCode, 0f, 0f);
            m_keyDown = false;
        }
    }

    private int ConvertMouseButtonCode(MouseButtons inCode) {
        if ((inCode & MouseButtons.Left) != 0) return (int)ReceiveUserIOMouseButtonCode.Left;
        if ((inCode & MouseButtons.Right) != 0) return (int)ReceiveUserIOMouseButtonCode.Right;
        if ((inCode & MouseButtons.Middle) != 0) return (int)ReceiveUserIOMouseButtonCode.Middle;
        return 0;
    }
}
}
