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
#include "SkyBoxSkyX.h"
#include "RegionTracker.h"
#include "RendererOgre.h"

namespace LG {

SkyBoxSkyX::SkyBoxSkyX() {
}

SkyBoxSkyX::~SkyBoxSkyX() {
}

void SkyBoxSkyX::Initialize() {
	// Create SkyX
	LG::Log("SkyBoxSkyX::Initialize");
	m_SkyX = new SkyX::SkyX(LG::RendererOgre::Instance()->m_sceneMgr, LG::RendererOgre::Instance()->m_camera->Cam);
	m_SkyX->create();
}

void SkyBoxSkyX::Start() {
	LG::Log("SkyBoxSkyX::Start");
	// Add a basic cloud layer
	// m_SkyX->getCloudsManager()->add(SkyX::CloudLayer::Options(/* Default options */));
	// add cloud layer 1. These are the default values
	m_SkyX->getCloudsManager()->add(SkyX::CloudLayer::Options(
		100.0f,		// height
		0.001f,		// Scale
		Ogre::Vector2(1.0f, 1.0f),	// wind direction
		0.125f,		// Time mulitplier
		0.05f,		// distance attenuation
		1.0f,		// detail attenuation
		2.0f,		// normal multiplier
		0.25f,		// height volume
		0.01f		// volumetric displacement
		));
	// add another layer that is a little off
	m_SkyX->getCloudsManager()->add(SkyX::CloudLayer::Options(
		130.0f,		// height
		0.001f,		// Scale
		Ogre::Vector2(1.0f, 0.8f),	// wind direction
		0.2f,		// Time mulitplier
		0.05f,		// distance attenuation
		1.0f,		// detail attenuation
		2.0f,		// normal multiplier
		0.25f,		// height volume
		0.01f		// volumetric displacement
		));

	m_sun = LG::RendererOgre::Instance()->m_sceneMgr->createLight("sun");
	m_sun->setType(Ogre::Light::LT_DIRECTIONAL);	// directional and sun-like
	m_sun->setDiffuseColour(LG::GetParameterColor("Renderer.Ogre.Sun.Color"));
	m_sun->setCastShadows(true);

	m_moon = LG::RendererOgre::Instance()->m_sceneMgr->createLight("moon");
	m_moon->setType(Ogre::Light::LT_DIRECTIONAL);	// directional and sun-like
	m_moon->setDiffuseColour(LG::GetParameterColor("Renderer.Ogre.Moon.Color"));
	m_moon->setCastShadows(true);

	if (LG::GetParameterBool("Renderer.Ogre.SkyX.LightingHDR")) {
		LG::Log("SkyBoxSkyX::Start: HDR lighting mode");
		m_SkyX->setLightingMode(m_SkyX->LM_HDR);
	}
	else {
		LG::Log("SkyBoxSkyX::Start: LDR lighting mode");
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
	LG::Log("SkyBoxSkyX::Stop");
	LG::RendererOgre::Instance()->m_root->removeFrameListener(this);
	if (m_SkyX != 0) {
		delete m_SkyX;
		m_SkyX = 0;
	}
}

void SkyBoxSkyX::AddSkyPass(Ogre::MaterialPtr matP) {
	return;
}


int sunPositionThrottle = 10;
bool SkyBoxSkyX::frameStarted(const Ogre::FrameEvent &e) {
	LG::StatIn(LG::InOutSkyBoxSkyX);

// bool SkyBoxSkyX::frameRenderingQueued(const Ogre::FrameEvent &e) {
	try {
		/* Don't know what this code does
		// Check camera height
		Ogre::Camera* cam = LG::RendererOgre::Instance()->m_camera->Cam;
		Ogre::RaySceneQuery * raySceneQuery = 
			LG::RendererOgre::Instance()->m_sceneMgr->
			createRayQuery(Ogre::Ray(cam->getPosition() + Ogre::Vector3(0,1000000,0), 
			Ogre::Vector3::NEGATIVE_UNIT_Y));
		Ogre::RaySceneQueryResult& qryResult = raySceneQuery->execute();
        Ogre::RaySceneQueryResult::iterator i = qryResult.begin();
        if (i != qryResult.end() && i->worldFragment) {
			if (cam->getDerivedPosition().y < i->worldFragment->singleIntersection.y + 30) {
                cam->setPosition(cam->getPosition().x, 
                                 i->worldFragment->singleIntersection.y + 30, 
                                 cam->getPosition().z);
			}
        }
		*/

		try {
			m_SkyX->update(e.timeSinceLastFrame);
		}
		catch (...) {
			LG::Log("EXCEPTION updating SkyX");
		}

		Ogre::Vector3 globalSun = m_SkyX->getAtmosphereManager()->getSunPosition();
		Ogre::Vector3 desiredSun = LG::RegionTracker::Instance()->PositionForFocusRegion(globalSun);
		m_sun->setPosition(desiredSun);
		m_sun->setDirection(m_SkyX->getAtmosphereManager()->getSunDirection());
		// if below the horizon, turn it off
		if (m_sun->getPosition().y < 0) {
			m_sun->setVisible(false);
		}
		else {
			m_sun->setVisible(true);
		}

		Ogre::Vector3 globalMoon = m_SkyX->getMoonManager()->getMoonSceneNode()->getPosition();
		// Ogre::Vector3 desiredMoon = LG::RegionTracker::Instance()->PositionForFocusRegion(globalMoon);
		Ogre::Vector3 desiredMoon = globalMoon;
		m_moon->setPosition(desiredMoon);
		m_moon->setDirection(m_SkyX->getCamera()->getPosition() - m_moon->getPosition());
		// if below the horizon, turn it off
		if (m_moon->getPosition().y < 0) {
			m_moon->setVisible(false);
		}
		else {
			m_moon->setVisible(true);
		}

		/*
		// Update terrain material
		static_cast<Ogre::MaterialPtr>(Ogre::MaterialManager::getSingleton().getByName("Terrain"))->
				getTechnique(0)->getPass(0)->
				getFragmentProgramParameters()->
				setNamedConstant("uLightY", -mSkyX->getAtmosphereManager()->getSunDirection().y);
				*/
	}
	catch (...) {
		LG::Log("SkyBoxSkyX: EXCEPTION FRAMESTARTED:");
	}
		
	LG::StatOut(LG::InOutSkyBoxSkyX);
    return true;

}
}