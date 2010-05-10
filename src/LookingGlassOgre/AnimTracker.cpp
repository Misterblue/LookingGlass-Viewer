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
	// LGLOCK_LOCK(m_animationsMutex);
	std::list<Animat*>::iterator li;
	for (li = m_animations.begin(); li != m_animations.end(); li++) {
		li._Ptr->_Myval->Process(evt.timeSinceLastFrame);
	}
	for (li = m_removeAnimations.begin(); li != m_removeAnimations.end(); li++) {
		Animat* anim = li._Ptr->_Myval;
		delete anim;
	}
	m_removeAnimations.clear();
	// LGLOCK_UNLOCK(m_animationsMutex);
	animLock.Unlock();
	return true;
}

void AnimTracker::RotateSceneNode(Ogre::String sceneNodeName, Ogre::Vector3 axis, float rate) {
	LGLOCK_ALOCK animLock;	// a lock that will be released if we have an exception
	LG::Log("AnimTracker::RotateSceneNode for %s", sceneNodeName.c_str());
	RemoveAnimations(sceneNodeName);
	animLock.Lock(m_animationsMutex);
	// LGLOCK_LOCK(m_animationsMutex);
	Animat* anim = new Animat(sceneNodeName);
	m_animations.push_back(anim);
	// LGLOCK_UNLOCK(m_animationsMutex);
	animLock.Unlock();
	anim->Rotation(axis, rate);
}

void AnimTracker::RemoveAnimations(Ogre::String sceneNodeName) {
	LGLOCK_ALOCK animLock;	// a lock that will be released if we have an exception
	animLock.Lock(m_animationsMutex);
	// LGLOCK_LOCK(m_animationsMutex);
	std::list<Animat*>::iterator li;
	for (li = m_animations.begin(); li != m_animations.end(); li++) {
		if (!li._Ptr->_Myval->SceneNodeName.empty()) {
			if (li._Ptr->_Myval->SceneNodeName == sceneNodeName) {
				m_animations.erase(li);
				m_removeAnimations.push_back(li._Ptr->_Myval);
			}
		}
	}
	// LGLOCK_UNLOCK(m_animationsMutex);
	animLock.Unlock();
}

// Called by an animation to say it is complete. This will cause the animat to
// be deleted after processing is complete.
void AnimTracker::AnimationComplete(Animat* anim) {
	m_removeAnimations.push_back(anim);
}

}
