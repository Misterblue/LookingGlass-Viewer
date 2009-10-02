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

// LookingGlassOgre.cpp : Defines the exported functions for the DLL application.

#include "stdafx.h"
#include <stdarg.h>
#include "ProcessBetweenFrame.h"
#include "LookingGlassOgre.h"
#include "RendererOgre.h"
#include "OLMaterialTracker.h"

namespace ProcessBetweenFrame {

std::queue<void*> m_betweenFrameWork;
ProcessBetweenFrame* m_singleton;
RendererOgre::RendererOgre* m_ro;

const int GenericCode = 0;
struct GenericQd {
	int type;
	int data;
};

const int RefreshResourceCode = 1;
struct RefreshResourceQd {
	int type;
	Ogre::String matName;
	int rType;
};

const int CreateMaterialResourceCode = 2;
struct CreateMaterialResourceQd {
	int type;
	Ogre::String matName;
	Ogre::String texName;
	float parms[OLMaterialTracker::OLMaterialTracker::CreateMaterialSize];
};

ProcessBetweenFrame::ProcessBetweenFrame(RendererOgre::RendererOgre* ro) {
	m_singleton = this;
	m_ro = ro;
	LookingGlassOgr::GetOgreRoot()->addFrameListener(this);
}

ProcessBetweenFrame::~ProcessBetweenFrame() {
	LookingGlassOgr::GetOgreRoot()->removeFrameListener(this);
}

void ProcessBetweenFrame::RefreshResource(char* resourceName, int rType) {
	RefreshResourceQd* rrq = OGRE_NEW_T(RefreshResourceQd, Ogre::MEMCATEGORY_GENERAL);
	rrq->type = RefreshResourceCode;
	rrq->matName = Ogre::String(resourceName);
	rrq->rType = rType;
	m_betweenFrameWork.push((GenericQd*)rrq);
}

void ProcessBetweenFrame::CreateMaterialResource2(const char* matName, char* texName, const float* parms) {
	CreateMaterialResourceQd* cmrq = OGRE_NEW_T(CreateMaterialResourceQd, Ogre::MEMCATEGORY_GENERAL);
	cmrq->type = CreateMaterialResourceCode;
	cmrq->matName = Ogre::String(matName);
	cmrq->texName = Ogre::String(texName);
	memcpy(cmrq->parms, parms, OLMaterialTracker::OLMaterialTracker::CreateMaterialSize*sizeof(float));
	m_betweenFrameWork.push((GenericQd*)cmrq);
}

// we're between frames, on our own thread so we can do the work without locking
bool ProcessBetweenFrame::frameRenderingQueued(const Ogre::FrameEvent& evt) {
	while (!m_betweenFrameWork.empty()) {
		GenericQd* workGeneric = (GenericQd*)m_betweenFrameWork.front();
		m_betweenFrameWork.pop();
		switch (workGeneric->type) {
			case RefreshResourceCode: {
				RefreshResourceQd* rrq = (RefreshResourceQd*)workGeneric;
				m_ro->MaterialTracker()->RefreshResource(rrq->matName.c_str(), rrq->rType);
				rrq->matName.clear();
				OGRE_FREE(rrq, Ogre::MEMCATEGORY_GENERAL);
				break;
			}
			case CreateMaterialResourceCode: {
				CreateMaterialResourceQd* cmrq = (CreateMaterialResourceQd*)workGeneric;
				m_ro->MaterialTracker()->CreateMaterialResource2(
						cmrq->matName.c_str(), cmrq->texName.c_str(), cmrq->parms);
				cmrq->matName.clear();
				cmrq->texName.clear();
				OGRE_FREE(cmrq, Ogre::MEMCATEGORY_GENERAL);
				break;
			 }
		}
	}
	return true;
}

}