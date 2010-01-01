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
#include "Shadow02.h"
#include "RendererOgre.h"

namespace LG { 
	
Shadow02::Shadow02(const char* shadowName) : ShadowBase() {
	LG::RendererOgre::Instance()->m_sceneMgr->setShadowTexturePixelFormat(Ogre::PF_FLOAT16_R);
	LG::RendererOgre::Instance()->m_sceneMgr->setShadowTechnique(Ogre::SHADOWTYPE_TEXTURE_ADDITIVE);
	LG::RendererOgre::Instance()->m_sceneMgr->setShadowTextureSelfShadow(true);

	LG::RendererOgre::Instance()->m_sceneMgr->setShadowTextureCasterMaterial("Shadow02/ShadowCaster");
	LG::RendererOgre::Instance()->m_sceneMgr->setShadowTextureReceiverMaterial("Shadow02/ShadowReceiver");

	int shadowFarDistance = LG::GetParameterInt("Renderer.Ogre.ShadowFarDistance");
	LG::RendererOgre::Instance()->m_sceneMgr->setShadowFarDistance((float)shadowFarDistance);
	LG::RendererOgre::Instance()->m_sceneMgr->setShadowColour(Ogre::ColourValue(0.5, 0.5, 0.5));
	return;
}

Shadow02::~Shadow02() {
}

void Shadow02::AddTerrainShadow(Ogre::Material* mat) {
	return;
}

void Shadow02::AddLightShadow(Ogre::Light* lit) {
	return;
}

void Shadow02::AddReceiverShadow(Ogre::Material* mat) {
	mat->setReceiveShadows(true);
	return;
}

void Shadow02::AddCasterShadow(Ogre::MovableObject* mob) {
	mob->setCastShadows(true);

	return;
}

}
