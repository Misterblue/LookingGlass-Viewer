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
    private bool m_shouldPreMoveAvatar = true;
    private float m_rotFudge = 2f;      // degrees moved per rotation
    private float m_moveFudge = 0.4f;      // meters moved per movement
    private float m_flyFudge = 2.5f;      // meters moved per movement
    private float m_runFudge = 0.8f;      // meters moved per movement

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
        if (LookingGlassBase.Instance.AppParams.HasParameter("World.LL.Agent.PreMove.FlyFudge")) {
            m_flyFudge = LookingGlassBase.Instance.AppParams.ParamFloat("World.LL.Agent.PreMove.FlyFudge");
        }
        if (LookingGlassBase.Instance.AppParams.HasParameter("World.LL.Agent.PreMove.RunFudge")) {
            m_runFudge = LookingGlassBase.Instance.AppParams.ParamFloat("World.LL.Agent.PreMove.RunFudge");
        }
    }

    // The underlying data has been updated. Forget local things.
    public void DataUpdate(UpdateCodes what) {
        // Local values are set for dead-reconning but once we have official values, use  them
        // if ((what & UpdateCodes.Position) != 0) m_haveLocalPosition = false;
        if ((what & UpdateCodes.Rotation) != 0) m_haveLocalHeading = false;
    }

    #region MOVEMENT
    public void StopAllMovement() {
        m_client.Self.Movement.Stop = true;
    }

    public void MoveForward(bool startstop) {
        m_client.Self.Movement.AtPos = startstop;
        m_client.Self.Movement.SendUpdate();
        // TODO: test if running or flying and use other fudges
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                OMV.Vector3 newPos = m_myAvatar.LocalPosition +
                            new OMV.Vector3(CalcMoveFudge(), 0f, 0f) * m_myAvatar.Heading;
                m_log.Log(LogLevel.DWORLDDETAIL|LogLevel.DUPDATEDETAIL, "MoveForward: premove from {0} to {1}", 
                        m_myAvatar.LocalPosition.ToString(), newPos);
                this.LocalPosition = newPos;
                m_client.Self.RelativePosition = newPos;
                m_myAvatar.LocalPosition = newPos;
                m_myAvatar.Update(UpdateCodes.Position);
            }
        }
        // updates to the server are sent automatically by the movement framework
    }

    public void MoveBackward(bool startstop) {
        m_client.Self.Movement.AtNeg = startstop;
        m_client.Self.Movement.SendUpdate();
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                OMV.Vector3 newPos = m_myAvatar.LocalPosition +
                            new OMV.Vector3(-CalcMoveFudge(), 0f, 0f) * m_myAvatar.Heading;
                m_log.Log(LogLevel.DWORLDDETAIL|LogLevel.DUPDATEDETAIL, "MoveBackward: premove from {0} to {1}", 
                        m_myAvatar.LocalPosition.ToString(), newPos);
                this.LocalPosition = newPos;
                m_myAvatar.LocalPosition = newPos;
                m_client.Self.RelativePosition = newPos;
                m_myAvatar.Update(UpdateCodes.Position);
            }
        }
    }

    public void MoveUp(bool startstop) {
        m_client.Self.Movement.UpPos = startstop;
        m_client.Self.Movement.SendUpdate();
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                this.LocalPosition = m_myAvatar.LocalPosition + new OMV.Vector3(0f, 0f, CalcMoveFudge());
                m_myAvatar.LocalPosition = this.LocalPosition;
                m_client.Self.RelativePosition = this.LocalPosition;
                m_myAvatar.Update(UpdateCodes.Position);
            }
        }
    }

    public void MoveDown(bool startstop) {
        m_client.Self.Movement.UpNeg = startstop;
        m_client.Self.Movement.SendUpdate();
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                this.LocalPosition = m_myAvatar.LocalPosition + new OMV.Vector3(0f, 0f, -CalcMoveFudge());
                m_myAvatar.LocalPosition = this.LocalPosition;
                m_client.Self.RelativePosition = this.LocalPosition;
                m_myAvatar.Update(UpdateCodes.Position);
            }
        }
    }

    public void Fly(bool startstop) {
        if (startstop) {
            // flying is modal. If we're flying, stop.
            m_client.Self.Movement.Fly = !m_client.Self.Movement.Fly;
            m_client.Self.Movement.SendUpdate();
        }
    }

    public void TurnLeft(bool startstop) {
        m_client.Self.Movement.TurnLeft = startstop;
        if (startstop) {
            OMV.Quaternion Zturn = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, Constants.PI / 180 * m_rotFudge);
            Zturn.Normalize();
            m_client.Self.Movement.BodyRotation = OMV.Quaternion.Normalize(m_client.Self.Movement.BodyRotation* Zturn);
            m_client.Self.Movement.HeadRotation = OMV.Quaternion.Normalize(m_client.Self.Movement.HeadRotation* Zturn);
            //m_client.Self.LocalPosition += Zturn;
        }
        m_client.Self.Movement.SendUpdate();
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                this.Heading = m_client.Self.Movement.BodyRotation;
                m_myAvatar.Heading = m_client.Self.Movement.BodyRotation;
                m_myAvatar.Update(UpdateCodes.Rotation);
                m_log.Log(LogLevel.DWORLDDETAIL | LogLevel.DUPDATEDETAIL, "TurnLeft: premove to {0}", 
                    m_client.Self.Movement.BodyRotation);
            }
        }
    }

    public void TurnRight(bool startstop) {
        m_client.Self.Movement.TurnRight = startstop;
        if (startstop) {
            OMV.Quaternion Zturn = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, -Constants.PI / 180 * m_rotFudge);
            Zturn.Normalize();
            m_client.Self.Movement.BodyRotation = OMV.Quaternion.Normalize(m_client.Self.Movement.BodyRotation* Zturn);
            m_client.Self.Movement.HeadRotation = OMV.Quaternion.Normalize(m_client.Self.Movement.HeadRotation* Zturn);
            // m_client.Self.LocalPosition += Zturn;
        }
        // Send the movement (the turn) to the simulator. The rotation above will be corrected by the simulator
        m_client.Self.Movement.SendUpdate();
        // if we are to move the avatar when the user commands movement, push the avatar
        if (startstop && m_shouldPreMoveAvatar) {
            if (m_myAvatar != null) {
                this.Heading = m_client.Self.Movement.BodyRotation;
                m_log.Log(LogLevel.DWORLDDETAIL | LogLevel.DUPDATEDETAIL, "TurnRight: premove to {0}", 
                    m_client.Self.Movement.BodyRotation);
                m_myAvatar.Heading = m_client.Self.Movement.BodyRotation;
                // This next call sets off a tricky calling sequence:
                // LLEntityAvatar.Update
                //    calls LLEntityBase.Update
                //        calls EntityBase.Update
                //            (entity updates percolate up to the entity's container)
                //            calls RegionContext.UpdateEntity
                //                calls RegionContextBase.UpdateEntity
                //                    fires RegionContextBase.OnEntityUpdate
                //                        (World subscribes to the region entity container to get updates)
                //                        calls World.Region_OnEntityUpdate
                //                            fires World.OnEntityUpdate
                //                                (Viewer subscribes to entity updates to update display)
                //                                calls Viewer.World_OnEntityUpdate
                //                                    calls Renderer.RenderUpdate
                //                                        updates entity's pos and rot on screen
                //    calls World.Instance.UpdateAgent
                //        fires World.OnAgentUpdate
                //            calls Viewer.World_OnAgentUpdate
                //                (the camera follows the agent which just happens to be the avatar)
                //                calls mainCameraUpdate(with agent pos and rot)
                //                    fires CameraControl.OnCameraUpdate
                //                        (camera changes show up on the screen)
                //                        calls Renderer.UpdateCamera
                //                            calls into renderer to update view camera position
                //                        (camera changes are sent to simulator for culling optimizations)
                //                        calls Viewer.CameraControl_OnCameraUpdate
                //                            calls LLAgent.UpdateCamera
                //                                sends camera interest info (pos and rot) to simulator
                m_myAvatar.Update(UpdateCodes.Rotation);
            }
        }
    }

    private float CalcMoveFudge() {
        // TODO: test if client is running or flying and return the correct fudge
        return m_moveFudge;
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

    private OMV.Vector3 m_localPosition;
    public OMV.Vector3 LocalPosition {
        get {
            if (AssociatedAvatar != null) {
                m_localPosition = m_client.Self.SimPosition;
                return m_client.Self.SimPosition;
            }
            return m_localPosition;
        }
        set {
            m_localPosition = value;
        }
    }

    public OMV.Vector3 RegionPosition {
        get {
            return this.LocalPosition;
        }
    }

    public OMV.Vector3d GlobalPosition {
        get {
            return m_client.Self.GlobalPosition;
            // return AssociatedAvatar.RegionContext.CalculateGlobalPosition(RelativePosition);
        }
    }
    #endregion POSITION

    #region INTEREST
    /// <summary>
    /// Called when the camera is updated so we can tell the simulator where our camera is
    /// and it can do any optimizations based thereon.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="direction"></param>
    /// <param name="far"></param>
    public void UpdateCamera(OMV.Vector3d position, OMV.Quaternion direction, float far) {
        float roll;
        float pitch;
        float yaw;
        direction.GetEulerAngles(out roll, out pitch, out yaw);
        OMV.Vector3 pos = new OMV.Vector3((float)position.X, (float)position.Y, (float)position.Z);
        m_client.Self.Movement.Camera.SetPositionOrientation(pos, roll, pitch, yaw);
        m_client.Self.Movement.Camera.Far = far;
        m_log.Log(LogLevel.DVIEWDETAIL, "UpdateCamera: {0}, {1}, {2}, {3}", pos.X, pos.Y, pos.Z, direction.ToString());
        return;
    }

    public void UpdateInterest(int interest) {
        return;
    }
    #endregion INTEREST
}
}
