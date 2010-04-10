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

namespace LG {

	// Container for teh camera that hides all the armature stuff and where the
	// position and orientation are really hidden.
	class LGCamera {
	public:
		LGCamera(Ogre::String nam, Ogre::SceneManager* mgr);
		~LGCamera();


		void updateCamera(double px, double py, double pz, 
			float dw, float dx, float dy, float dz,
			float nearClip, float farClip, float aspect);
		void AdvanceCamera(const Ogre::FrameEvent& evt);

		void setOrientation(Ogre::Quaternion qq);
		void setOrientation(float ww, float xx, float yy, float zz);
		Ogre::Quaternion getOrientation();

		void setPosition(double, double, double);
		void setPosition(Ogre::Vector3 vv);
		Ogre::Vector3 getPosition();

		bool isVisible(const Ogre::AxisAlignedBox&);
		bool isVisible(const Ogre::Sphere&);

		float getNearClipDistance();
		void setNearClipDistance(float);
		float getFarClipDistance();
		void setFarClipDistance(float);

		float getDistanceFromCamera(Ogre::Node* , Ogre::Vector3);

		void CreateCameraArmature(const char* cameraSceneNodeName, float px, float py, float pz,
					float sx, float sy, float sz, float ow, float ox, float oy, float oz);
		bool AttachCamera(const char* parentNodeName, float offsetX, float offsetY, float offsetZ,
					float ow, float ox, float oy, float oz);

		Ogre::Camera* Cam;
	private:
		Ogre::SceneNode* CamSceneNode;		// handle to the top camera scene node
		Ogre::SceneNode* CamSceneNode2;		// handle to the one above camera scene node

		bool m_cameraAttached;				// true if camera attached to the avatar

		Ogre::Quaternion m_desiredCameraOrientation;
		Ogre::Vector3 m_desiredPosition;
		float m_desiredCameraOrientationProgress;

	};
}