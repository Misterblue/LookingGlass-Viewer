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
#include "Shadow06.h"
#include "RendererOgre.h"

namespace LG { 
	
	Shadow06::Shadow06(const char* shadowName) : ShadowBase() {
	Ogre::SceneManager* smgr = LG::RendererOgre::Instance()->m_sceneMgr;
    // This is the default material to use for shadow buffer rendering pass, overridable in script.
    // Note that we use the same single material (vertex program) for each object, so we're relying on
    // that we use Ogre software skinning. Hardware skinning would require us to do different vertex programs
    // for skinned/nonskinned geometry.
    std::string ogreShadowCasterMaterial = "rex/ShadowCaster";
    unsigned short shadowTextureSize = 2048;
    size_t shadowTextureCount = 1;
    Ogre::ColourValue shadowColor(0.6f, 0.6f, 0.6f);
	int shadowFarDistance = LG::GetParameterInt("Renderer.Ogre.ShadowFarDistance");

    smgr->setShadowColour(shadowColor);
    smgr->setShadowFarDistance(shadowFarDistance);
    smgr->setShadowTextureSize(shadowTextureSize);
    smgr->setShadowTextureCount(shadowTextureCount);

    smgr->setShadowTexturePixelFormat(Ogre::PF_FLOAT16_R);
    smgr->setShadowTechnique(Ogre::SHADOWTYPE_TEXTURE_ADDITIVE_INTEGRATED);
    smgr->setShadowTextureCasterMaterial(ogreShadowCasterMaterial.c_str());
    smgr->setShadowTextureSelfShadow(true);

    Ogre::ShadowCameraSetupPtr shadowCameraSetup = Ogre::ShadowCameraSetupPtr(new Ogre::FocusedShadowCameraSetup());
    smgr->setShadowCameraSetup(shadowCameraSetup);

    // If set to true, problems with objects that clip into the ground
    smgr->setShadowCasterRenderBackFaces(false);
	return;
}

Shadow06::~Shadow06() {
}

void Shadow06::AddTerrainShadow(Ogre::Material* mat) {
	return;
}

void Shadow06::AddLightShadow(Ogre::Light* lit) {
	return;
}

void Shadow06::AddReceiverShadow(Ogre::Material* mat) {
	mat->setReceiveShadows(true);
	return;
}

void Shadow06::AddCasterShadow(Ogre::MovableObject* mob) {
	mob->setCastShadows(true);
	return;
}

}
