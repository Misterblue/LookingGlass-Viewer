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
#include "LGCamera.h"
#include "RegionTracker.h"
#include "RendererOgre.h"

namespace LG {

LGCamera::LGCamera(Ogre::String nam, Ogre::SceneManager* mgr) {
	Cam = mgr->createCamera(nam);
	Cam->setPosition(0.0, 0.0, 0.0);
	Cam->setDirection(0.0, 0.0, -1.0);
	// Cam->setOrientation(Ogre::Quaternion(Ogre::Radian(1.5707963), Ogre::Vector3(1.0, 0.0, 0.0)));
	// Cam->setOrientation(Ogre::Quaternion());
	// Cam->setNearClipDistance(2.0);
	Cam->setNearClipDistance(LG::GetParameterFloat("Renderer.Ogre.Camera.NearClip"));
	// Cam->setFarClipDistance(90000.0 * 6);
	Cam->setFarClipDistance(LG::GetParameterFloat("Renderer.Ogre.Camera.FarClip"));
	Cam->setAutoAspectRatio(true);
	if (Cam == NULL) {
		LG::Log("RendererOgre::createCamera: CAMERA FAILED TO CREATE");
	}
	this->m_cameraAttached = false;
}

LGCamera::~LGCamera() {
}

// Update the camera position given an location and a direction
void LGCamera::updateCamera(double px, double py, double pz, 
			float dw, float dx, float dy, float dz,
			float nearClip, float farClip, float aspect) {
	// LG::Log("LGCamera::UpdateCamera: pos=<%f, %f, %f>", (double)px, (double)py, (double)pz);
	// passed global parameters, localize the camera for the focus region that was moved to zero
	m_desiredPosition = LG::RegionTracker::Instance()->PositionCameraForFocusRegion(px, py, pz);
	m_desiredCameraOrientation = Ogre::Quaternion(dw, dx, dy, dz);
	m_desiredCameraOrientationProgress = 0.0;
	// to do slerped movement, comment the next lines and uncomment "XXXX" below
	// this->setOrientation(Ogre::Quaternion(dw, dx, dy, dz));
	// this->setPosition(m_desiredPosition);

	/*	don't fool with far and clip for the moment
	if (nearClip != this->getNearClipDistance()) {
		this->setNearClipDistance(nearClip);
	}
	if (farClip != this->getFarClipDistance()) {
		this->setFarClipDistance(farClip);
	}
	*/
	LG::RendererOgre::Instance()->m_visCalc->RecalculateVisibility();
	return;
}

// called at the beginning of the frame so we can slrp the camera
#define SECONDS_TO_SLERP 0.5f
void LGCamera::AdvanceCamera(const Ogre::FrameEvent& evt) {
	// Say time since last frame is .1s. That's 1/10 sec and if we're trying to
	//   to the smooth turn in 1/2 sec, this is 1/5 of our way there.
	float progress = evt.timeSinceLastFrame / SECONDS_TO_SLERP;
	m_desiredCameraOrientationProgress += progress;
	if (m_desiredCameraOrientationProgress > 0) {
		// if greater than zero we're working on progress
		if (m_desiredCameraOrientationProgress < 1.0) {
			// still within the progress area
			Ogre::Quaternion newOrientation = Ogre::Quaternion::Slerp(m_desiredCameraOrientationProgress, 
			// Ogre::Quaternion newOrientation = Ogre::Quaternion::nlerp(m_desiredCameraOrientationProgress, 
				this->getOrientation(), m_desiredCameraOrientation, true);
			this->setOrientation(newOrientation); // XXXX
			this->setPosition( this->getPosition() // XXXX
				+ ((m_desiredPosition - this->getPosition()) * m_desiredCameraOrientationProgress)); // XXXX
			LG::RendererOgre::Instance()->m_visCalc->RecalculateVisibility(); // XXXX
		}
		else {
			// we've advanced to progress. Make sure we get the last event in
			this->setOrientation(m_desiredCameraOrientation);
			this->setPosition(m_desiredPosition);
			m_desiredCameraOrientationProgress = -1.0;	// flag to say done
		}
	}
}


void LGCamera::setOrientation(Ogre::Quaternion qq) {
	// Ogre::Quaternion orient = Ogre::Quaternion(Ogre::Radian(1.5707963), Ogre::Vector3(1.0, 0.0, 0.0));
	// Ogre::Quaternion orient = Ogre::Quaternion(-Ogre::Radian(1.5707963), Ogre::Vector3(0.0, 1.0, 0.0));
	// Ogre::Quaternion orient = Ogre::Quaternion();
	if (Cam) Cam->setOrientation(qq);
}
void LGCamera::setOrientation(float ww, float xx, float yy, float zz) { 
	LG::Log("LGCamera::setOrientation: <%f, %f, %f, %f>", (double)ww, (double)xx, (double)yy, (double)zz);
	this->setOrientation(Ogre::Quaternion(ww, xx, yy, zz));
}
Ogre::Quaternion LGCamera::getOrientation() {
	if (Cam) return Cam->getOrientation();
	return Ogre::Quaternion();
}

void LGCamera::setPosition(double xx, double yy, double zz) {
	// LG::Log("LGCamera::setPosition: pos=<%f, %f, %f>", xx, yy, zz);
	if (!m_cameraAttached) {
		if (Cam) Cam->setPosition((float)xx, (float)yy, (float)zz);
	}
	// this->setPosition(Ogre::Vector3(xx, yy, zz));
}
void LGCamera::setPosition(Ogre::Vector3 vv) {
	// LG::Log("LGCamera::setPosition: pos=<%f, %f, %f>", (double)vv.x, (double)vv.y, (double)vv.z);
	if (!m_cameraAttached) {
		if (Cam) Cam->setPosition(vv.x, vv.y, vv.z);
	}
	// if (CamSceneNode2) CamSceneNode2->setPosition(vv);
}
Ogre::Vector3 LGCamera::getPosition() {
	// if (CamSceneNode2) 
	// return CamSceneNode2->getPosition();
	if (Cam) {
		Ogre::Vector3 pos = Cam->getPosition();
		return (Ogre::Vector3(pos.x, pos.y, pos.z));
	}
	return Ogre::Vector3();
}

// The region and the camera are in funny Ogre global address (neg z for instance).
// Here we hide all that funnyness by localizing the camera address then calculating
// that distance from the passed region localized address.
float LGCamera::getDistanceFromCamera(Ogre::Node* regionNode, Ogre::Vector3 otherLoc) {
	Ogre::Vector3 localizedCamPos;
	if (m_cameraAttached) {
		// if camera attached, the coordinates are already local
		localizedCamPos = Cam->getPosition();
	}
	else {
		// convert global, unaligned camera coords to region local coords
		localizedCamPos = Cam->getPosition() - regionNode->getPosition();
		// KLUDGE!!: since  the camera is unrotated compared to the terrain, its coordinates
		//    need tweeding before use. Someday make the camera in local coordinates.
		localizedCamPos = Ogre::Vector3( localizedCamPos.x, -localizedCamPos.z, localizedCamPos.y);
	}
	float dist = localizedCamPos.distance(otherLoc);
	if (dist < 0) dist = -dist;
	/* this routine is called too many times for it to normally output messages
	LG::Log("LGCamera::getDistanceFromCamera: camPos=<%f, %f, %f>", 
			(double)camPos.x, (double)camPos.y, (double)camPos.z);
	LG::Log("LGCamera::getDistanceFromCamera: rPos=<%f, %f, %f>", 
			(double)regionNode->getPosition().x, (double)regionNode->getPosition().y, (double)regionNode->getPosition().z);
	LG::Log("LGCamera::getDistanceFromCamera: lcamPos=<%f, %f, %f>, d=%f", 
			(double)localizedCamPos.x, (double)localizedCamPos.y, (double)localizedCamPos.z, (double)dist);
	*/
	return dist;
}

bool LGCamera::isVisible(const Ogre::AxisAlignedBox& aab) {
	if (Cam) return Cam->isVisible(aab);
	return false;
}
bool LGCamera::isVisible(const Ogre::Sphere& sph) {
	if (Cam) return Cam->isVisible(sph);
	return false;
}

float LGCamera::getNearClipDistance() {
	if (Cam) return Cam->getNearClipDistance();
	return 0.0;
}
void LGCamera::setNearClipDistance(float ncd) {
	if (Cam) Cam->setNearClipDistance(ncd);
}
float LGCamera::getFarClipDistance() {
	if (Cam) return Cam->getFarClipDistance();
	return 0.0;
}
void LGCamera::setFarClipDistance(float fcd) {
	if (Cam) Cam->setFarClipDistance(fcd);
}

// Pass any special camera orientation to this camera class. This could be used to pass
// the region twist that is used to map LL coordinates into Ogre coordinates.
void LGCamera::CreateCameraArmature(const char* cameraSceneNodeName, float px, float py, float pz,
			float sx, float sy, float sz, float ow, float ox, float oy, float oz) {
	/*
	// attempt to have to have the camera attached to rotated scene nodes.
	CamSceneNode = LG::RendererOgre::Instance()->CreateSceneNode(LG::RendererOgre::Instance()->m_sceneMgr, 
					cameraSceneNodeName, 0, true, true,
					0.0, 0.0, 0.0, sx, sy, sz, ow, ox, oy, oz);
	// Ogre::Quaternion rotat = Ogre::Quaternion(Ogre::Radian(1.5707963), Ogre::Vector3(1.0, 0.0, 0.0));
	Ogre::Quaternion rotat = Ogre::Quaternion();
	CamSceneNode2 = LG::RendererOgre::Instance()->CreateSceneNode(LG::RendererOgre::Instance()->m_sceneMgr, 
					cameraSceneNodeName, CamSceneNode, true, true,
					0.0, 0.0, 0.0, sx, sy, sz, rotat.w, rotat.x, rotat.y, rotat.z);
	Cam->setVisible(true);
	CamSceneNode2->attachObject(Cam);
	*/
	return;
}

// Another attempt to tame the camera.
// Attache the camera to a scene node that is a child of another scene node. The othere scene node is
// usually the agent's avatar so the camera will move around behind the avatar.
bool LGCamera::AttachCamera(const char* parentNodeName, float offsetX, float offsetY, float offsetZ,
				float ow, float ox, float oy, float oz) {
		bool ret = false;
		Ogre::String parentSceneNodeName = Ogre::String(parentNodeName);
		if (Cam && LG::RendererOgre::Instance()->m_sceneMgr->hasSceneNode(parentSceneNodeName)) {
			Ogre::SceneNode* parentNode = LG::RendererOgre::Instance()->m_sceneMgr->getSceneNode(Ogre::String(parentSceneNodeName));
			this->CamSceneNode = parentNode->createChildSceneNode("Camera/" + parentSceneNodeName, Ogre::Vector3(offsetX, offsetY, offsetZ));
			this->CamSceneNode->attachObject(Cam);
			m_cameraAttached = true;
			LG::Log("LGCamera::AttachCamera: camera attached to %s", parentNodeName);
			ret = true;
		}
		else {
			LG::Log("LGCamera::AttachCamera: could not attach camera to %s", parentNodeName);
		}
		return ret;
}
}