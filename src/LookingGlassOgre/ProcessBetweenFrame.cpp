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

ProcessBetweenFrame* m_singleton;
RendererOgre::RendererOgre* m_ro;

// the generic base class that goes in the list
class GenericQc {
public:
	int priority;
	int cost;
	Ogre::String uniq;
	virtual void Process() {};
};

// ====================================================================
// RefreshResource
// Given a resource name and a resource type, cause Ogre to reload the resource
class RefreshResourceQc : public GenericQc {
public:
	Ogre::String matName;
	int rType;
	RefreshResourceQc(int prio, Ogre::String uni, char* resourceName, int rTyp) {
		this->priority = prio;
		this->cost = 20;
		this->uniq = uni;
		this->matName = Ogre::String(resourceName);
		this->rType = rTyp;
	}
	~RefreshResourceQc(void) {
		this->uniq.clear();
		this->matName.clear();
	}
	void Process() {
		m_ro->MaterialTracker()->RefreshResource(this->matName.c_str(), this->rType);
	}
};

// ====================================================================
class CreateMaterialResourceQc : public GenericQc {
public:
	Ogre::String matName;
	Ogre::String texName;
	float parms[OLMaterialTracker::OLMaterialTracker::CreateMaterialSize];
	CreateMaterialResourceQc(int prio, Ogre::String uni, 
					const char* mName, const char* tName, const float* inParms) {
		this->priority = prio;
		this->cost = 10;
		this->uniq = uni;
		this->matName = Ogre::String(mName);
		this->texName = Ogre::String(tName);
		memcpy(this->parms, inParms, OLMaterialTracker::OLMaterialTracker::CreateMaterialSize*sizeof(float));
	}
	~CreateMaterialResourceQc(void) {
		this->uniq.clear();
		this->matName.clear();
		this->texName.clear();
	}
	void Process() {
		m_ro->MaterialTracker()->CreateMaterialResource2(this->matName.c_str(), this->texName.c_str(), this->parms);
	}
};

// ====================================================================
class CreateMeshResourceQc : public GenericQc {
public:
	Ogre::String meshName;
	int* faceCounts;
	float* faceVertices;
	CreateMeshResourceQc(int prio, Ogre::String uni, 
					const char* mName, const int* faceC, const float* faceV) {
		this->priority = prio;
		this->cost = 100;
		this->uniq = uni;
		this->meshName = Ogre::String(mName);
		this->faceCounts = (int*)malloc((*faceC) * sizeof(int));
		memcpy(this->faceCounts, faceC, (*faceC) * sizeof(int));
		this->faceVertices = (float*)malloc((*faceV) * sizeof(float));
		memcpy(this->faceVertices, faceV, (*faceV) * sizeof(float));
	}
	~CreateMeshResourceQc(void) {
		this->uniq.clear();
		this->meshName.clear();
		free(this->faceCounts);
		free(this->faceVertices);
	}
	void Process() {
		m_ro->CreateMeshResource(this->meshName.c_str(), this->faceCounts, this->faceVertices);
	}
};

// ====================================================================
class CreateMeshSceneNodeQc : public GenericQc {
public:
	Ogre::SceneManager* sceneMgr; 
	Ogre::String sceneNodeName;
	Ogre::SceneNode* parentNode;
	Ogre::String entityName;
	Ogre::String meshName;
	bool inheritScale; bool inheritOrientation;
	float px; float py; float pz;
	float sx; float sy; float sz;
	float ow; float ox; float oy; float oz;
	CreateMeshSceneNodeQc(int prio, Ogre::String uni,
					Ogre::SceneManager* sceneMgr, 
					char* sceneNodeName,
					Ogre::SceneNode* parentNode,
					char* entityName,
					char* meshName,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz) {
		this->priority = prio;
		this->cost = 10;
		this->uniq = uni;
		this->sceneMgr = sceneMgr;
		this->sceneNodeName = Ogre::String(sceneNodeName);
		this->parentNode = parentNode;
		this->entityName = Ogre::String(entityName);
		this->meshName = Ogre::String(meshName);
		this->inheritScale = inheritScale;
		this->inheritOrientation = inheritOrientation;
		this->px = px; this->py = py; this->pz = pz;
		this->sx = sx; this->sy = sy; this->sz = sz;
		this->ow = ow; this->ox = ox; this->oy = oy; this->oz = oz;
	}
	~CreateMeshSceneNodeQc(void) {
		this->uniq.clear();
		this->sceneNodeName.clear();
		this->entityName.clear();
		this->meshName.clear();
	}
	void Process() {
		Ogre::SceneNode* node = m_ro->CreateSceneNode(
					this->sceneMgr, this->sceneNodeName.c_str(), this->parentNode,
					this->inheritScale, this->inheritOrientation,
					this->px, this->py, this->pz,
					this->sx, this->sy, this->sz,
					this->ow, this->ox, this->oy, this->oz);
		m_ro->AddEntity(this->sceneMgr, node, this->entityName.c_str(), this->meshName.c_str());
	}
};

