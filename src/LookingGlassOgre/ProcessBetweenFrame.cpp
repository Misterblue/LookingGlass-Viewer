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

const int CreateMeshResourceCode = 3;
struct CreateMeshResourceQd {
	int type;
	Ogre::String meshName;
	int* faceCounts;
	float* faceVertices;
};

const int CreateMeshSceneNodeCode = 4;
struct CreateMeshSceneNodeQd {
	int type;
	Ogre::SceneManager* sceneMgr; 
	Ogre::String sceneNodeName;
	Ogre::SceneNode* parentNode;
	Ogre::String entityName;
	Ogre::String meshName;
	bool inheritScale; bool inheritOrientation;
	float px; float py; float pz;
	float sx; float sy; float sz;
	float ow; float ox; float oy; float oz;
};

const int UpdateSceneNodeCode = 5;
struct UpdateSceneNodeQd {
	int type;
	Ogre::String entName;
	bool setPosition;
	bool setScale;
	bool setRotation;
	float px; float py; float pz;
	float sx; float sy; float sz;
	float ow; float ox; float oy; float oz;
};

ProcessBetweenFrame::ProcessBetweenFrame(RendererOgre::RendererOgre* ro, int workItems) {
	m_singleton = this;
	m_ro = ro;
	m_numWorkItemsToDoBetweenFrames = workItems;
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

void ProcessBetweenFrame::CreateMeshResource(const char* meshName, const int* faceCounts, const float* faceVertices) {
	CreateMeshResourceQd* cmrq = OGRE_NEW_T(CreateMeshResourceQd, Ogre::MEMCATEGORY_GENERAL);
	cmrq->type = CreateMeshResourceCode;
	cmrq->meshName = Ogre::String(meshName);
	cmrq->faceCounts = (int*)OGRE_MALLOC((*faceCounts) * sizeof(int), Ogre::MEMCATEGORY_GENERAL);
	memcpy(cmrq->faceCounts, faceCounts, (*faceCounts) * sizeof(int));
	cmrq->faceVertices = (float*)OGRE_MALLOC((*faceVertices) * sizeof(float), Ogre::MEMCATEGORY_GENERAL);
	memcpy(cmrq->faceVertices, faceVertices, (*faceVertices) * sizeof(float));
	m_betweenFrameWork.push((GenericQd*)cmrq);
}

void ProcessBetweenFrame::CreateMeshSceneNode(Ogre::SceneManager* sceneMgr, char* sceneNodeName,
					Ogre::SceneNode* parentNode,
					char* entityName,
					char* meshName,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz) {
	CreateMeshSceneNodeQd* csnq = OGRE_NEW_T(CreateMeshSceneNodeQd, Ogre::MEMCATEGORY_GENERAL);
	csnq->type = CreateMeshSceneNodeCode;
	csnq->sceneMgr = sceneMgr;
	csnq->sceneNodeName = Ogre::String(sceneNodeName);
	csnq->parentNode = parentNode;
	csnq->entityName = Ogre::String(entityName);
	csnq->meshName = Ogre::String(meshName);
	csnq->inheritScale = inheritScale;
	csnq->inheritOrientation = inheritOrientation;
	csnq->px = px; csnq->py = py; csnq->pz = pz;
	csnq->sx = sx; csnq->sy = sy; csnq->sz = sz;
	csnq->ow = ow; csnq->ox = ox; csnq->oy = oy; csnq->oz = oz;
	m_betweenFrameWork.push((GenericQd*)csnq);
}

void ProcessBetweenFrame::UpdateSceneNode(char* entName,
					bool setPosition, float px, float py, float pz,
					bool setScale, float sx, float sy, float sz,
					bool setRotation, float ow, float ox, float oy, float oz) {
	UpdateSceneNodeQd* usnq = OGRE_NEW_T(UpdateSceneNodeQd, Ogre::MEMCATEGORY_GENERAL);
	usnq->type = UpdateSceneNodeCode;
	usnq->entName = Ogre::String(entName);
	usnq->setPosition = setPosition;
	usnq->setScale = setScale;
	usnq->setRotation = setRotation;
	usnq->px = px; usnq->py = py; usnq->pz = pz;
	usnq->sx = sx; usnq->sy = sy; usnq->sz = sz;
	usnq->ow = ow; usnq->ox = ox; usnq->oy = oy; usnq->oz = oz;
	m_betweenFrameWork.push((GenericQd*)usnq);
}

// we're between frames, on our own thread so we can do the work without locking
bool ProcessBetweenFrame::frameEnded(const Ogre::FrameEvent& evt) {
	ProcessWorkItems(m_numWorkItemsToDoBetweenFrames);
	return true;
}

// return true if there is still work to do
bool ProcessBetweenFrame::HasWorkItems() {
	return !m_betweenFrameWork.empty();
}

void ProcessBetweenFrame::ProcessWorkItems(int numToProcess) {
	int loopCount = numToProcess;
	while (!m_betweenFrameWork.empty()) {
		// only do so much work each frame
		if (loopCount-- < 0) break;
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
			case CreateMeshResourceCode: {
				CreateMeshResourceQd* cmrq = (CreateMeshResourceQd*)workGeneric;
				m_ro->CreateMeshResource(cmrq->meshName.c_str(), cmrq->faceCounts, cmrq->faceVertices);
				cmrq->meshName.clear();
				OGRE_FREE(cmrq->faceCounts, Ogre::MEMCATEGORY_GENERAL);
				OGRE_FREE(cmrq->faceVertices, Ogre::MEMCATEGORY_GENERAL);
				OGRE_FREE(cmrq, Ogre::MEMCATEGORY_GENERAL);
				break;
			}
			case CreateMeshSceneNodeCode: {
				CreateMeshSceneNodeQd* csnq = (CreateMeshSceneNodeQd*)workGeneric;
				Ogre::SceneNode* node = m_ro->CreateSceneNode(
					csnq->sceneMgr, csnq->sceneNodeName.c_str(), csnq->parentNode,
					csnq->inheritScale, csnq->inheritOrientation,
					csnq->px, csnq->py, csnq->pz,
					csnq->sx, csnq->sy, csnq->sz,
					csnq->ow, csnq->ox, csnq->oy, csnq->oz);
				m_ro->AddEntity(csnq->sceneMgr, node, csnq->entityName.c_str(), csnq->meshName.c_str());
				csnq->sceneNodeName.clear();
				csnq->entityName.clear();
				csnq->meshName.clear();
				OGRE_FREE(csnq, Ogre::MEMCATEGORY_GENERAL);
				break;
			}
			case UpdateSceneNodeCode: {
				UpdateSceneNodeQd* usnq = (UpdateSceneNodeQd*)workGeneric;
				m_ro->UpdateSceneNode( usnq->entName.c_str(),
					usnq->setPosition, usnq->px, usnq->py, usnq->pz,
					usnq->setScale, usnq->sx, usnq->sy, usnq->sz,
					usnq->setRotation, usnq->ow, usnq->ox, usnq->oy, usnq->oz);
				usnq->entName.clear();
				OGRE_FREE(usnq, Ogre::MEMCATEGORY_GENERAL);
			}
		}
	}
	return;
}

}