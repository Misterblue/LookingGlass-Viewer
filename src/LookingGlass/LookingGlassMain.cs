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
using System.Threading;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using LookingGlass.View;

namespace LookingGlass {
class LookingGlassMain {
    ILog m_log = LogManager.GetLogger("LookingGlass");

    public LookingGlassMain() {
    }

    public void Start() {
        Globals.KeepRunning = true;

        try {
            StartEverything();
        }
        catch {
            m_log.Log(LogLevel.DBADERROR, "STARTEVERYTHING FAILED. NOT RUNNING");
            Globals.KeepRunning = false;
        }

        System.Threading.ThreadPool.SetMaxThreads(100, 1000);
        // Some renderers (Mogre and Ogre, I'm looking at you) require the main thread to
        // do their rendering and window management. This kludge calls into the
        // viewer to give the main thread to the renderer. If the renderer doesn't
        // need it, the function returns 'false' and we just wait for things to
        // finish.
        // Thread m_renderingThread = new Thread(RunRenderer);
        // m_renderingThread.Start();
        if ( Globals.KeepRunning 
                && !((IViewProvider)ModuleManager.Module("Viewer")).RendererThreadEntry()
            ) {
            // wait until everything shuts down
            while (Globals.KeepRunning) {
                Thread.Sleep(1 * 1000);
            }
        }
        else {
            // renderer thread exited, we turn stuff off
            Globals.KeepRunning = false;
            Thread.Sleep(3 * 1000);
        }

        StopEverything();
    }

    public void Stop() {
        // this causes things to shutdown
        Globals.KeepRunning = false;
    }

    private void StartEverything() {
        try {
            if (!ModuleManager.LoadAndStartModules()) {
                m_log.Log(LogLevel.DBADERROR, "Failed starting modules");
                Globals.KeepRunning = false;
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Failed starting modules: " + e.ToString());
            Globals.KeepRunning = false;
        }
        m_log.Log(LogLevel.DINIT, "Completed main module startup");
    }

    private void StopEverything() {
        try {
            m_log.Log(LogLevel.DINIT, "STOP INITIATED. Stopping modules.");
            ModuleManager.Stop();

            m_log.Log(LogLevel.DINIT, "Unloading modules.");
            ModuleManager.PrepareForUnload();

            m_log.Log(LogLevel.DINIT, "Pushing out configuration file if needed.");
            Globals.WriteConfigurationFile();
        }
        catch (Exception e) {
            // we don't know how bad things got while shutting down..
            // just exit gracefully
            m_log.Log(LogLevel.DINIT, "EXCEPTION WHILE SHUTTING DOWN: "+e.ToString());
        }
    }
}
}
