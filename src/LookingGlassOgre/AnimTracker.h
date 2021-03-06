/* Copyright (c) Robert Adams
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
#pragma once

#include "LGOCommon.h"
#include "SingletonInstance.h"
#include "Animat.h"
#include "LGLocking.h"

namespace LG {
	// Tracker for animations.
	// Routine keeps a list of the animations that are in progress and, once
	// a frame, calls their 'Process' routine. The animation's process routine
	// could call the 'AnimationComplete' method if it is finished. This will
	// delete the animation object after all the animations have been processed
	// (ie, solve the animation deleting itself problem).
class AnimTracker : Ogre::FrameListener, public SingletonInstance {
public:
	AnimTracker();
	~AnimTracker();

	static AnimTracker* Instance() { 
		if (LG::AnimTracker::m_instance == NULL) {
			LG::AnimTracker::m_instance = new AnimTracker();
		}
		return LG::AnimTracker::m_instance; 
	}

	// schedule a constant rotation to a scene node
	void FixedRotationSceneNode(Ogre::String sceneNodeName, Ogre::Vector3 axis, float rate);
	// schedule a move to a new position
	void MoveToPosition(Ogre::String sceneNodeName, Ogre::Vector3 newPos, float duration);
	// schedule a rotation
	void Rotate(Ogre::String sceneNodeName, Ogre::Quaternion newRot, float duration);

	// remove all animations associated with a scene node
	void RemoveAnimations(Ogre::String sceneNodeName);
	// remove all animations associated with a scene node of a specific type
	void RemoveAnimations(Ogre::String sceneNodeName, int typ);

	// Ogre::FrameListener
	bool frameStarted(const Ogre::FrameEvent&);

private:
	static AnimTracker* m_instance;

	LGLOCK_MUTEX m_animationsMutex;
	std::list<Animat*> m_animations;
	std::list<Animat*> m_removeAnimations;

	void EmptyRemoveAnimationList();
};
}