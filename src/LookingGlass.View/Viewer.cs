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
using System.Threading;
using System.Windows.Forms;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using LookingGlass.Framework.WorkQueue;
using LookingGlass.Renderer;
using LookingGlass.World;
using OMV = OpenMetaverse;

namespace LookingGlass.View {

    /// <summary>
    /// The viewer looks into a world or worlds and creates a view.
    /// It usually creates a 3D renderer to actually display the world.
    /// In general, the viewer subscribes to world events and maps these
    ///   events into what the renderer needs to make the user's display.
    /// The goal is to make the viewer as world independent as possible.
    ///
    /// The viewer's resposibility is:
    /// Mapping of world coordinates into any renderer coordinates
    /// User input
    ///
    /// </summary>
public class Viewer : ModuleBase, IViewProvider {

    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    private IAgent m_trackedAgent;

    // the viewer manages the camera
    private CameraControl m_mainCamera;
    private enum CameraMode {
        TrackingAgent = 1,
        LookingAt
    }
    private CameraMode m_cameraMode;
    private OMV.Vector3d m_cameraLookAt;
        
    // mouse control
    private int m_lastMouseMoveTime = System.Environment.TickCount;
    private float m_cameraSpeed = 5f;     // world units per second to move
    private float m_cameraRotationSpeed = 0.1f;     // degrees to rotate
    private float m_agentCameraBehind;
    private float m_agentCameraAbove;

    private BasicWorkQueue m_workQueue = new BasicWorkQueue("Viewer");

    /// <summary>
    /// Constructor called in instance of main and not in own thread. This is only
    /// good for setting up structures.
    /// </summary>
    public Viewer() {
    }

    #region IModule methods
    public override void OnLoad(string name, LookingGlassBase lgbase) {
        base.OnLoad(name, lgbase);
        ModuleParams.AddDefaultParameter(m_moduleName + ".Renderer.Name", "Renderer", "");
        // todo: make this variable so there can be multiple viewers

        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.Speed", "5", "Units per second to move camera");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.RotationSpeed", "0.100", "Degrees to rotate camera");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.ServerFar", "300.0", "Far distance sent to server");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.BehindAgent", "4.0", "Distance camera is behind agent");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.AboveAgent", "2.0", "Distance camera is above agent (combined with behind)");

