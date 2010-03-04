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
#include "Shadow03.h"
#include "RendererOgre.h"

namespace LG { 
	
	Shadow03::Shadow03(const char* shadowName) : ShadowBase() {
	Ogre::SceneManager* smgr = LG::RendererOgre::Instance()->m_sceneMgr;
	LG::RendererOgre::Instance()->m_viewport->setClearEveryFrame(true);
	smgr->setShadowTextureSelfShadow(true);
	smgr->setShadowTextureCasterMaterial("Shadow03/shadow_caster");
	// ->setShadowTextureCount(LG::GetParameterInt("Renderer.Ogre.Shadow03.ShadowTextureCount));
	smgr->setShadowTextureCount(5);
	// smgr->setShadowTextureSize(LG::GetParameterInt("Renderer.Ogre.Shadow03.ShadowTextureSize"));
	smgr->setShadowTextureSize(1024);
	// smgr->setShadowTextureSize(LG::GetParameterInt("Renderer.Ogre.Shadow03.ShadowTextureSize"));
	smgr->setShadowTexturePixelFormat(Ogre::PF_FLOAT16_RGB);
	smgr->setShadowCasterRenderBackFaces(false);

	const unsigned numShadowRTTs = smgr->getShadowTextureCount();
    for (unsigned i = 0; i < numShadowRTTs; ++i) {
        Ogre::TexturePtr tex = smgr->getShadowTexture(i);
        Ogre::Viewport *vp = tex->getBuffer()->getRenderTarget()->getViewport(0);
        vp->setBackgroundColour(Ogre::ColourValue(1, 1, 1, 1));
        vp->setClearEveryFrame(true);
        //Ogre::CompositorManager::getSingleton().addCompositor(vp, "blur");
        //Ogre::CompositorManager::getSingleton().setCompositorEnabled(vp, "blur", true);
    }
    smgr->setShadowTechnique(Ogre::SHADOWTYPE_TEXTURE_ADDITIVE_INTEGRATED);
	// smgr->addShadowListener(this);


	LG::RendererOgre::Instance()->m_sceneMgr->setShadowTexturePixelFormat(Ogre::PF_FLOAT16_R);

	LG::RendererOgre::Instance()->m_sceneMgr->setShadowTexturePixelFormat(Ogre::PF_FLOAT16_R);
	LG::RendererOgre::Instance()->m_sceneMgr->setShadowTechnique(Ogre::SHADOWTYPE_TEXTURE_ADDITIVE);


	int shadowFarDistance = LG::GetParameterInt("Renderer.Ogre.ShadowFarDistance");
	LG::RendererOgre::Instance()->m_sceneMgr->setShadowFarDistance((float)shadowFarDistance);
	LG::RendererOgre::Instance()->m_sceneMgr->setShadowColour(Ogre::ColourValue(0.5, 0.5, 0.5));
	return;
}

Shadow03::~Shadow03() {
}

void Shadow03::AddTerrainShadow(Ogre::Material* mat) {
	return;
}

void Shadow03::AddLightShadow(Ogre::Light* lit) {
	return;
}

void Shadow03::AddReceiverShadow(Ogre::Material* mat) {
	mat->setReceiveShadows(true);
	return;
}

void Shadow03::AddCasterShadow(Ogre::MovableObject* mob) {
	mob->setCastShadows(true);

	return;
}

void Shadow03::shadowTextureCasterPreViewProj(Ogre::Light *light, Ogre::Camera *cam) {
    // basically, here we do some forceful camera near/far clip attenuation
    // yeah.  simplistic, but it works nicely.  this is the function I was talking
    // about you ignoring above in the Mgr declaration.
    float range = light->getAttenuationRange();
    cam->setNearClipDistance(range * 0.01f);
    cam->setFarClipDistance(range);
    // we just use a small near clip so that the light doesn't "miss" anything
    // that can shadow stuff.  and the far clip is equal to the lights' range.
    // (thus, if the light only covers 15 units of objects, it can only
    // shadow 15 units - the rest of it should be attenuated away, and not rendered)
}


}
