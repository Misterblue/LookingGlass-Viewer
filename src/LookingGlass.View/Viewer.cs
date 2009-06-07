﻿/* Copyright (c) Robert Adams
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

    private IRenderProvider m_Renderer = null;
    public IRenderProvider Renderer { get { return m_Renderer; } set { m_Renderer = value; } }

    private World.World m_world = null;
    public World.World TheWorld {
        get {
            if (m_world == null) {
                string worldName = ModuleParams.ParamString(ModuleName + ".World.Name");
                try {
                    m_world = (World.World)ModuleManager.Module(worldName);
                }
                catch (Exception e) {
                    m_log.Log(LogLevel.DBADERROR, "FAILED TO FIND WORLD " + worldName + ": " + e.ToString());
                    throw new LookingGlassException("Viewer " + ModuleName
                        + " could not find world to connect to in parameter "
                        + ModuleName + ".World.Name");
                }
            }
            return m_world;
        }
    }

    // the viewer manages the camera
    private EntityCamera m_mainCamera;
    private IAgent m_trackedAgent;

    // mouse control
    private DateTime m_lastMouseMoveTime = System.DateTime.UtcNow;
    private float m_cameraSpeed = 100f;     // world units per second to move
    private float m_cameraRotationSpeed = 0.1f;     // degrees to rotate
    
    /// <summary>
    /// Constructor called in instance of main and not in own thread. This is only
    /// good for setting up structures.
    /// </summary>
    public Viewer() {
    }

    #region IModule methods
    public override void OnLoad(string name, IAppParameters theParams) {
        m_moduleName = name;
        ModuleParams = theParams;
        ModuleParams.AddDefaultParameter(m_moduleName + ".World.Name", "World", "Name of world module to connect to");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Renderer.Name", "Renderer", "");
        // todo: make this variable so there can be multiple viewers

        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.Speed", "10", "Units per second to move camera");
        ModuleParams.AddDefaultParameter(m_moduleName + ".Camera.RotationSpeed", "100", "Thousandth of degrees to rotate camera");

        m_EntitySlot = EntityBase.AddAdditionSubsystem("VIEWER");
    }

    override public bool AfterAllModulesLoaded() {
        m_log.Log(LogLevel.DINIT, "entered AfterAllModulesLoaded()");

        Renderer = (IRenderProvider)ModuleManager.Module(ModuleParams.ParamString(m_moduleName + ".Renderer.Name"));
        if (Renderer == null) {
            m_log.Log(LogLevel.DBADERROR, "UNABLE TO LOAD RENDERER!!!! ");
            return false;
        }

        m_cameraSpeed = (float)ModuleParams.ParamInt(m_moduleName + ".Camera.Speed");
        m_cameraRotationSpeed = (float)ModuleParams.ParamInt(m_moduleName + ".Camera.RotationSpeed")/1000;
        m_mainCamera = new EntityCamera(null, null);
        // m_MainCamera.Position = new OMV.Vector3(128f, -192f, 90f); // from OpenGL code
        m_mainCamera.GlobalPosition = new OMV.Vector3d(0f, 20f, 30f);   // World coordinates (Z up)
        // camera starts pointing down Y axis
        m_mainCamera.InitDirection = new OMV.Vector3(0f, 1f, 0f);
        m_mainCamera.Heading = new OMV.Quaternion(OMV.Vector3.UnitY, 0f);
        m_mainCamera.Zoom = 1.0d;
        m_mainCamera.Far = 3000.0d;

        // connect me to the world so I can know when things change in the world
        TheWorld.OnWorldRegionConnected += new WorldRegionConnectedCallback(OnRegionConnected);
        TheWorld.OnWorldRegionChanging += new WorldRegionChangingCallback(OnRegionChanged);
        TheWorld.OnWorldEntityNew += new WorldEntityNewCallback(OnEntityNew);
        TheWorld.OnWorldEntityUpdate += new WorldEntityUpdateCallback(OnEntityUpdate);
        TheWorld.OnWorldEntityKilled += new WorldEntityKilledCallback(OnEntityKilled);
        TheWorld.OnWorldTerrainUpdated += new WorldTerrainUpdateCallback(OnTerrainUpdated);
        TheWorld.OnAgentNew += new WorldAgentNewCallback(OnAgentNew);
        TheWorld.OnAgentUpdate += new WorldAgentUpdateCallback(OnAgentUpdate);
        TheWorld.OnAgentRemoved += new WorldAgentRemovedCallback(OnAgentRemoved);

        m_log.Log(LogLevel.DINIT, "exiting AfterAllModulesLoaded()");
        return true;
    }

    override public void Start() {
        Renderer.UpdateCamera(m_mainCamera);

        // start getting IO stuff from the user
        Renderer.UserInterface.OnUserInterfaceKeypress += new UserInterfaceKeypressCallback(UserInterface_OnKeypress);
        Renderer.UserInterface.OnUserInterfaceMouseMove += new UserInterfaceMouseMoveCallback(UserInterface_OnMouseMove);

        // start the renderer
        ((IModule)Renderer).Start();

        m_log.Log(LogLevel.DINIT, "exiting Start()");
        return;
    }

    override public void Stop() {
        return;
    }
    #endregion IModule methods

    #region IViewProvider methods
    // Special kludge to pass the main execution thread to the renderer if it's the
    // the kind of renderer that needs the main event thread to work.
    // return true if 
    public bool RendererThreadEntry() {
        return m_Renderer.RendererThread();
    }
    #endregion IViewProvider methods

    private void OnEntityNew(IEntity ent) {
        // m_log.Log(LogLevel.DVIEWDETAIL, "OnEntityNew: Telling renderer about a new entity");
        Renderer.Render(ent);
    }

    private void OnNewFoliage(IEntity ent) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnNewFoliage: Telling renderer about a new foliage entity");
        return;
    }

    private void OnEntityUpdate(IEntity ent, World.UpdateCodes what) {
        if (ent is IEntityAvatar) {
            m_log.Log(LogLevel.DVIEWDETAIL, "OnEntityUpdate: Avatar. Reason={0}", World.World.UpdateCodeName(what));
        }
        else {
            m_log.Log(LogLevel.DVIEWDETAIL, "OnEntityUpdate: Other. Reason={0}", World.World.UpdateCodeName(what));
        }
        return;
    }

    private void OnEntityKilled(IEntity ent) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnEntityKilled: ");
        return;
    }

    /// <summary>
    /// Terrain has changed.
    /// </summary>
    /// <remarks>
    /// This is first attempt at terrain. The description of the land comes in
    /// as a heightmap defined by OMV. The renderer will have to deal with that.
    /// How to generalize this so it works for any world representation?
    /// What about a cylindrical spaceship world?
    /// </remarks>
    /// <param name="sim"></param>
    private void OnTerrainUpdated(RegionContextBase reg) {
        // m_log.Log(LogLevel.DVIEWDETAIL, "OnTerrainUpdated: ");
        Renderer.UpdateTerrain(reg);
        return;
    }

    // When a region is connected, one job is to map it into the view.
    // Chat with the renderer to enhance the rcontext with mapping info
    private void OnRegionConnected(RegionContextBase rcontext) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnRegionConnected: ");
        Renderer.MapRegionIntoView(rcontext);
        return;
    }

    private void OnRegionChanged(IRegionContext rcontext) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnRegionChanged: ");
        // clean up the old lists -- all new stuff is coming
        return;
    }

    // called from the renderer when the mouse moves
    private void UserInterface_OnMouseMove(int param, float x, float y) {
        float sinceLastMouse = (float)System.DateTime.UtcNow.Subtract(m_lastMouseMoveTime).Milliseconds;
        // m_log.Log(LogLevel.DVIEWDETAIL, "OnMouseMove:"
        //             + " x=" + x.ToString() + ", y=" + y.ToString()
        //             + "time since last = " + sinceLastMouse.ToString() );
        if (m_mainCamera != null) {
            if ( ((Renderer.UserInterface.LastKeyCode & Keys.Control) != 0)
                    && ((Renderer.UserInterface.LastKeyCode & Keys.Alt) != 0) ) {
                // if ALT+CNTL is held down, movement is on view plain
                float xMove = x * m_cameraSpeed;
                float yMove = y * m_cameraSpeed;
                OMV.Vector3d movement = new OMV.Vector3d(
                            0,
                            (double)xMove, 
                            (double)yMove);
                m_mainCamera.GlobalPosition -= movement;
            }
            else if ((Renderer.UserInterface.LastKeyCode & Keys.Control) != 0) {
                // if CNTL is held down, movement is on land plane
                float xMove = x * m_cameraSpeed;
                float yMove = y * m_cameraSpeed;
                OMV.Vector3d movement = new OMV.Vector3d(
                            (double)yMove,
                            (double)xMove, 
                            (double)0);
                m_mainCamera.GlobalPosition -= movement;
            }
            else {
                // move the camera around the horizontal (X) and vertical (Z) axis
                float xMove = (-x * m_cameraRotationSpeed * Constants.DEGREETORADIAN) % Constants.TWOPI;
                float yMove = (-y * m_cameraRotationSpeed * Constants.DEGREETORADIAN) % Constants.TWOPI;
                // rotate around local axis
                m_mainCamera.rotate(yMove, 0f, xMove);
            }
            m_Renderer.UpdateCamera(m_mainCamera);
        }
        return;
    }

    // callsed from the renderer when the state of the keyboard changes
    private void UserInterface_OnKeypress(Keys key, bool updown) {
        if (key == (Keys.Control | Keys.C) ) Globals.KeepRunning = false;
        if (key == Keys.Escape) {
            // force the camera to the client position
            if (m_trackedAgent != null) {
                m_log.Log(LogLevel.DVIEWDETAIL, "OnKeyPress: ESC: restoring camera position");
                m_mainCamera.GlobalPosition = m_trackedAgent.GlobalPosition;
                m_Renderer.UpdateCamera(m_mainCamera);
            }
        }
        return;
    }

    // When an agent is added to the scene
    // At the moment we don't have good control for associating an agent with the viewer.
    // Assume the last agent is the one we are tracking.
    private void OnAgentNew(IAgent agnt) {
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

    private void OnAgentUpdate(IAgent agnt, UpdateCodes what) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentUpdate: ");
        return;
    }

    private void OnAgentRemoved(IAgent agnt) {
        m_log.Log(LogLevel.DVIEWDETAIL, "OnAgentRemoved: ");
        return;
    }


}
}
