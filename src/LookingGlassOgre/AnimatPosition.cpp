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
// #include "StdAfx.h"
#include "Animat.h"
#include "AnimatPosition.h"
#include "LookingGlassOgre.h"
#include "RendererOgre.h"

namespace LG {

AnimatPosition::AnimatPosition() {
	this->SceneNodeName.clear();
};
AnimatPosition::AnimatPosition(Ogre::String sNodeName, Ogre::Vector3 newPos, float durationSeconds) {
	this->SceneNodeName = sNodeName;
	this->AnimatType = AnimatTypePosition;

	Ogre::SceneNode* node = LG::RendererOgre::Instance()->m_sceneMgr->getSceneNode(this->SceneNodeName);
	m_originalPosition = node->getPosition();
	m_targetPosition = newPos;
	m_durationSeconds = durationSeconds;
	m_distanceVector = m_targetPosition - m_originalPosition;
	m_progress = 0.0f;
};
AnimatPosition::~AnimatPosition() {
};

bool AnimatPosition::Process(float timeSinceLastFrame) {
	bool ret = true;
	float thisProgress = timeSinceLastFrame / m_durationSeconds;
	m_progress += thisProgress;
	try {
		Ogre::SceneNode* node = LG::RendererOgre::Instance()->m_sceneMgr->getSceneNode(this->SceneNodeName);
		if (node != NULL) {
			if (m_progress > 1.0f) {
				node->setPosition(m_targetPosition);
				ret = false;	// return false to say animation is done and should be deleted
			}
			else {
				node->setPosition(m_originalPosition + m_distanceVector * m_progress);
			}
		}
	}
	catch (...) {
		LG::Log("Animat::Process: exception getting scene node");
	}
	return ret;
} 


}