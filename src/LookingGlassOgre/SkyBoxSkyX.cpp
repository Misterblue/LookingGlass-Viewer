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

namespace LG {

SkyBoxSkyX::SkyBoxSkyX() {
}

SkyBoxSkyX::~SkyBoxSkyX() {
}

void SkyBoxSkyX::Initialize() {
	// Create SkyX
	m_SkyX = new SkyX::SkyX(LG::RendererOgre::Instance()->m_sceneMgr, LG::RendererOgre::Instance()->m_camera);
	m_SkyX->create();
}

void SkyBoxSkyX::Start() {
	// Add a basic cloud layer
	// m_SkyX->getCloudsManager()->add(SkyX::CloudLayer::Options(/* Default options */));
	// add cloud layer 1. These are the default values
	m_SkyX->getCloudsManager()->add(SkyX::CloudLayer::Options(
		100.0,		// height
		0.001,		// Scale
		Ogre::Vector2(1.0, 1.0),	// wind direction
		0.125,		// Time mulitplier
		0.05,		// distance attenuation
		1.0,		// detail attenuation
		2.0,		// normal multiplier
		0.25,		// height volume
		0.01		// volumetric displacement
		));
	// add another layer that is a little off
	m_SkyX->getCloudsManager()->add(SkyX::CloudLayer::Options(
		130.0,		// height
		0.001,		// Scale
		Ogre::Vector2(1.0, 0.8),	// wind direction
		0.2,		// Time mulitplier
		0.05,		// distance attenuation
		1.0,		// detail attenuation
		2.0,		// normal multiplier
		0.25,		// height volume
		0.01		// volumetric displacement
		));

	m_sun = LG::RendererOgre::Instance()->m_sceneMgr->createLight("sun");
	m_sun->setType(Ogre::Light::LT_DIRECTIONAL);	// directional and sun-like
	m_sun->setDiffuseColour(LG::GetParameterColor("Renderer.Ogre.Sun.Color"));
	m_sun->setCastShadows(true);

	m_moon = LG::RendererOgre::Instance()->m_sceneMgr->createLight("moon");
	m_moon->setType(Ogre::Light::LT_DIRECTIONAL);	// directional and sun-like
	m_moon->setDiffuseColour(Ogre::ColourValue(LG::GetParameterColor("Renderer.Ogre.Moon.Color")));
	m_moon->setCastShadows(true);

	if (LG::GetParameterBool("Renderer.Ogre.SkyX.LightingHDR")) {
		m_SkyX->setLightingMode(m_SkyX->LM_HDR);
	}
	else {
		m_SkyX->setLightingMode(m_SkyX->LM_LDR);
	}

	SkyX::AtmosphereManager::Options SkyXOptions = m_SkyX->getAtmosphereManager()->getOptions();

	// make east the same direction as in SL
	SkyXOptions.EastPosition = Ogre::Vector2(1.0,0.0);

	m_SkyX->getAtmosphereManager()->setOptions(SkyXOptions);

	// Add frame listener
	LG::RendererOgre::Instance()->m_root->addFrameListener(this);

}

void SkyBoxSkyX::Stop() {
	LG::RendererOgre::Instance()->m_root->removeFrameListener(this);
	if (m_SkyX != 0) {
		delete m_SkyX;
		m_SkyX = 0;
	}
}

void SkyBoxSkyX::AddSkyPass(Ogre::MaterialPtr matP) {
	return;
}


// bool SkyBoxSkyX::frameEnded(const Ogre::FrameEvent &e) {
bool SkyBoxSkyX::frameRenderingQueued(const Ogre::FrameEvent &e) {
/*
		// Check camera height
		Ogre::RaySceneQuery * raySceneQuery = 
			LG::RendererOgre::Instance()->mSceneMgr->
			     createRayQuery(Ogre::Ray(mCamera->getPosition() + Ogre::Vector3(0,1000000,0), 
				                Vector3::NEGATIVE_UNIT_Y));
		Ogre::RaySceneQueryResult& qryResult = raySceneQuery->execute();
        Ogre::RaySceneQueryResult::iterator i = qryResult.begin();
        if (i != qryResult.end() && i->worldFragment) {
			if (LG::RendererOgre::Instance()->mCamera->getDerivedPosition().y < i->worldFragment->singleIntersection.y + 30) {
                LG::RendererOgre::Instance()->mCamera-> setPosition(mCamera->getPosition().x, 
                                 i->worldFragment->singleIntersection.y + 30, 
                                 mCamera->getPosition().z);
			}
        }
		*/

		m_sun->setPosition(m_SkyX->getAtmosphereManager()->getSunPosition());
		m_sun->setDirection(m_SkyX->getAtmosphereManager()->getSunDirection());
		// if below the horizon, turn it off
		if (m_sun->getPosition().y < 0) {
			m_sun->setVisible(false);
		}
		else {
			m_sun->setVisible(true);
		}
		m_moon->setPosition(m_SkyX->getMoonManager()->getMoonSceneNode()->getPosition());
		m_moon->setDirection(m_SkyX->getCamera()->getPosition() - m_moon->getPosition());
		// if below the horizon, turn it off
		if (m_moon->getPosition().y < 0) {
			m_moon->setVisible(false);
		}
		else {
			m_moon->setVisible(true);
		}

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