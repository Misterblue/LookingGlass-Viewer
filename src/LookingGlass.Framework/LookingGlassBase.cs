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
using System.Windows.Forms;
using System.Text;
using System.Threading;
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;

namespace LookingGlass {
public class LookingGlassBase : IInstance<LookingGlassBase> {
    // static things that are accessed very early (mostly by logger)
    private static string m_applicationName = "LookingGlass";
    public static string ApplicationName { get { return m_applicationName; } }
    private static string m_applicationVersion = "V0.4.6";
    public static string ApplicationVersion { get { return m_applicationVersion; } }

    ILog m_log = LogManager.GetLogger("LookingGlassBase");

    private static LookingGlassBase m_instance = null;
    public static LookingGlassBase Instance {
        get {
            if (m_instance == null) {
                throw new LookingGlassException("FETCH OF LOOKINGGLASSBASE INSTANCE BEFORE SET!!!!");
            }
            return m_instance;
        }
    }
    
    public LookingGlassBase() {
        m_instance = this;
        AppParams = new AppParameters();
        // The MaxValue causes everything to be written. When done debugging (ha!), reduce to near zero.
        AppParams.AddDefaultParameter("Log.FilterLevel", ((int)LogLevel.DNONDETAIL).ToString(),
                    "Default, initial logging level");
    }

    /// <summary>
    /// True if everything should keep running. Anything can set this to 'false'.
    /// Once set, the main control will call Stop() on everything and shut it all
    /// down.
    /// </summary>
    private bool m_keepRunning = false;
    public bool KeepRunning { 
        get { return m_keepRunning; } 
        set { m_keepRunning = value; } 
    }

    private ModuleManager m_moduleManager = null;
    public ModuleManager ModManager {
        get {
            if (m_moduleManager == null) {
                m_moduleManager = new ModuleManager(this);
            }
            return m_moduleManager;
        }
    }

    /// <summary>
    /// A kludge that allows passing of an out-of-context manager. If you are going to use
    /// this for anything other that Radegast you better know what you're doing.
    /// </summary>
    private Object m_otherManager = null;
    public Object OtherManager {
        get { return m_otherManager; }
        set { m_otherManager = value; }
    }
    

    /// <summary>
    /// All of the parameters for the applicaiton are saved in this stucture
    /// </summary>
    private AppParameters m_configuration = null;
    public AppParameters AppParams { get { return m_configuration; } set { m_configuration = value; } }
    public void ReadConfigurationFile() {
        // if anything goes wrong, just throw exception
        IParameterPersist ipp = (IParameterPersist)m_configuration;
        ipp.ReadParameterPersist();
    }
    public void WriteConfigurationFile() {
        // if anything goes wrong, just throw exception
        IParameterPersist ipp = (IParameterPersist)m_configuration;
        ipp.WriteParameterPersist();
    }

    /// <summary>
    /// Once configuration parameters are in place, this call will cause modules to
    /// be loaded and all the infrastructure to configure itself.
    /// </summary>
    /// <returns>'false' if everything didn't initialize. Shouldn't keep running</returns>
    public bool Initialize() {
        KeepRunning = true;
        m_log.Log(LogLevel.DINIT, "======= {0} {1}", ApplicationName, ApplicationVersion);

        try {
            if (!ModManager.LoadAndStartModules()) {
                m_log.Log(LogLevel.DBADERROR, "Failed starting modules");
                KeepRunning = false;
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Failed starting modules: " + e.ToString());
            KeepRunning = false;
        }

        if (KeepRunning) {
            m_log.Log(LogLevel.DINIT, "Completed main module startup");
            System.Threading.ThreadPool.SetMaxThreads(200, 1000);
        }
        else {
            m_log.Log(LogLevel.DBADERROR, "STARTEVERYTHING FAILED. NOT RUNNING");
        }
        return KeepRunning;
    }

    /// <summary>
    /// Start a thread to wait for the keep running flag to turn off
    /// </summary>
    private Thread m_KeepRunningThread = null;
    private ApplicationContext m_formsContext;
    public void Start(ApplicationContext formsContext) {
        m_formsContext = formsContext;
        if (m_KeepRunningThread == null) {
            m_KeepRunningThread = new Thread(CheckKeepRunning);
            m_KeepRunningThread.Name = "Check keep running";
            m_log.Log(LogLevel.DINIT, "Starting keep running thread");
            m_KeepRunningThread.Start();
        }
    }

    /// <summary>
    /// Hang around here waiting for the application to stop then make
    /// sure everything is stopped and shutdown.
    /// </summary>
    private void CheckKeepRunning() {
        while (this.KeepRunning) {
            Thread.Sleep(1 * 1000);
        }
        m_log.Log(LogLevel.DINIT, "KEEPRUNNING OFF. SHUTTING DOWN");
        StopEverything();
    }
    
    public void Stop() {
        // this causes all threads things to shutdown
        KeepRunning = false;
        Thread.Sleep(3 * 1000);
        StopEverything();
    }

    public delegate bool MainThreadCallback();
    protected MainThreadCallback m_wantsMainThread = null;
    public bool GetMainThread(MainThreadCallback mtc) {
        m_wantsMainThread = mtc;
        return true;
    }

    private void StopEverything() {
        try {
            m_log.Log(LogLevel.DINIT, "STOP INITIATED. Stopping forms.");
            m_log.Log(LogLevel.DINIT, "Stopping modules.");
            ModManager.Stop();

            if (m_formsContext != null) {
                m_log.Log(LogLevel.DINIT, "Stopping forms thread");
                m_formsContext.ExitThread();
            }

            m_log.Log(LogLevel.DINIT, "Unloading modules.");
            ModManager.PrepareForUnload();

            m_log.Log(LogLevel.DINIT, "Pushing out configuration file if needed.");
            WriteConfigurationFile();
        }
        catch (Exception e) {
            // we don't know how bad things got while shutting down..
            // just exit gracefully
            m_log.Log(LogLevel.DINIT, "EXCEPTION WHILE SHUTTING DOWN: "+e.ToString());
        }
    }
}
}
