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

    // our reserved slot in the Entity subsystem additions list
    private int m_EntitySlot;

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
    private float m_cameraSpeed = 100f;     // world units per second to move
    private float m_cameraRotationSpeed = 0.1f;     // degrees to rotate
    private float m_agentCameraBehind;
    private float m_agentCameraAbove;

    /// <summary>
    /// Constructor called in instance of main and not in own thread. This is only
    /// good for setting up structures.
    /// </summary>
    public Viewer() {
    }

    #region IModule methods
    public override void OnLoad(string name, LookingGlassBase lgbase) {
        base.OnLoad(name, lgbase);
        ModuleParams.AddDefaultParameter(m_moduleName + ".World.Name", "World", "Name of world module to connect to");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Renderer.Name", "Renderer", "");
        // todo: make this variable so there can be multiple viewers

        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.Speed", "10", "Units per second to move camera");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.RotationSpeed", "100", "Thousandth of degrees to rotate camera");

        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.BehindAgent", "5", "Distance camera is behind agent");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.AboveAgent", "5", "Distance camera is above agent (combined with behind)");


        m_EntitySlot = EntityBase.AddAdditionSubsystem("VIEWER");
    }

    override public bool AfterAllModulesLoaded() {
        m_log.Log(LogLevel.DINIT, "entered AfterAllModulesLoaded()");

        Renderer = (IRenderProvider)LGB.ModManager.Module(ModuleParams.ParamString(m_moduleName + ".Renderer.Name"));
        if (Renderer == null) {
            m_log.Log(LogLevel.DBADERROR, "UNABLE TO FIND RENDERER!!!! ");
            return false;
        }

        m_cameraSpeed = (float)ModuleParams.ParamInt(m_moduleName + ".Camera.Speed");
        m_cameraRotationSpeed = (float)ModuleParams.ParamInt(m_moduleName + ".Camera.RotationSpeed")/1000;
        m_agentCameraBehind = (float)ModuleParams.ParamInt(m_moduleName + ".Camera.BehindAgent");
        m_agentCameraAbove = (float)ModuleParams.ParamInt(m_moduleName + ".Camera.AboveAgent");
        m_mainCamera = new CameraControl();
        m_mainCamera.GlobalPosition = new OMV.Vector3d(0d, 20d, 30d);   // World coordinates (Z up)
        // camera starts pointing down Y axis
        m_mainCamera.Heading = new OMV.Quaternion(OMV.Vector3.UnitZ, Constants.PI/2);
        m_mainCamera.Zoom = 1.0f;
        m_mainCamera.Far = 300.0f;
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
        m_mainCamera.OnCameraUpdate += new CameraControlUpdateCallback(OnCameraUpdate);

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

    private World.World m_world = null;
    public World.World TheWorld {
        get {
            if (m_world == null) {
                // there is only one world this viewer can be looking at
                m_world = World.World.Instance;
            }
            return m_world;
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
        if (ent is IEntityAvatar) {
            m_log.Log(LogLevel.DVIEWDETAIL, "OnEntityUpdate: Avatar.");
        }
        else {
            m_log.Log(LogLevel.DVIEWDETAIL, "OnEntityUpdate: Other");
        }
        return;
    }

    private void World_OnEntityRemoved(IEntity ent) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnEntityRemoved: ");
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
    private void OnCameraUpdate(CameraControl cam) {
        // m_log.Log(LogLevel.DVIEWDETAIL, "OnCameraUpdate: ");
        if (m_trackedAgent != null) {
            // tell the agent the camera moved if it cares
            // This is an outgoing message that tells the world where the camera is
            //   pointing so the server can do interest management
            m_trackedAgent.UpdateCamera(cam.GlobalPosition, cam.Heading);
        }
    }

    #region user IO
    // called from the renderer when the mouse moves
    private void UserInterface_OnMouseMove(int param, float x, float y) {
        int sinceLastMouse = System.Environment.TickCount - m_lastMouseMoveTime;
        // m_log.Log(LogLevel.DVIEWDETAIL, "OnMouseMove:"
        //             + " x=" + x.ToString() + ", y=" + y.ToString()
        //             + "time since last = " + sinceLastMouse.ToString() );
        if (m_mainCamera != null) {
            if (((Renderer.UserInterface.LastKeyCode & Keys.Control) == 0)
                    && ((Renderer.UserInterface.LastKeyCode & Keys.Alt) != 0)) {
            }
            if ( ((Renderer.UserInterface.LastKeyCode & Keys.Control) != 0)
                    && ((Renderer.UserInterface.LastKeyCode & Keys.Alt) != 0) ) {
                // if ALT+CNTL is held down, movement is on view plain
                float xMove = x * m_cameraSpeed;
                float yMove = y * m_cameraSpeed;
                OMV.Vector3d movement = new OMV.Vector3d( 0, xMove, yMove);
                m_mainCamera.GlobalPosition -= movement;
            }
            else if ((Renderer.UserInterface.LastKeyCode & Keys.Control) != 0) {
                // if CNTL is held down, movement is on land plane
                float xMove = x * m_cameraSpeed;
                float yMove = y * m_cameraSpeed;
                // m_log.Log(LogLevel.DVIEWDETAIL, "OnMouseMove: Move camera x={0}, y={1}", xMove, yMove);
                OMV.Vector3d movement = new OMV.Vector3d( yMove, xMove, 0f);
                m_mainCamera.GlobalPosition -= movement;
            }
            else {
                // move the camera around the horizontal (X) and vertical (Z) axis
                float xMove = (-x * m_cameraRotationSpeed * Constants.DEGREETORADIAN) % Constants.TWOPI;
                float yMove = (-y * m_cameraRotationSpeed * Constants.DEGREETORADIAN) % Constants.TWOPI;
                // rotate around local axis
                // m_log.Log(LogLevel.DVIEWDETAIL, "OnMouseMove: Rotate camera x={0}, y={1}", xMove, yMove);
                m_mainCamera.rotate(yMove, 0f, xMove);
            }
        }
        return;
    }

    private void UserInterface_OnMouseButton(MouseButtons param, bool updown) {
        return;
    }

    // called from the renderer when the state of the keyboard changes
    private void UserInterface_OnKeypress(Keys key, bool updown) {
        try {   // we let exceptions test for null
            switch (key) {
                case (Keys.Control | Keys.C):
                    // CNTL-C says to stop everything now
                    LGB.KeepRunning = false;
                    break;
                case Keys.Right:
                    m_trackedAgent.TurnRight(updown);
                    break;
                case Keys.Left:
                    m_trackedAgent.TurnLeft(updown);
                    break;
                case Keys.Up:
                    m_trackedAgent.MoveForward(updown);
                    break;
                case Keys.Down:
                    m_trackedAgent.MoveBackward(updown);
                    break;
                case Keys.Home:
                    m_trackedAgent.Fly(updown);
                    break;
                case Keys.PageUp:
                    m_trackedAgent.MoveUp(updown);
                    break;
                case Keys.PageDown:
                    m_trackedAgent.MoveDown(updown);
                    break;
                case Keys.Escape:
                    // force the camera to the client position
                    m_log.Log(LogLevel.DVIEWDETAIL, "OnKeypress: ESC: restoring camera position");
                    m_mainCamera.GlobalPosition = m_trackedAgent.GlobalPosition;
                    m_cameraMode = CameraMode.TrackingAgent;
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
            m_mainCamera.GlobalPosition = agnt.GlobalPosition;
            m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentNew: Camera to {0}, {1}, {2}",
                m_mainCamera.GlobalPosition.X, m_mainCamera.GlobalPosition.Y, m_mainCamera.GlobalPosition.Z);
            m_Renderer.UpdateCamera(m_mainCamera);
        }
        return;
    }

    private void World_OnAgentUpdate(IAgent agnt, UpdateCodes what) {
        // m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentUpdate: p={0}, h={1}", agnt.GlobalPosition.ToString(), agnt.Heading.ToString());
        if ((what & (UpdateCodes.Rotation | UpdateCodes.Position)) != 0) {
            if (m_cameraMode == CameraMode.TrackingAgent) {
                if ((agnt != null) && (m_mainCamera != null)) {
                    // vector for camera position behind the avatar
                    // note: coordinates are in LL form: Z up
                    OMV.Quaternion cameraOffset = new OMV.Quaternion(0, -m_agentCameraBehind, m_agentCameraAbove, 0);
                    OMV.Quaternion invertHeading = OMV.Quaternion.Inverse(agnt.Heading);
                    // rotate the vector in the direction the agent is pointing
                    OMV.Quaternion cameraBehind = agnt.Heading * cameraOffset * invertHeading;
                    cameraBehind.Normalize();
                    // create the global offset from the agent's position
                    OMV.Vector3d globalOffset = new OMV.Vector3d(cameraBehind.X, cameraBehind.Y, cameraBehind.Z);
                    // m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentUpdate: offset={0}, behind={1}, goffset={2}, gpos={3}",
                    //     cameraOffset.ToString(), cameraBehind.ToString(), 
                    //     globalOffset.ToString(), agnt.GlobalPosition.ToString());
                    m_mainCamera.Update(agnt.GlobalPosition + globalOffset, agnt.Heading);
                }
            }
        }
        return;
    }

    private void World_OnAgentRemoved(IAgent agnt) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentRemoved: ");
        return;
    }
    #endregion Agent management


}
}
