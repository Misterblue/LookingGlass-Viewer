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
#include "StdAfx.h"
#include "LGOCommon.h"
#include "LookingGlassOgre.h"
#include "AnimTracker.h"
#include "Animat.h"
#include "AnimatFixedRotation.h"
#include "AnimatPosition.h"
#include "AnimatRotation.h"

namespace LG {
AnimTracker* AnimTracker::m_instance = NULL;

AnimTracker::AnimTracker() {
	m_animationsMutex = LGLOCK_ALLOCATE_MUTEX("AnimTracker");
	LG::GetOgreRoot()->addFrameListener(this);
};
AnimTracker::~AnimTracker() {
	LG::GetOgreRoot()->removeFrameListener(this);
};

// Between frame, update all the animations
bool AnimTracker::frameStarted(const Ogre::FrameEvent& evt) {
	LGLOCK_ALOCK animLock;	// a lock that will be released if we have an exception
	animLock.Lock(m_animationsMutex);

	std::list<Animat*>::iterator li;
	for (li = m_animations.begin(); li != m_animations.end(); li++) {
		try {
			if (!(*li)->Process(evt.timeSinceLastFrame)) {
				m_removeAnimations.push_back(*li);
			}
		}
		catch (...) {
			LG::Log("AnimTracker::frameStarted EXCEPTION calling Process on t=%d, s=%s", 
				(*li)->AnimatType, (*li)->SceneNodeName.c_str());
		}
	}

	// if any of the animations asked to be removed, remove them now
	// (done since we can't delete it out of the list while iterating to call Process())
	EmptyRemoveAnimationList();

	animLock.Unlock();
	return true;
}

// delete animations of a certain type for this scenenode
void AnimTracker::RemoveAnimations(Ogre::String sceneNodeName, int typ) {
	LGLOCK_ALOCK animLock;	// a lock that will be released if we have an exception
	animLock.Lock(m_animationsMutex);

	std::list<Animat*>::iterator li;
	for (li = m_animations.begin(); li != m_animations.end(); li++) {
		if ( !((*li)->SceneNodeName.empty()) ) {
			if ((typ == AnimatTypeAny) || ((*li)->AnimatType == typ)) {
				if ((*li)->SceneNodeName == sceneNodeName) {
					// m_animations.erase(li);
					m_removeAnimations.push_back(*li);
				}
			}
		}
	}
	EmptyRemoveAnimationList();
	animLock.Unlock();
}

// Delete all animations for this scene node
void AnimTracker::RemoveAnimations(Ogre::String sceneNodeName) {
	RemoveAnimations(sceneNodeName, AnimatTypeAny);
}

// note: assumes the list is protected by a lock
void AnimTracker::EmptyRemoveAnimationList() {
	std::list<Animat*>::iterator li;
	for (li = m_removeAnimations.begin(); li != m_removeAnimations.end(); li++) {
		Animat* anim = *li;
		m_animations.remove(anim);
		delete anim;
	}
	m_removeAnimations.clear();
}

// =======================================================================
// Do a fixed rotation at some rate around some axis
void AnimTracker::FixedRotationSceneNode(Ogre::String sceneNodeName, Ogre::Vector3 axis, float rate) {
	LG::Log("AnimTracker::RotateSceneNode for %s", sceneNodeName.c_str());
	LGLOCK_ALOCK animLock;	// a lock that will be released if we have an exception
	// Remove any outstanding animations of this type on this scenenode
	RemoveAnimations(sceneNodeName, AnimatTypeFixedRotation);
	animLock.Lock(m_animationsMutex);
	AnimatFixedRotation* anim = new AnimatFixedRotation(sceneNodeName, axis, rate);
	m_animations.push_back((Animat*)anim);
	animLock.Unlock();
}

// =======================================================================
void AnimTracker::MoveToPosition(Ogre::String sceneNodeName, Ogre::Vector3 newPos, float duration) {
	// LG::Log("AnimTracker::MoveToPosition for %s, d=%f", sceneNodeName.c_str(), duration);
	LGLOCK_ALOCK animLock;	// a lock that will be released if we have an exception
	// Remove any outstanding animations of this type on this scenenode
	RemoveAnimations(sceneNodeName, AnimatTypePosition);
	animLock.Lock(m_animationsMutex);
	AnimatPosition* anim = new AnimatPosition(sceneNodeName, newPos, duration);
	m_animations.push_back((Animat*)anim);
	animLock.Unlock();
}

// =======================================================================
void AnimTracker::Rotate(Ogre::String sceneNodeName, Ogre::Quaternion newRot, float duration) {
	// LG::Log("AnimTracker::MoveToPosition for %s, d=%f", sceneNodeName.c_str(), duration);
	LGLOCK_ALOCK animLock;	// a lock that will be released if we have an exception
	// Remove any outstanding animations of this type on this scenenode
	RemoveAnimations(sceneNodeName, AnimatTypeRotation);
	animLock.Lock(m_animationsMutex);
	AnimatRotation* anim = new AnimatRotation(sceneNodeName, newRot, duration);
	m_animations.push_back((Animat*)anim);
	animLock.Unlock();
}

}
