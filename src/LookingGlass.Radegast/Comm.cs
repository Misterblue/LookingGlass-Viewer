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
using System.Text;
using LookingGlass.Comm;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using Radegast;

namespace LookingGlass.Radegast {
    /// <summary>
    /// When LookingGlass is loaded as part of Radegast, all of the communication is done
    /// via that program. This module is loaded by LookingGlass.
    /// </summary>
public class Comm : ModuleBase, ICommProvider {

    public Comm() {
    }

    #region IModule methods

    public override void OnLoad(string name, LookingGlassBase lgbase) {
        base.OnLoad(name, lgbase);
        ModuleParams.AddDefaultParameter(ModuleName + ".Assets.CacheDir", 
                    Utilities.GetDefaultApplicationStorageDir(null),
                    "Filesystem location to build the texture cache");
    }

    public override bool AfterAllModulesLoaded() {
        return true;
    }

    public override void Start() {
        base.Start();
    }

    // If the base system says to stop, we make sure we're disconnected
    public override void Stop() {
        base.Stop();
    }

    public override bool PrepareForUnload() {
        base.PrepareForUnload();
        return true;
    }

    #endregion IModule methods

    #region ICommProvider methods
    protected bool m_isConnected = false;
    public bool IsConnected { get { return m_isConnected; } }

    protected bool m_isLoggedIn = false;
    public bool IsLoggedIn { get { return m_isLoggedIn; } }

    public bool Connect(ParameterSet parms) {
        return false;
    }

    public bool Disconnect() {
        return false;
    }

    // initiate a connection
    public ParameterSet ConnectionParams { get { return null; } }

    #endregion ICommProvider methods
}
}
