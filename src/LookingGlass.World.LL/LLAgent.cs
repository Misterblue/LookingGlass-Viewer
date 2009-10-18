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

    // if 'true', move avatar when we get the outgoing command to move the agent
    private bool m_shouldPreMoveAvatar = false;
    private float m_rotFudge = 5f;      // degrees moved per rotation
    private float m_moveFudge = 0.5f;      // meters moved per movement

    private IEntityAvatar m_myAvatar = null;
    public IEntityAvatar AssociatedAvatar {
        get {
            return m_myAvatar;
        }
        set {
            m_myAvatar = value;
        }
    }

    public LLAgent(OMV.GridClient theClient) {
        m_client = theClient;
        if (LookingGlassBase.Instance.AppParams.HasParameter("World.LL.Agent.PreMoveAvatar")) {
            m_shouldPreMoveAvatar = LookingGlassBase.Instance.AppParams.ParamBool("World.LL.Agent.PreMoveAvatar");
        }
        if (LookingGlassBase.Instance.AppParams.HasParameter("World.LL.Agent.PreMove.RotFudge")) {
            m_rotFudge = LookingGlassBase.Instance.AppParams.ParamFloat("World.LL.Agent.PreMove.RotFudge");
        }
        if (LookingGlassBase.Instance.AppParams.HasParameter("World.LL.Agent.PreMove.MoveFudge")) {
            m_moveFudge = LookingGlassBase.Instance.AppParams.ParamFloat("World.LL.Agent.PreMove.MoveFudge");
        }
    }

    // The underlying data has been updated. Forget local things.
    public void DataUpdate(UpdateCodes what) {
        if ((what & UpdateCodes.Position) != 0) m_haveLocalPosition = false;
        if ((what & UpdateCodes.Rotation) != 0) m_haveLocalHeading = false;
    }

    #region MOVEMENT
    public void StopAllMovement() {
        m_client.Self.Movement.Stop = true;
    }

    public void MoveForward(bool startstop) {
        m_client.Self.Movement.AtPos = startstop;
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                OMV.Vector3 newPos = m_myAvatar.RelativePosition +
                            new OMV.Vector3(m_moveFudge, 0f, 0f) * m_myAvatar.Heading;
                m_log.Log(LogLevel.DWORLDDETAIL, "MoveForward: premove from {0} to {1}", 
                        m_myAvatar.RelativePosition.ToString(), newPos);
                this.RelativePosition = newPos;
                m_myAvatar.RelativePosition = newPos;
                m_myAvatar.Update(UpdateCodes.Position);
            }
        }
        // updates to the server are sent automatically by the movement framework
    }

    public void MoveBackward(bool startstop) {
        m_client.Self.Movement.AtNeg = startstop;
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                OMV.Vector3 newPos = m_myAvatar.RelativePosition +
                            new OMV.Vector3(-m_moveFudge, 0f, 0f) * m_myAvatar.Heading;
                m_log.Log(LogLevel.DWORLDDETAIL, "MoveBackward: premove from {0} to {1}", 
                        m_myAvatar.RelativePosition.ToString(), newPos);
                this.RelativePosition = newPos;
                m_myAvatar.RelativePosition = newPos;
                m_myAvatar.Update(UpdateCodes.Position);
            }
        }
    }

    public void MoveUp(bool startstop) {
        m_client.Self.Movement.UpPos = startstop;
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                this.RelativePosition = m_myAvatar.RelativePosition + new OMV.Vector3(0f, 0f, m_moveFudge);
                m_myAvatar.RelativePosition = this.RelativePosition;
                m_myAvatar.Update(UpdateCodes.Position);
            }
        }
    }

    public void MoveDown(bool startstop) {
        m_client.Self.Movement.UpNeg = startstop;
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                this.RelativePosition = m_myAvatar.RelativePosition + new OMV.Vector3(0f, 0f, -m_moveFudge);
                m_myAvatar.RelativePosition = this.RelativePosition;
                m_myAvatar.Update(UpdateCodes.Position);
            }
        }
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
            OMV.Quaternion Zturn = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, Constants.PI / (180/m_rotFudge));
            Zturn.Normalize();
            m_client.Self.Movement.BodyRotation *= Zturn;
            m_client.Self.Movement.HeadRotation *= Zturn;
        }
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                this.Heading = m_client.Self.Movement.BodyRotation;
                m_myAvatar.Heading = m_client.Self.Movement.BodyRotation;
                m_myAvatar.Update(UpdateCodes.Rotation);
            }
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
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                this.Heading = m_client.Self.Movement.BodyRotation;
                m_myAvatar.Heading = m_client.Self.Movement.BodyRotation;
                m_myAvatar.Update(UpdateCodes.Rotation);
            }
        }
    }

    #endregion MOVEMENT

    #region POSITION
    private bool m_haveLocalHeading = false;
    private OMV.Quaternion m_heading;
    public OMV.Quaternion Heading {
        get {
            // kludge to allow the local agent to be different for dead reconning
            if (m_haveLocalHeading) {
                return m_heading;
            }
            return m_client.Self.SimRotation;
        }
        set {
            m_heading = value;
            m_haveLocalHeading = true;
        }
    }

    private bool m_haveLocalPosition = false;
    private OMV.Vector3 m_relativePosition;
    public OMV.Vector3 RelativePosition {
        get {
            if (m_haveLocalPosition) {
                return m_relativePosition;
            }
            return m_client.Self.SimPosition;
        }
        set {
            m_relativePosition = value;
            m_haveLocalPosition = true;
        }
    }   // position relative to RegionContext

    public OMV.Vector3d GlobalPosition {
        get {
            if (m_haveLocalPosition) {
                if (AssociatedAvatar != null) {
                    return AssociatedAvatar.RegionContext.CalculateGlobalPosition(m_relativePosition);
                }
            }
            return m_client.Self.GlobalPosition;
        }
        set {
            m_log.Log(LogLevel.DBADERROR, "GlobalPosition.set NOT IMPLEMENTED");
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
        m_log.Log(LogLevel.DVIEWDETAIL, "UpdateCamera: {0}, {1}, {2}, {3}", pos.X, pos.Y, pos.Z, direction.ToString());
        return;
    }

    public void UpdateInterest(int interest) {
        return;
    }
    #endregion INTEREST
}
}