// ====================================================================
class UpdateSceneNodeQc : public GenericQc {
public:
	Ogre::String entName;
	bool setPosition;
	bool setScale;
	bool setRotation;
	float px; float py; float pz;
	float sx; float sy; float sz;
	float ow; float ox; float oy; float oz;
	UpdateSceneNodeQc(int prio, Ogre::String uni,
					char* entName,
					bool setPosition, float px, float py, float pz,
					bool setScale, float sx, float sy, float sz,
					bool setRotation, float ow, float ox, float oy, float oz) {
		this->priority = prio;
		this->cost = 10;
		this->uniq = uni;
		this->entName = Ogre::String(entName);
		this->setPosition = setPosition;
		this->setScale = setScale;
		this->setRotation = setRotation;
		this->px = px; this->py = py; this->pz = pz;
		this->sx = sx; this->sy = sy; this->sz = sz;
		this->ow = ow; this->ox = ox; this->oy = oy; this->oz = oz;
	}
	~UpdateSceneNodeQc(void) {
		this->uniq.clear();
		this->entName.clear();
	}
	void Process() {
		m_ro->UpdateSceneNode(this->entName.c_str(),
					this->setPosition, this->px, this->py, this->pz,
					this->setScale, this->sx, this->sy, this->sz,
					this->setRotation, this->ow, this->ox, this->oy, this->oz);
	}
};

std::list<GenericQc*> m_betweenFrameWork;

// ====================================================================
// Queue of work to do between frames.
// To add a between frame operation, you write a subclass of GenericQc like those
// above, write a routine to create and instance of the class and put it in the
// queue and later, between frames, the Process() routine will be called.
// The constructors and destructors of the *Qc class handles all the allocation
// and deallocation of memory needed to pass the parameters.
ProcessBetweenFrame::ProcessBetweenFrame(RendererOgre::RendererOgre* ro, int workItems) {
	m_singleton = this;
	m_ro = ro;
	// this is the number of work items to do when between two frames
	m_numWorkItemsToDoBetweenFrames = workItems;
	// link into the renderer.
	LookingGlassOgr::GetOgreRoot()->addFrameListener(this);
}

ProcessBetweenFrame::~ProcessBetweenFrame() {
	LookingGlassOgr::GetOgreRoot()->removeFrameListener(this);
}

// ====================================================================
// refresh a resource
void ProcessBetweenFrame::RefreshResource(int priority, char* resourceName, int rType) {
	RefreshResourceQc* rrq = new RefreshResourceQc(priority, Ogre::String(), resourceName, rType);
	m_betweenFrameWork.push_back((GenericQc*)rrq);
	LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameWorkItems);
	LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameRefreshResource);
}

void ProcessBetweenFrame::CreateMaterialResource2(int priority, 
			  const char* matName, const char* texName, const float* parms) {
	CreateMaterialResourceQc* cmrq = new CreateMaterialResourceQc(priority, Ogre::String(), matName, texName, parms);
	m_betweenFrameWork.push_back((GenericQc*)cmrq);
	LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameWorkItems);
	LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameCreateMaterialResource);
}

void ProcessBetweenFrame::CreateMeshResource(int priority, 
				 const char* meshName, const int* faceCounts, const float* faceVertices) {
	CreateMeshResourceQc* cmrq = new CreateMeshResourceQc(priority, Ogre::String(), meshName, faceCounts, faceVertices);
	m_betweenFrameWork.push_back((GenericQc*)cmrq);
	LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameWorkItems);
	LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameCreateMeshResource);
}

void ProcessBetweenFrame::CreateMeshSceneNode(int priority,
					Ogre::SceneManager* sceneMgr, 
					char* sceneNodeName,
					Ogre::SceneNode* parentNode,
					char* entityName,
					char* meshName,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz) {
	CreateMeshSceneNodeQc* csnq = new CreateMeshSceneNodeQc(priority, Ogre::String(), 
					sceneMgr, 
					sceneNodeName,
					parentNode,
					entityName,
					meshName,
					inheritScale, inheritOrientation,
					px, py, pz,
					sx, sy, sz,
					ow, ox, oy, oz);
	m_betweenFrameWork.push_back((GenericQc*)csnq);
	LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameWorkItems);
	LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameCreateMeshSceneNode);
}

void ProcessBetweenFrame::UpdateSceneNode(int priority, char* entName,
					bool setPosition, float px, float py, float pz,
					bool setScale, float sx, float sy, float sz,
					bool setRotation, float ow, float ox, float oy, float oz) {
	UpdateSceneNodeQc* usnq = new UpdateSceneNodeQc(priority, Ogre::String(),
					entName,
					setPosition, px, py, pz,
					setScale, sx, sy, sz,
					setRotation, ow, ox, oy, oz);
	m_betweenFrameWork.push_back((GenericQc*)usnq);
	LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameWorkItems);
	LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameUpdateSceneNode);
}

// ====================================================================
// we're between frames, on our own thread so we can do the work without locking
bool ProcessBetweenFrame::frameEnded(const Ogre::FrameEvent& evt) {
	ProcessWorkItems(m_numWorkItemsToDoBetweenFrames);
	return true;
}

// return true if there is still work to do
bool ProcessBetweenFrame::HasWorkItems() {
	return !m_betweenFrameWork.empty();
}


bool XXCompareElements(const GenericQc* e1, const GenericQc* e2) {
	return (e1->priority < e2->priority);
}

void ProcessBetweenFrame::ProcessWorkItems(int numToProcess) {
	// This sort is intended to put the highest priority (ones with lowest numbers) at
	//   the front of the list for processing first.
	// TODO: figure out why uncommenting this line causes exceptions
	// m_betweenFrameWork.sort(XXCompareElements);
	int loopCost = numToProcess;
	while (!m_betweenFrameWork.empty() && (loopCost > 0) ) {
		GenericQc* workGeneric = (GenericQc*)m_betweenFrameWork.front();
		m_betweenFrameWork.pop_front();
		LookingGlassOgr::SetStat(LookingGlassOgr::StatBetweenFrameWorkItems, m_betweenFrameWork.size());
		LookingGlassOgr::IncStat(LookingGlassOgr::StatBetweenFrameTotalProcessed);
		workGeneric->Process();
		loopCost -= workGeneric->cost;
		free(workGeneric);
	}
	return;
}

}