/* Copyright 2008 (c) Robert Adams
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
using System.IO;
using System.Text;
using System.Windows.Forms;
using LookingGlass.Comm.LLLP;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Parameters;
using LookingGlass.Radegast;
using OMV = OpenMetaverse;
using Radegast;

namespace LookingGlass {

class RadegastMain : IRadegastPlugin {
    public static RadegastInstance RadInstance = null;
    private ILog m_log = null;

    private LookingGlass.LookingGlassBase m_lgb = null;
    private RadegastWindow m_viewDialog = null;

    public RadegastMain() {
    }

    #region IRadegastPlugin methods

    public void StartPlugin(RadegastInstance inst) {
        m_lgb = new LookingGlassBase();
        m_lgb.OtherManager = inst;

        m_log = LogManager.GetLogger("RadegastMain");

        m_log.Log(LogLevel.DRADEGAST, "StartPlugin()");
        RadInstance = inst;

        // put the menu entry in. Clicking will initialize and run LookingGlass
        ToolStripMenuItem menuItem = new ToolStripMenuItem("LookingGlass", null, new EventHandler(startLGView));
        RadInstance.MainForm.ToolsMenu.DropDownItems.Add(menuItem);

        // make it so libomv decodes the sim terrain so when LG opens, terrain is there
        RadInstance.Client.Settings.STORE_LAND_PATCHES = true;
    }

    // This code follows the form of the Main.cs code in setting up parameters then
    // initialzing and running.
    // Change one, be sure to change the other.
    public void startLGView(Object parm, EventArgs args) {
        m_log.Log(LogLevel.DRADEGAST, "startLGView()");
        // force a new default parameter files to Radegast specific ones 
        m_lgb.AppParams.AddDefaultParameter("Settings.File", 
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "RadegastLookingGlass.json"),
            "New default for running under Radecast");
        m_lgb.AppParams.AddDefaultParameter("Settings.Modules", 
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "RadegastModules.json"),
            "Modules configuration file");

        // Point the Ogre renderer to our panel window
        // Ya, ya. I know it's RendererOgre specific. Fix that someday.
        RadegastWindow m_viewDialog = new RadegastWindow(RadInstance, m_lgb);
        Control[] subControls = m_viewDialog.Controls.Find("LGWindow", true);
        if (subControls.Length == 1) {
            Control windowPanel = subControls[0];
            string wHandle = windowPanel.Handle.ToString();
            m_log.Log(LogLevel.DRADEGASTDETAIL, "Connecting to external window {0}, w={1}, h={2}",
                wHandle, windowPanel.Width, windowPanel.Height);
            m_lgb.AppParams.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Handle",
                windowPanel.Handle.ToString(),
                "The window handle to use for our rendering");
            m_lgb.AppParams.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Width",
                windowPanel.Width.ToString(), "width of external window");
            m_lgb.AppParams.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Height",
                windowPanel.Height.ToString(), "Height of external window");
        }
        else {
            throw new Exception("Could not find window control on dialog");
        }

        try {
            m_lgb.ReadConfigurationFile();
        }
        catch (Exception e) {
            throw new Exception("Could not read configuration file: " + e.ToString());
        }

        // log level after all the parameters have been set
        LogManager.CurrentLogLevel = (LogLevel)m_lgb.AppParams.ParamInt("Log.FilterLevel");

        // cause LookingGlass to load all it's modules
        if (!m_lgb.Initialize()) {
            m_log.Log(LogLevel.DRADEGASTDETAIL, "RadegastMain: Failed LookingGlass initialization. Bailing");
            return;
        }
        m_log.Log(LogLevel.DRADEGASTDETAIL, "RadegastMain: Completed LookingGlass initialization");

        string radCommName = m_lgb.AppParams.ParamString("Radegast.Comm.Name");
        CommLLLP worldComm = (CommLLLP)m_lgb.ModManager.Module(radCommName);

        // this sets up a bunch of structures and naming
        m_log.Log(LogLevel.DRADEGASTDETAIL, "RadegastMain: Network_OnLogin");
        worldComm.Network_OnLogin(OMV.LoginStatus.Success, "Radegast prelogin");
        m_log.Log(LogLevel.DRADEGASTDETAIL, "RadegastMain: Network_OnSimConnected for {0}",
                        RadInstance.Client.Network.CurrentSim.Name);
        worldComm.Network_OnSimConnected(RadInstance.Client.Network.CurrentSim);
        // if anything was queue for this sim, put them in the world
        LoadWorldObjects.LoadASim(RadInstance.Client.Network.CurrentSim, RadInstance.Client, worldComm);

        // copy the objects that are already in the comm layer into the world
        LoadWorldObjects.Load(RadInstance.Client, worldComm);
            
        // initialize the viewer dialog
        m_viewDialog.Initialize();

        // put the dialog up
        m_viewDialog.Show();

        // The dialog window will do all the image updating
    }

    public void StopPlugin(RadegastInstance inst) {
        m_log.Log(LogLevel.DRADEGAST, "StopPlugin()");
        if (m_lgb != null) {
            m_lgb.Stop();
            m_lgb = null;
        }
        if (m_viewDialog != null) {
            m_viewDialog.Refresh();
            m_viewDialog.Shutdown();
            m_viewDialog = null;
        }
    }

    #endregion IRadegastPlugin methods

}
}
