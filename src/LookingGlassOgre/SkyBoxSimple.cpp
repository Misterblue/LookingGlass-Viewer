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
#include "SkyBoxSimple.h"
#include "RendererOgre.h"

namespace LGSky {

SkyBoxSimple::SkyBoxSimple(RendererOgre::RendererOgre* ro) {
	m_ro = ro;
}

SkyBoxSimple::~SkyBoxSimple() {
}

void SkyBoxSimple::Initialize() {
	LookingGlassOgr::Log("DEBUG: LookingGlassOrge: createLight");
    // TODO: decide if I should connect  this to a scene node
    //    might make moving and control easier
	m_sunDistance = 2000.0;
	m_sunFocalPoint = Ogre::Vector3(128.0, 0.0, 128.0);
	m_sun = m_ro->m_sceneMgr->createLight("sun");
	m_sun->setType(Ogre::Light::LT_DIRECTIONAL);	// directional and sun-like
	m_sun->setDiffuseColour(Ogre::ColourValue::White);
	m_sun->setPosition(0.0f, 1000.0f, 0.0f);
	Ogre::Vector3 sunDirection(0.0f, -1.0f, 1.0f);
	sunDirection.normalise();
	m_sun->setDirection(sunDirection);
	// m_sun->setDirection(0.0f, 1.0f, 0.0f);
	m_sun->setCastShadows(true);
	m_sun->setVisible(true);

	try {
		// Ogre::String skyboxName = "LookingGlass/CloudyNoonSkyBox";
		Ogre::String skyboxName = LookingGlassOgr::GetParameter("Renderer.Ogre.SkyboxName");
		m_ro->m_sceneMgr->setSkyBox(true, skyboxName);
		LookingGlassOgr::Log("createSky: setting skybox to %s", skyboxName.c_str());
	}
	catch (Ogre::Exception e) {
		LookingGlassOgr::Log("Failed to set scene skybox");
		m_ro->m_sceneMgr->setSkyBox(false, "");
	}
}

void SkyBoxSimple::Start() {
}

void SkyBoxSimple::Stop() {
}

}