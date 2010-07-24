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
#include "Shadow07.h"
#include "RendererOgre.h"

namespace LG { 
	
Shadow07::Shadow07(const char* shadowName) : ShadowBase() {
	LG::RendererOgre* ro = LG::RendererOgre::Instance();
	ro->m_sceneMgr->setShadowTextureSize(1024);
	// ro->m_sceneMgr->setShadowTechnique(Ogre::SHADOWTYPE_TEXTURE_MODULATIVE);
	ro->m_sceneMgr->setShadowTechnique(Ogre::SHADOWTYPE_TEXTURE_ADDITIVE);

	// Ogre::FocusedShadowCameraSetup* focussmSetup = new Ogre::FocusedShadowCameraSetup();
	// ro->m_sceneMgr->setShadowCameraSetup(Ogre::ShadowCameraSetupPtr(focussmSetup));

	Ogre::LiSPSMShadowCameraSetup* lispsmSetup = new Ogre::LiSPSMShadowCameraSetup();
	lispsmSetup->setOptimalAdjustFactor(2);
	ro->m_sceneMgr->setShadowCameraSetup(Ogre::ShadowCameraSetupPtr(lispsmSetup));

	int shadowFarDistance = LG::GetParameterInt("Renderer.Ogre.ShadowFarDistance");
	ro->m_sceneMgr->setShadowFarDistance((float)shadowFarDistance);
	ro->m_sceneMgr->setShadowColour(Ogre::ColourValue(0.35f, 0.35f, 0.35f));
	ro->m_sceneMgr->setAmbientLight(Ogre::ColourValue(0.3f, 0.3f, 0.3f));

	return;
}

Shadow07::~Shadow07() {
}

void Shadow07::AddTerrainShadow(Ogre::Material* mat) {
	return;
}

void Shadow07::AddLightShadow(Ogre::Light* lit) {
	return;
}

void Shadow07::AddReceiverShadow(Ogre::Material* mat) {
	mat->setReceiveShadows(true);
	return;
}

void Shadow07::AddCasterShadow(Ogre::MovableObject* mob) {
	mob->setCastShadows(true);
	return;
}

}
