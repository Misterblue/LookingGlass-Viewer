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
#include "Region.h"

namespace LG {

Region::Region() {
		this->CurrentBaseSceneNode = 0;
		this->TerrainSceneNode = 0;
		this->OceanSceneNode = 0;
		this->m_highRezSceneNode = 0;
		this->m_medRezSceneNode = 0;
		this->m_lowRezSceneNode = 0;
		this->m_veryLowRezSceneNode = 0;
		this->m_focusRegion = false;
		this->OceanHeight = 0.0;
}

Region::~Region() {
}

void Region::ReleaseRegion() {
}
void Region::ChangeRez(RegionRezCode newRez) {
	if (newRez != this->CurrentRez) {
		switch (newRez) {
			case RegionRezCodeHigh: {
				DisconnectOldRezAndConnectNew(newRez, m_highRezSceneNode);
			}
			case RegionRezCodeMed: {
				if (this->m_medRezSceneNode == NULL) {
					// create medium scene node
				}
				DisconnectOldRezAndConnectNew(newRez, m_medRezSceneNode);
			}
			case RegionRezCodeLow: {
				if (this->m_lowRezSceneNode == NULL) {
					// create low rez scene node
				}
				DisconnectOldRezAndConnectNew(newRez, m_lowRezSceneNode);
		   }
			case RegionRezCodeVeryLow: {
				if (this->m_veryLowRezSceneNode == NULL) {
					// create very low rez scene node
				}
				DisconnectOldRezAndConnectNew(newRez, m_veryLowRezSceneNode);
			}
		}
	}
}

void Region::DisconnectOldRezAndConnectNew(RegionRezCode newRez, Ogre::SceneNode* newBase) {
	// disconnect the old base node from the scene graph
	// connect new base node
	// point to our new state
	this->CurrentBaseSceneNode = newBase;
	this->CurrentRez = newRez;
	return;
}

void Region::SetFocusRegion() {
}

bool Region::IsFocusRegion() {
}

void Region::AddTerrain(Ogre::SceneNode*) {
}

void Region::AddOcean(Ogre::SceneNode*, float) {
}

}
