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
#include "VisCalcBase.h"

namespace VisCalc {

class VisCalcFrustDist : public VisCalcBase , public Ogre::FrameListener {

public:
	VisCalcFrustDist(RendererOgre::RendererOgre*);
	~VisCalcFrustDist();

	void Initialize();
	void Start();
	void Stop();

	void RecalculateVisibility();
	// called to do low level visibility calc
	virtual bool CalculateVisibilityImpl(Ogre::Camera*, Ogre::Entity*, float);

	// Ogre::FrameListener
	// bool frameStarted(const Ogre::FrameEvent &e);
	// bool frameRenderingQueued(const Ogre::FrameEvent &e);
	bool frameEnded(const Ogre::FrameEvent &e);

private:
	RendererOgre::RendererOgre* m_ro;
	VisCalcFrustDist* m_singleton;

	void calculateEntityVisibility();
	void calculateEntityVisibility(Ogre::Node*);
	bool calculateScaleVisibility(float, float);
	void processEntityVisibility();
	void queueMeshLoad(Ogre::Entity*, Ogre::MeshPtr);
	void queueMeshUnload(Ogre::MeshPtr);
	void unloadTheMesh(Ogre::MeshPtr);
	bool m_shouldCullByFrustrum;			// true if should cull visible objects by the camera frustrum
	bool m_shouldCullByDistance;			// true if should cull visible objects by distance from camera
	bool m_shouldCullMeshes;				// true if should cull meshes
	bool m_shouldCullTextures;				// true if should cull textures
	float m_visibilityScaleMaxDistance;		// not visible after this far
	float m_visibilityScaleOnlyLargeAfter;	// after this distance, only large things visible
	float m_visibilityScaleMinDistance;		// always visible is this close
	float m_visibilityScaleLargeSize;		// what is large enough to see at a distance
	bool m_recalculateVisibility;			// set to TRUE if visibility should be recalcuated

	int m_meshesReloadedPerFrame;			// number of meshes to reload per frame
};
}