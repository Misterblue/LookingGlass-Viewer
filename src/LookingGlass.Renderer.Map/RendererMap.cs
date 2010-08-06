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
using OMV = OpenMetaverse;

namespace LookingGlass.Renderer.Map {
    /// <summary>
    /// A renderer that will someday make map pages of the sim.
    /// At the moment it's just a null renderer.
    /// </summary>
public class RendererMap : IModule, IRenderProvider {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    #region IModule
    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected LookingGlassBase m_lgb = null;
    public LookingGlassBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    public RendererMap() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }

    // IModule.OnLoad
    public virtual void OnLoad(string modName, LookingGlassBase lgbase) {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;
    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".AfterAllModulesLoaded()");
        m_userInterface = new UserInterfaceNull();
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
        return;
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
