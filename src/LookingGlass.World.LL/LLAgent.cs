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
using LookingGlass;
using LookingGlass.Framework.Logging;
using LookingGlass.World;
using OMV = OpenMetaverse;

namespace LookingGlass.World.LL {

public class LLAgent : IAgent {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    public event AgentUpdatedCallback OnAgentUpdated;

    // since agents and avatars are so intertwined in LLLP, we just get a handle
    //   back to the real controller
    private OMV.GridClient m_client = null;

    public LLAgent(OMV.GridClient theClient) {
        m_client = theClient;
    }

    #region MOVEMENT
    public void MoveForward() {
        m_client.Self.Movement.AtPos = true;
        m_client.Self.Movement.SendUpdate();
    }

    public void MoveBackward() {
        m_client.Self.Movement.AtNeg = true;
        m_client.Self.Movement.SendUpdate();
    }

    public void TurnLeft() {
        m_client.Self.Movement.TurnLeft = true;
        m_client.Self.Movement.SendUpdate();
    }

    public void TurnRight() {
        m_client.Self.Movement.TurnRight = true;
        m_client.Self.Movement.SendUpdate();
    }

    #endregion MOVEMENT

    #region POSITION
    public OMV.Quaternion Heading {
        get {
            return m_client.Self.SimRotation;
        }
        set {
            m_log.Log(LogLevel.DBADERROR, "LLAgent.Heading.set: NOT IMPLEMENTED");
        }
    }

    public OMV.Vector3 RelativePosition {
        get {
            return m_client.Self.SimPosition;
        }
        set {
            m_log.Log(LogLevel.DBADERROR, "LLAgent.RelativePosition.set: NOT IMPLEMENTED");
        }
    }   // position relative to RegionContext

    public OMV.Vector3d GlobalPosition {
        get {
            return m_client.Self.GlobalPosition;
        }
        set {
            m_log.Log(LogLevel.DBADERROR, "LLAgent.GlobalPosition.set: NOT IMPLEMENTED");
        }
    }
    #endregion POSITION

    public void UpdateCamera(OMV.Vector3d position, OMV.Quaternion direction) {
        return;
    }

    public void UpdateInterest(int interest) {
        return;
    }
}
}
