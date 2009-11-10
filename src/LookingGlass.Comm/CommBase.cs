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
using LookingGlass;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Parameters;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Comm {
    /// <summary>
    /// Someday abstract out the basic object conversion logic (creation of
    /// agents, ...) into this base routine and let the comm specific stuff
    /// be in a subclass of this base.
    /// This class would have no comm (LLLP) or virtual world (LL) specific
    /// code in it. This classes sole purpose is to provide the bridge
    /// classes between comm and LookingGlass.World.
    /// for
    /// </summary>
public class CommBase /*: ICommProvider*/ {
    /*
    string Name { get; }
    
    bool IsConnected { get; }

    bool IsLoggedIn { get; }

    bool Connect(ParameterSet parms);

    bool Disconnect();

    // initiate a connection
    ParameterSet ConnectionParams { get; }

    // kludge to get underlying LL Comm (circular ref Comm.LLLP <=> World.LL)
    OMV.GridClient GridClient { get; }

    // each comm provider has a block of statistics
    ParameterSet CommStatistics();
     */
}
}