        // m_EntitySlot = EntityBase.AddAdditionSubsystem("VIEWER");        // used by anyone?
    }

    override public bool AfterAllModulesLoaded() {
        m_log.Log(LogLevel.DINIT, "entered AfterAllModulesLoaded()");

        Renderer = (IRenderProvider)ModuleManager.Instance.Module(ModuleParams.ParamString(m_moduleName + ".Renderer.Name"));
        if (Renderer == null) {
            m_log.Log(LogLevel.DBADERROR, "UNABLE TO FIND RENDERER!!!! ");
            return false;
        }

        m_cameraSpeed = ModuleParams.ParamFloat(m_moduleName + ".Camera.Speed");
        m_cameraRotationSpeed = ModuleParams.ParamFloat(m_moduleName + ".Camera.RotationSpeed");
        m_agentCameraBehind = ModuleParams.ParamFloat(m_moduleName + ".Camera.BehindAgent");
        m_agentCameraAbove = ModuleParams.ParamFloat(m_moduleName + ".Camera.AboveAgent");
        m_mainCamera = new CameraControl();
        m_mainCamera.GlobalPosition = new OMV.Vector3d(1000d, 1000d, 40d);   // World coordinates (Z up)
        // camera starts pointing down Y axis
        m_mainCamera.Heading = new OMV.Quaternion(OMV.Vector3.UnitZ, Constants.PI/2);
        m_mainCamera.Zoom = 1.0f;
        m_mainCamera.Far = ModuleParams.ParamFloat(m_moduleName + ".Camera.ServerFar");
        m_cameraMode = CameraMode.TrackingAgent;
        m_cameraLookAt = new OMV.Vector3d(0d, 0d, 0d);

        // connect me to the world so I can know when things change in the world
        TheWorld.OnWorldRegionNew += new WorldRegionNewCallback(World_OnRegionNew);
        TheWorld.OnWorldRegionUpdated += new WorldRegionUpdatedCallback(World_OnRegionUpdated);
        TheWorld.OnWorldRegionRemoved += new WorldRegionRemovedCallback(World_OnRegionRemoved);
        TheWorld.OnWorldEntityNew += new WorldEntityNewCallback(World_OnEntityNew);
        TheWorld.OnWorldEntityUpdate += new WorldEntityUpdateCallback(World_OnEntityUpdate);
        TheWorld.OnWorldEntityRemoved += new WorldEntityRemovedCallback(World_OnEntityRemoved);
        TheWorld.OnAgentNew += new WorldAgentNewCallback(World_OnAgentNew);
        TheWorld.OnAgentUpdate += new WorldAgentUpdateCallback(World_OnAgentUpdate);
        TheWorld.OnAgentRemoved += new WorldAgentRemovedCallback(World_OnAgentRemoved);

        m_log.Log(LogLevel.DINIT, "exiting AfterAllModulesLoaded()");
        return true;
    }

    override public void Start() {
        // this will cause the renderer to move it's camera whenever the main camera is moved
        m_mainCamera.OnCameraUpdate += new CameraControlUpdateCallback(Renderer.UpdateCamera);
        // this will cause camera direction to be sent back  to the server for interest management
        m_mainCamera.OnCameraUpdate += new CameraControlUpdateCallback(CameraControl_OnCameraUpdate);

        // force an initial update to position the displayed camera
        Renderer.UpdateCamera(m_mainCamera);

        // start getting IO stuff from the user
        Renderer.UserInterface.OnUserInterfaceKeypress += new UserInterfaceKeypressCallback(UserInterface_OnKeypress);
        Renderer.UserInterface.OnUserInterfaceMouseMove += new UserInterfaceMouseMoveCallback(UserInterface_OnMouseMove);
        Renderer.UserInterface.OnUserInterfaceMouseButton += new UserInterfaceMouseButtonCallback(UserInterface_OnMouseButton);

        // start the renderer
        // ((IModule)Renderer).Start();

        m_log.Log(LogLevel.DINIT, "exiting Start()");
        return;
    }

    override public void Stop() {
        return;
    }
    #endregion IModule methods

    #region IViewProvider methods
    private IRenderProvider m_Renderer = null;
    public IRenderProvider Renderer { get { return m_Renderer; } set { m_Renderer = value; } }

    public World.World TheWorld {
        get {
            return World.World.Instance;
        }
    }

    #endregion IViewProvider methods

    private void World_OnEntityNew(IEntity ent) {
        // m_log.Log(LogLevel.DVIEWDETAIL, "OnEntityNew: Telling renderer about a new entity");
        Renderer.Render(ent);
    }

    private void World_OnNewFoliage(IEntity ent) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnNewFoliage: Telling renderer about a new foliage entity");
        return;
    }

    private void World_OnEntityUpdate(IEntity ent, World.UpdateCodes what) {
        IEntityAvatar av = null;
        if (ent.TryGet<IEntityAvatar>(out av)) {
            m_log.Log(LogLevel.DUPDATEDETAIL|LogLevel.DVIEWDETAIL, "OnEntityUpdate: Avatar: {0}", ent.Name.Name);
            this.Renderer.RenderUpdate(ent, what);
        }
        else {
            m_log.Log(LogLevel.DUPDATEDETAIL|LogLevel.DVIEWDETAIL, "OnEntityUpdate: Other. w={0}", what);
            this.Renderer.RenderUpdate(ent, what);
        }
        return;
    }

    private void World_OnEntityRemoved(IEntity ent) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnEntityRemoved: ");
        Renderer.UnRender(ent);
        return;
    }

    // When a region is connected, one job is to map it into the view.
    // Chat with the renderer to enhance the rcontext with mapping info
    private void World_OnRegionNew(RegionContextBase rcontext) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnRegionNew: ");
        Renderer.MapRegionIntoView(rcontext);
        return;
    }

    private void World_OnRegionUpdated(RegionContextBase rcontext, UpdateCodes what) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnRegionUpdated: ");
        if ((what & UpdateCodes.Terrain) != 0) {
            // This is first attempt at terrain. The description of the land comes in
            // as a heightmap defined by OMV. The renderer will have to deal with that.
            // How to generalize this so it works for any world representation?
            // What about a cylindrical spaceship world?
            Renderer.UpdateTerrain(rcontext);
        }
        // TODO: other things to test?
        return;
    }

    // When a region is connected, one job is to map it into the view.
    // Chat with the renderer to enhance the rcontext with mapping info
    private void World_OnRegionRemoved(RegionContextBase rcontext) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnRegionRemoved: ");
        // TODO: when we have proper region management
        return;
    }


    // called when the camera changes position or orientation
    private void CameraControl_OnCameraUpdate(CameraControl cam) {
        // m_log.Log(LogLevel.DVIEWDETAIL, "OnCameraUpdate: ");
        if (m_trackedAgent != null) {
            // tell the agent the camera moved if it cares
            // This is an outgoing message that tells the world where the camera is
            //   pointing so the server can do interest management
            m_trackedAgent.UpdateCamera(cam.GlobalPosition, cam.Heading, cam.Far);
        }
    }

    #region user IO
    // called from the renderer when the mouse moves
    private void UserInterface_OnMouseMove(int param, float x, float y) {
        Object[] moveParams = { param, x, y };
        m_workQueue.DoLater(UI_MouseMoveLater, moveParams);
        return;
    }

    private bool UI_MouseMoveLater(DoLaterBase qInstance, Object parms) {
        Object[] loadParams = (Object[])parms;
        int param = (int)loadParams[0];
        float x = (float)loadParams[1];
        float y = (float)loadParams[2];

        int sinceLastMouse = System.Environment.TickCount - m_lastMouseMoveTime;
        m_lastMouseMoveTime = System.Environment.TickCount;
        // m_log.Log(LogLevel.DVIEWDETAIL, "OnMouseMove: x={0}, y={1}, time since last={2}", x, y, sinceLastMouse);
        if (m_mainCamera != null) {
            if (((Renderer.UserInterface.LastKeyCode & Keys.Control) == 0)
                    && ((Renderer.UserInterface.LastKeyCode & Keys.Alt) != 0)) {
                m_log.Log(LogLevel.DVIEWDETAIL, "OnMouseMove: ALT: ");
            }
            else if ( ((Renderer.UserInterface.LastKeyCode & Keys.Control) != 0)
                    && ((Renderer.UserInterface.LastKeyCode & Keys.Alt) != 0) ) {
                // if ALT+CNTL is held down, movement is on view plain
                float xMove = x * m_cameraSpeed;
                float yMove = y * m_cameraSpeed;
                OMV.Vector3d movement = new OMV.Vector3d( 0, xMove, yMove);
                m_log.Log(LogLevel.DVIEWDETAIL, "OnMouseMove: CNTL-ALT: Move camera x={0}, y={1}", xMove, yMove);
                m_mainCamera.GlobalPosition -= movement;
            }
            else if ((Renderer.UserInterface.LastKeyCode & Keys.Control) != 0) {
                // if CNTL is held down, movement is on land plane
                float xMove = x * m_cameraSpeed;
                float yMove = y * m_cameraSpeed;
                m_log.Log(LogLevel.DVIEWDETAIL, "OnMouseMove: CNTL: Move camera x={0}, y={1}", xMove, yMove);
                OMV.Vector3d movement = new OMV.Vector3d( yMove, xMove, 0f);
                m_mainCamera.GlobalPosition -= movement;
            }
            else if ((Renderer.UserInterface.LastMouseButtons & MouseButtons.Left) != 0) {
                    // move the camera around the horizontal (X) and vertical (Z) axis
                    float xMove = (-x * m_cameraRotationSpeed * Constants.DEGREETORADIAN) % Constants.TWOPI;
                    float yMove = (-y * m_cameraRotationSpeed * Constants.DEGREETORADIAN) % Constants.TWOPI;
                    // rotate around local axis
                    // m_log.Log(LogLevel.DVIEWDETAIL, "OnMouseMove: Rotate camera x={0}, y={1}, lmb={2}", 
                    //         xMove, yMove, Renderer.UserInterface.LastMouseButtons);
                    m_mainCamera.rotate(yMove, 0f, xMove);
            }
        }
        return true;
    }

    private void UserInterface_OnMouseButton(MouseButtons param, bool updown) {
        return;
    }

    // called from the renderer when the state of the keyboard changes
    private void UserInterface_OnKeypress(Keys key, bool updown) {
        try {   // we let exceptions test for null
            m_log.Log(LogLevel.DVIEWDETAIL, "UserInterfase_OnKeypress: k={0}, f={1}", key, updown);
            switch (key) {
                case (Keys.Control | Keys.C):
                    // CNTL-C says to stop everything now
                    m_log.Log(LogLevel.DVIEW, "UserInterfase_OnKeypress: CNTL-C. Setting KeepRunning to FALSE");
                    LGB.KeepRunning = false;
                    break;
                case Keys.Right:    m_trackedAgent.TurnRight(updown); break;
                case Keys.L:        m_trackedAgent.TurnRight(updown); break;
                case Keys.Left:     m_trackedAgent.TurnLeft(updown); break;
                case Keys.H:        m_trackedAgent.TurnLeft(updown); break;
                case Keys.Up:       m_trackedAgent.MoveForward(updown); break;
                case Keys.K:        m_trackedAgent.MoveForward(updown); break;
                case Keys.Down:     m_trackedAgent.MoveBackward(updown); break;
                case Keys.J:        m_trackedAgent.MoveBackward(updown); break;
                case Keys.Home:     m_trackedAgent.Fly(updown); break;
                case Keys.PageUp:   m_trackedAgent.MoveUp(updown); break;
                case Keys.PageDown: m_trackedAgent.MoveDown(updown); break;
                case Keys.Escape:
                    // force the camera to the client position
                    m_log.Log(LogLevel.DVIEWDETAIL, "OnKeypress: ESC: restoring camera position");
                    // m_mainCamera.GlobalPosition = m_trackedAgent.GlobalPosition;
                    m_cameraMode = CameraMode.TrackingAgent;
                    UpdateMainCameraToAgentTracking();
                    break;
            }
        }
        catch {
            // don't do anything, the user will type again later
        }
        return;
    }
    #endregion user IO

    #region Agent management
    // When an agent is added to the scene
    // At the moment we don't have good control for associating an agent with the viewer.
    // Assume the last agent is the one we are tracking.
    private void World_OnAgentNew(IAgent agnt) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentNew: ");
        m_trackedAgent = agnt;
        if (m_mainCamera != null) {
            m_cameraMode = CameraMode.TrackingAgent;
            m_mainCamera.AssociatedAgent = agnt;
            UpdateMainCameraToAgentTracking();
            m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentNew: Camera to {0}, {1}, {2}",
                m_mainCamera.GlobalPosition.X, m_mainCamera.GlobalPosition.Y, m_mainCamera.GlobalPosition.Z);
        }
        return;
    }

    private void World_OnAgentUpdate(IAgent agnt, UpdateCodes what) {
        // m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentUpdate: p={0}, h={1}", agnt.GlobalPosition.ToString(), agnt.Heading.ToString());
        if ((what & (UpdateCodes.Rotation | UpdateCodes.Position)) != 0) {
            // if changing position, update the camera position
            if (m_cameraMode == CameraMode.TrackingAgent) {
                if ((agnt != null) && (m_mainCamera != null)) {
                    // m_mainCamera.AssociatedAgent = agnt;
                    // vector for camera position behind the avatar
                    UpdateMainCameraToAgentTracking();
                }
            }
        }
        else {
            m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentUpdate: update code not pos or rot: {0}", what);
        }
        return;
    }

    private void UpdateMainCameraToAgentTracking() {
        try {
            if (m_mainCamera != null && m_mainCamera.AssociatedAgent != null) {
                IAgent agnt = m_mainCamera.AssociatedAgent;
                /*
                // note: coordinates are in LL form: Z up
                OMV.Vector3 cameraOffset = new OMV.Vector3(-m_agentCameraBehind, 0, m_agentCameraAbove);
                OMV.Quaternion invertHeading = OMV.Quaternion.Inverse(agnt.Heading);
                // rotate the vector in the direction the agent is pointing
                OMV.Vector3 cameraBehind = cameraOffset * invertHeading;
                // create the global offset from the agent's position
                OMV.Vector3d globalOffset = new OMV.Vector3d(cameraBehind.X, cameraBehind.Y, cameraBehind.Z);
                m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentUpdate: offset={0}, behind={1}, goffset={2}, gpos={3}",
                    cameraOffset.ToString(), cameraBehind.ToString(), 
                    globalOffset.ToString(), agnt.GlobalPosition.ToString());
                m_mainCamera.Update(agnt.GlobalPosition + globalOffset, agnt.Heading);
                 */
                // OMV.Vector3 cameraOffset = new OMV.Vector3(0, m_agentCameraBehind, m_agentCameraAbove);
                OMV.Vector3 cameraOffset = new OMV.Vector3(m_agentCameraBehind, 0, m_agentCameraAbove);
                // OMV.Vector3 rotatedOffset = Utilities.RotateVector(agnt.Heading, cameraOffset);
                OMV.Vector3 rotatedOffset = cameraOffset * OMV.Quaternion.Inverse(agnt.Heading);
                OMV.Vector3d globalRotatedOffset = new OMV.Vector3d(-rotatedOffset.X, rotatedOffset.Y, rotatedOffset.Z);
                // 'kludgeOffset' exists because the above calculation doesn't give the right camera position
                // Don't know why, but this extra offset is needed
                OMV.Vector3d kludgeOffset = new OMV.Vector3d(10d, 10d, 0d);
                OMV.Vector3d desiredCameraPosition = agnt.GlobalPosition + globalRotatedOffset + kludgeOffset;

                m_log.Log(LogLevel.DVIEWDETAIL, "UpdateMainCameraToAgentTracking: offset={0}, goffset={1}, cpos={2}, apos={3}",
                    cameraOffset, globalRotatedOffset, desiredCameraPosition, agnt.GlobalPosition);

                m_mainCamera.Update(desiredCameraPosition, agnt.Heading);
            }
        }
        catch (Exception e) {
        }
    }

    private void World_OnAgentRemoved(IAgent agnt) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentRemoved: ");
        return;
    }
    #endregion Agent management


}
}
