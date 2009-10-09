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
#include "SkyBoxSkyX.h"
#include "RendererOgre.h"

namespace LGSky {

SkyBoxSkyX::SkyBoxSkyX(RendererOgre::RendererOgre* ro) {
	m_ro = ro;
}

SkyBoxSkyX::~SkyBoxSkyX() {
}

void SkyBoxSkyX::Initialize() {
	// Create SkyX
	m_SkyX = new SkyX::SkyX(m_ro->m_sceneMgr, m_ro->m_camera);
	m_SkyX->create();
}

void SkyBoxSkyX::Start() {
	// Add a basic cloud layer
	m_SkyX->getCloudsManager()->add(SkyX::CloudLayer::Options(/* Default options */));

	// Add frame listener
	m_ro->m_root->addFrameListener(this);

}

void SkyBoxSkyX::Stop() {
	if (m_SkyX != 0) {
		delete m_SkyX;
		m_SkyX = 0;
	}
}

/* THINGS WE HAVEN'T ADDED YET
		// Add our ground atmospheric scattering pass to terrain material
		mSkyX->getGPUManager()->addGroundPass(
			static_cast<Ogre::MaterialPtr>(Ogre::MaterialManager::getSingleton().
			getByName("Terrain"))->getTechnique(0)->createPass(), 5000, Ogre::SBT_TRANSPARENT_COLOUR);
*/

bool SkyBoxSkyX::frameStarted(const Ogre::FrameEvent &e) {

/*
		// Check camera height
		Ogre::RaySceneQuery * raySceneQuery = 
			m_ro->mSceneMgr->
			     createRayQuery(Ogre::Ray(mCamera->getPosition() + Ogre::Vector3(0,1000000,0), 
				                Vector3::NEGATIVE_UNIT_Y));
		Ogre::RaySceneQueryResult& qryResult = raySceneQuery->execute();
        Ogre::RaySceneQueryResult::iterator i = qryResult.begin();
        if (i != qryResult.end() && i->worldFragment) {
			if (m_ro->mCamera->getDerivedPosition().y < i->worldFragment->singleIntersection.y + 30) {
                m_ro->mCamera-> setPosition(mCamera->getPosition().x, 
                                 i->worldFragment->singleIntersection.y + 30, 
                                 mCamera->getPosition().z);
			}
        }
		*/

		m_SkyX->update(e.timeSinceLastFrame);

		/*
		// Update terrain material
		static_cast<Ogre::MaterialPtr>(Ogre::MaterialManager::getSingleton().getByName("Terrain"))->
				getTechnique(0)->getPass(0)->
				getFragmentProgramParameters()->
				setNamedConstant("uLightY", -mSkyX->getAtmosphereManager()->getSunDirection().y);
				*/
		
        return true;

}
}