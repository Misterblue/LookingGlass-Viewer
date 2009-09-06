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
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Parameters;
using LookingGlass.Radegast;
using Radegast;

namespace LookingGlass {

class RadegastMain : IRadegastPlugin {
    public static RadegastInstance RadInstance = null;
    private ILog m_log = null;

    LookingGlassMain m_lookingGlassInstance = null;


    public RadegastMain() {
        m_log = LogManager.GetLogger("RadegastMain");
        m_log.Log(LogLevel.DRADEGAST, "Entered constructor");
    }

    #region IRadegastPlugin methods

    public void StartPlugin(RadegastInstance inst) {
        m_log.Log(LogLevel.DRADEGAST, "StartPlugin()");
        RadInstance = inst;

        Globals.Configuration = new AppParameters();
        // The MaxValue causes everything to be written. When done debugging (ha!), reduce to near zero.
        Globals.Configuration.AddDefaultParameter("Log.FilterLevel", ((int)LogLevel.DNONDETAIL).ToString(),
                    "Default, initial logging level");


        // force a new default parameter files
        Globals.Configuration.AddDefaultParameter("Settings.File", 
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "RadegastLookingGlass.json"),
            "New default for running under Radecast");
        Globals.Configuration.AddDefaultParameter("Settings.Modules", 
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "RadegastModules.json"),
            "Modules configuration file");

        RadegastWindow viewDialog = new RadegastWindow();
        Control[] subControls = viewDialog.Controls.Find("LGWindow", true);
        if (subControls.Length == 1) {
            Control windowPanel = subControls[0];
            string wHandle = windowPanel.Handle.ToString();
            m_log.Log(LogLevel.DRADEGASTDETAIL, "Connecting to external window {0}, w={1}, h={2}",
                wHandle, windowPanel.Width, windowPanel.Height);
            Globals.Configuration.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Handle",
                windowPanel.Handle.ToString(),
                "The window handle to use for our rendering");
            Globals.Configuration.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Width",
                windowPanel.Width.ToString(), "width of external window");
            Globals.Configuration.AddDefaultParameter("Renderer.Ogre.ExternalWindow.Height",
                windowPanel.Height.ToString(), "Height of external window");
        }
        else {
            throw new Exception("Could not find window control on dialog");
        }

        try {
            Globals.ReadConfigurationFile();
        }
        catch (Exception e) {
            throw new Exception("Could not read configuration file: " + e.ToString());
        }

        // log level after all the parameters have been set
        LogManager.CurrentLogLevel = (LogLevel)Globals.Configuration.ParamInt("Log.FilterLevel");

        viewDialog.Show();

        m_lookingGlassInstance = new LookingGlassMain();

        // this causes LookingGlass to load all it's modules and get running.
        m_lookingGlassInstance.Start();
    }

    public void StopPlugin(RadegastInstance inst) {
        m_log.Log(LogLevel.DRADEGAST, "StopPlugin()");
        m_lookingGlassInstance.Stop();
    }

    #endregion IRadegastPlugin methods

}
}
