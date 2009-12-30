/* Copyright (c) 2008 Robert Adams
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
using System.Windows.Forms;

namespace LookingGlass.View {
public class ControlViews : IControlViewProvider, IModule {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    FormAvatars m_avatarView;
    ViewChat m_chatView;
    ViewWindow m_viewWindow;

#region IMODULE
    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected LookingGlassBase m_lgb = null;
    public LookingGlassBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    public ControlViews() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }

    // IModule.OnLoad
    public virtual void OnLoad(string modName, LookingGlassBase lgbase) {
        m_log.Log(LogLevel.DINIT, "ControlViews.OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;

        // some of these parameters are overridden early. Add defaults if not overridden
        if (!LGB.AppParams.HasParameter(ModuleName + ".WorldView.Enable")) {
            LGB.AppParams.AddDefaultParameter(ModuleName + ".WorldView.Enable", "true",
                "Default action is to enable the view window");
        }
        if (!LGB.AppParams.HasParameter(ModuleName + ".AvatarView.Enable")) {
            LGB.AppParams.AddDefaultParameter(ModuleName + ".AvatarView.Enable", "true",
                "Default action is to enable the view window");
        }
        if (!LGB.AppParams.HasParameter(ModuleName + ".ChatView.Enable")) {
            LGB.AppParams.AddDefaultParameter(ModuleName + ".ChatView.Enable", "true",
                "Default action is to enable the view window");
        }

        if (LGB.AppParams.ParamBool(ModuleName + ".WorldView.Enable")) {
            // Point the Ogre renderer to our panel window
            // Ya, ya. I know it's RendererOgre specific. Fix that someday.
            m_viewWindow = new ViewWindow(LGB);
            Control[] subControls = m_viewWindow.Controls.Find("LGWindow", true);
            if (subControls.Length == 1) {
                Control windowPanel = subControls[0];
                string wHandle = windowPanel.Handle.ToString();
                m_log.Log(LogLevel.DRADEGASTDETAIL, "Connecting to external window {0}, w={1}, h={2}",
                    wHandle, windowPanel.Width, windowPanel.Height);
                LGB.AppParams.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Handle",
                    windowPanel.Handle.ToString(),
                    "The window handle to use for our rendering");
                LGB.AppParams.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Width",
                    windowPanel.Width.ToString(), "width of external window");
                LGB.AppParams.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Height",
                    windowPanel.Height.ToString(), "Height of external window");
            }
            else {
                m_log.Log(LogLevel.DBADERROR, "Could not find window control on dialog");
                throw new Exception("Could not find window control on dialog");
            }
        }

    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        m_log.Log(LogLevel.DINIT, "ControlViews.AfterAllModulesLoaded()");
        return true;
    }

    // IModule.Start
    public virtual void Start() {
        if (LGB.AppParams.ParamBool(ModuleName + ".WorldView.Enable")) {
            m_log.Log(LogLevel.DINIT, "ControlViews.Start(): Initializing ViewWindow");
            m_viewWindow.Initialize();
            m_viewWindow.Visible = true;
            m_viewWindow.Show();
        }

        if (LGB.AppParams.ParamBool(ModuleName + ".AvatarView.Enable")) {
            m_log.Log(LogLevel.DINIT, "ControlViews.Start(): Initializing FormAvatar");
            m_avatarView = new FormAvatars(LGB);
            m_avatarView.Initialize();
            m_avatarView.Visible = true;
        }

        if (LGB.AppParams.ParamBool(ModuleName + ".ChatView.Enable")) {
            m_log.Log(LogLevel.DINIT, "ControlViews.Start(): Initializing ViewChat");
            m_chatView = new ViewChat(LGB);
            m_chatView.Initialize();
            m_chatView.Visible = true;
        }
        return;
    }

    // IModule.Stop
    public virtual void Stop() {
        if (m_viewWindow != null) {
            m_log.Log(LogLevel.DINIT, "ControlViews.Stop(): Stopping ViewWindow");
            m_viewWindow.Shutdown();
            m_viewWindow = null;
        }
        if (m_avatarView != null) {
            m_log.Log(LogLevel.DINIT, "ControlViews.Stop(): Stopping FormAvatar");
            m_avatarView.Shutdown();
            m_avatarView = null;
        }
        if (m_chatView != null) {
            m_log.Log(LogLevel.DINIT, "ControlViews.Stop(): Stopping ViewChat");
            m_chatView.Shutdown();
            m_chatView = null;
        }
        return;
    }

    // IModule.PrepareForUnload
    public virtual bool PrepareForUnload() {
        return false;
    }
#endregion IMODULE
}
}
