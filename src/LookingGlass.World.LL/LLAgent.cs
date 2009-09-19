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

# pragma warning disable 0067   // disable unused event warning
    public event AgentUpdatedCallback OnAgentUpdated;
# pragma warning restore 0067

    // since agents and avatars are so intertwined in LLLP, we just get a handle
    //   back to the real controller
    private OMV.GridClient m_client = null;

    public LLAgent(OMV.GridClient theClient) {
        m_client = theClient;
    }

    #region MOVEMENT
    public void StopAllMovement() {
        m_client.Self.Movement.Stop = true;
    }

    public void MoveForward(bool startstop) {
        m_client.Self.Movement.AtPos = startstop;
        // updates are sent automatically by the movement framework
    }

    public void MoveBackward(bool startstop) {
        m_client.Self.Movement.AtNeg = startstop;
    }

    public void MoveUp(bool startstop) {
        m_client.Self.Movement.UpPos = startstop;
    }

    public void MoveDown(bool startstop) {
        m_client.Self.Movement.UpNeg = startstop;
    }

    public void Fly(bool startstop) {
        if (startstop) {
            // flying is modal. If we're flying, stop.
            m_client.Self.Movement.Fly = !m_client.Self.Movement.Fly;
        }
    }

    public void TurnLeft(bool startstop) {
        m_client.Self.Movement.TurnLeft = startstop;
        if (startstop) {
            OMV.Quaternion Zturn = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, Constants.PI / 18);
            Zturn.Normalize();
            m_client.Self.Movement.BodyRotation *= Zturn;
            m_client.Self.Movement.HeadRotation *= Zturn;
        }
    }

    public void TurnRight(bool startstop) {
        m_client.Self.Movement.TurnRight = startstop;
        if (startstop) {
            OMV.Quaternion Zturn = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, -Constants.PI / 18);
            Zturn.Normalize();
            m_client.Self.Movement.BodyRotation *= Zturn;
            m_client.Self.Movement.HeadRotation *= Zturn;
        }
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

    #region INTEREST
    public void UpdateCamera(OMV.Vector3d position, OMV.Quaternion direction) {
        float roll;
        float pitch;
        float yaw;
        direction.GetEulerAngles(out roll, out pitch, out yaw);
        OMV.Vector3 pos = new OMV.Vector3((float)position.X, (float)position.Y, (float)position.Z);
        m_client.Self.Movement.Camera.SetPositionOrientation(pos, roll, pitch, yaw);
        m_client.Self.Movement.Camera.Far = 200f;
        // m_log.Log(LogLevel.DVIEWDETAIL, "UpdateCamera: {0}, {1}, {2}, {3}", pos.X, pos.Y, pos.Z, direction.ToString());
        return;
    }

    public void UpdateInterest(int interest) {
        return;
    }
    #endregion INTEREST
}
}
