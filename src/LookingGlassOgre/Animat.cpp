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
#include "Animat.h"
#include "LookingGlassOgre.h"
#include "RendererOgre.h"
#include "AnimTracker.h"

namespace LG {

Animat::Animat() {
	this->SceneNodeName.clear();
};
Animat::Animat(Ogre::String sNodeName) {
	this->m_doingRotation = false;
	this->SceneNodeName = sNodeName;
};
Animat::~Animat() {
};

// start a rotation on a scene node
// A kludge rotation for LL which is specified by a vector where the direction
// of the vector is the axis to rotate around and the length of the vector
// is the radians per second to rotate.
void Animat::Rotation(float X, float Y, float Z) {
	Ogre::Vector3 axis = Ogre::Vector3(X, Y, Z);
	float rotPerSec = axis.length();	// rotation in radians per second
	rotPerSec = rotPerSec / Ogre::Math::TWO_PI;	// converted into rotations per second
	axis.normalise();
	this->Rotation(axis, rotPerSec);
}

void Animat::Rotation(Ogre::Vector3 axis, float rotationsPerSecond) {
	this->m_rotationScale = rotationsPerSecond;
	this->m_rotationAxis = axis;
	this->m_rotationLast = 0;
	this->m_doingFixedRotation = true;
	LG::Log("Animat::Rotation: setting rotation %f animation for %s", 
				(double)this->m_rotationScale, this->SceneNodeName.c_str());
	return;
}

void Animat::Rotation(Ogre::Quaternion from, Ogre::Quaternion to, float seconds) {
	return;
}

void Animat::Translate(Ogre::Vector3 from, Ogre::Vector3 to, float seconds) {
	return;
}

void Animat::Process(float timeSinceLastFrame) {
	if (m_doingFixedRotation) {
		float nextStep = this->m_rotationScale * timeSinceLastFrame;
		this->m_rotationLast += Ogre::Math::TWO_PI * nextStep;
		while (this->m_rotationLast >= Ogre::Math::TWO_PI) this->m_rotationLast -= Ogre::Math::TWO_PI;
		Ogre::Quaternion newRotation;
		newRotation.FromAngleAxis(Ogre::Radian(this->m_rotationLast), this->m_rotationAxis);
		try {
			Ogre::SceneNode* node = LG::RendererOgre::Instance()->m_sceneMgr->getSceneNode(this->SceneNodeName);
			node->setOrientation(newRotation);
		}
		catch (...) {
			LG::Log("Animat::Process: exception getting scene node");
		}
	}
	return;
} 


}
