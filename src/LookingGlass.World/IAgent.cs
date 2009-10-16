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
using OMV = OpenMetaverse;

namespace LookingGlass.World {

public delegate void AgentUpdatedCallback(IAgent agnt, UpdateCodes what);

/// <summary>
/// The user acts on the world as an 'agent'. There is often an avatar
/// associated with the agent (agent movement commands turn into movement
/// of an avatar) but this is not required.
/// </summary>
public interface IAgent {
    event AgentUpdatedCallback OnAgentUpdated;

    #region MOVEMENT
    // This also updates the agent's representation in the world (usually an avatar)
    // TODO: this is just enough to get display working. Figure out better movement model
    void MoveForward(bool startstop);
    void MoveBackward(bool startstop);
    void MoveUp(bool startstop);
    void MoveDown(bool startstop);
    void TurnLeft(bool startstop);
    void TurnRight(bool startstop);
    void Fly(bool startstop);
    void StopAllMovement();
    #endregion MOVEMENT

    #region POSITION
    OMV.Quaternion Heading { get; set; }
    OMV.Vector3 RelativePosition { get; set; }   // position relative to RegionContext
    OMV.Vector3d GlobalPosition { get; }
    #endregion POSITION

    // there is a binding between the agent in the world and their representation
    IEntityAvatar AssociatedAvatar { get; set; }

    // This is a call from the viewer telling the agent the camera has moved. The
    // agent can use this for anything it wishes but it's mostly used by data sources
    // to generate culling or update ordering.
    void UpdateCamera(OMV.Vector3d position, OMV.Quaternion direction);

    // A number between 0..10 which give hints as to the user's interaction with the viewer.
    // Can be used by the agent to control update frequency and LOD.
    // Since stupid C# doesn't allow me to define constants in an interface
    //   definition, here are some values:
    // enum Interest {
    //     None = 0,     // user just doesn't care
    //     NoFocus = 2,  // viewer window does not have focus
    //     Idle = 6,     // user has been idle for a period
    //     Most = 10,    // user is active and doing stuff
    // };
    void UpdateInterest(int interest);
}
}
