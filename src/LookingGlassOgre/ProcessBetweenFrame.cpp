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

namespace LG {

ProcessBetweenFrame* ProcessBetweenFrame::m_instance = NULL;
bool ProcessBetweenFrame::m_keepProcessing = false;

// ====================================================================
// RefreshResource
// Given a resource name and a resource type, cause Ogre to reload the resource
class RefreshResourceQc : public GenericQc {
public:
	Ogre::String matName;
	int rType;
	RefreshResourceQc(float prio, Ogre::String uni, char* resourceName, int rTyp) {
		// this->priority = prio + 300.0;	// EXPERIMENTAL: do refreshes last
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
		LG::OLMaterialTracker::Instance()->RefreshResource(this->matName.c_str(), this->rType);
	}
};

// ====================================================================
class CreateMaterialResourceQc : public GenericQc {
public:
	Ogre::String matName;
	Ogre::String texName;
	float parms[LG::OLMaterialTracker::CreateMaterialSize];
	CreateMaterialResourceQc(float prio, Ogre::String uni, 
					const char* mName, const char* tName, const float* inParms) {
		// this->priority = prio;
		this->priority = 0.0;	// EXPERIMENTAL: to get materials out of the way
		// this->priority = prio - fmod(prio, (float)100.0);	// EXPERIMENTAL. Group material ops
		this->cost = 0;
		this->uniq = uni;
		this->matName = Ogre::String(mName);
		this->texName = Ogre::String(tName);
		memcpy(this->parms, inParms, LG::OLMaterialTracker::CreateMaterialSize*sizeof(float));
	}
	~CreateMaterialResourceQc(void) {
		this->uniq.clear();
		this->matName.clear();
		this->texName.clear();
	}
	void Process() {
		LG::OLMaterialTracker::Instance()->CreateMaterialResource2(this->matName.c_str(), this->texName.c_str(), this->parms);
	}
};

// ====================================================================
class CreateMaterialResource6Qc : public GenericQc {
public:
	Ogre::String matName1;
	Ogre::String matName2;
	Ogre::String matName3;
	Ogre::String matName4;
	Ogre::String matName5;
	Ogre::String matName6;
	Ogre::String textureName1;
	Ogre::String textureName2;
	Ogre::String textureName3;
	Ogre::String textureName4;
	Ogre::String textureName5;
	Ogre::String textureName6;
	const float* matParams;
	CreateMaterialResource6Qc(float prio, Ogre::String uni, 
			const char* matName1p, const char* matName2p, const char* matName3p, 
			const char* matName4p, const char* matName5p, const char* matName6p, 
			char* textureName1p, char* textureName2p, char* textureName3p, 
			char* textureName4p, char* textureName5p, char* textureName6p, 
			const float* parmsp) {
		// this->priority = prio;
		this->priority = 0.0;	// EXPERIMENTAL: to get materials out of the way
		// this->priority = prio - fmod(prio, (float)100.0);	// EXPERIMENTAL. Group material ops
		this->cost = 0;
		this->uniq = uni;
		if (matName1p != 0) {
			this->matName1 = Ogre::String(matName1p);
			this->textureName1 = Ogre::String(textureName1p);
		}
		if (matName2p != 0) {
			this->matName2 = Ogre::String(matName2p);
			this->textureName2 = Ogre::String(textureName2p);
		}
		if (matName3p != 0) {
			this->matName3 = Ogre::String(matName3p);
			this->textureName3 = Ogre::String(textureName3p);
		}
		if (matName4p != 0) {
			this->matName4 = Ogre::String(matName4p);
			this->textureName4 = Ogre::String(textureName4p);
		}
		if (matName5p != 0) {
			this->matName5 = Ogre::String(matName5p);
			this->textureName5 = Ogre::String(textureName5p);
		}
		if (matName6p != 0) {
			this->matName6 = Ogre::String(matName6p);
			this->textureName6 = Ogre::String(textureName6p);
		}
		int blocksize = (*parmsp * 6 + 1 ) * sizeof(float);
		this->matParams = (float*)malloc(blocksize);
		memcpy((void*)this->matParams, parmsp, blocksize);
	}
	~CreateMaterialResource6Qc(void) {
		this->uniq.clear();
		this->matName1.clear(); this->matName2.clear(); this->matName3.clear();
		this->matName4.clear(); this->matName5.clear(); this->matName6.clear();
		this->textureName1.clear(); this->textureName2.clear(); this->textureName3.clear();
		this->textureName4.clear(); this->textureName5.clear(); this->textureName6.clear();
		free((void*)this->matParams);
	}
	void Process() {
		int stride = (int)this->matParams[0];
		LG::OLMaterialTracker::Instance()->CreateMaterialResource2(this->matName1.c_str(), this->textureName1.c_str(), &(this->matParams[1 + stride * 0]));
		if (!this->matName2.empty())
			LG::OLMaterialTracker::Instance()->CreateMaterialResource2(this->matName2.c_str(), this->textureName2.c_str(), &(this->matParams[1 + stride * 1]));
		if (!this->matName3.empty())
			LG::OLMaterialTracker::Instance()->CreateMaterialResource2(this->matName3.c_str(), this->textureName3.c_str(), &(this->matParams[1 + stride * 2]));
		if (!this->matName4.empty())
			LG::OLMaterialTracker::Instance()->CreateMaterialResource2(this->matName4.c_str(), this->textureName4.c_str(), &(this->matParams[1 + stride * 3]));
		if (!this->matName5.empty())
			LG::OLMaterialTracker::Instance()->CreateMaterialResource2(this->matName5.c_str(), this->textureName5.c_str(), &(this->matParams[1 + stride * 4]));
		if (!this->matName6.empty())
			LG::OLMaterialTracker::Instance()->CreateMaterialResource2(this->matName6.c_str(), this->textureName6.c_str(), &(this->matParams[1 + stride * 5]));
	}
};

// ====================================================================
class CreateMeshResourceQc : public GenericQc {
public:
	Ogre::String meshName;
	int* faceCounts;
	float* faceVertices;
	float origPriority;
	CreateMeshResourceQc(float prio, Ogre::String uni, 
					const char* mName, const int* faceC, const float* faceV) {
		this->priority = prio;
		this->origPriority = prio;
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
		LG::RendererOgre::Instance()->CreateMeshResource(this->meshName.c_str(), this->faceCounts, this->faceVertices);
	}

	void RecalculatePriority() {
		return;
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
	float origPriority;
	CreateMeshSceneNodeQc(float prio, Ogre::String uni,
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
		this->origPriority = prio;
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
		Ogre::SceneNode* node = LG::RendererOgre::Instance()->CreateSceneNode(
					this->sceneMgr, this->sceneNodeName.c_str(), this->parentNode,
					this->inheritScale, this->inheritOrientation,
					this->px, this->py, this->pz,
					this->sx, this->sy, this->sz,
					this->ow, this->ox, this->oy, this->oz);
		LG::RendererOgre::Instance()->AddEntity(this->sceneMgr, node, this->entityName.c_str(), this->meshName.c_str());
	}
	void RecalculatePriority() {
		Ogre::Vector3 ourLoc = Ogre::Vector3(this->px, this->py, this->pz);
		this->priority = ourLoc.distance(LG::RendererOgre::Instance()->m_camera->getPosition());
		/*
		Ogre::Vector3 cameraRelation = LG::RendererOgre::Instance()->m_camera->getOrientation() * ourLoc;
		if (cameraRelation.x < 0) {
			// we're behind the camera
			this->priority = this->priority + 300.0;
		}
		*/
		if (!LG::RendererOgre::Instance()->m_camera->isVisible(Ogre::Sphere(ourLoc, 3.0))) {
			// we're not visible at the moment so no rush to create us
			this->priority = this->priority + 500.0;
		}
		return;
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
	UpdateSceneNodeQc(float prio, Ogre::String uni,
					char* entName,
					bool setPosition, float px, float py, float pz,
					bool setScale, float sx, float sy, float sz,
					bool setRotation, float ow, float ox, float oy, float oz) {
		this->priority = prio;	// EXPERIMENTAL: do refreshes last 
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
		LG::RendererOgre::Instance()->UpdateSceneNode(this->entName.c_str(),
					this->setPosition, this->px, this->py, this->pz,
					this->setScale, this->sx, this->sy, this->sz,
					this->setRotation, this->ow, this->ox, this->oy, this->oz);
	}
};

// ====================================================================
// Queue of work to do between frames.
// To add a between frame operation, you write a subclass of GenericQc like those
// above, write a routine to create and instance of the class and put it in the
// queue and later, between frames, the Process() routine will be called.
// The constructors and destructors of the *Qc class handles all the allocation
// and deallocation of memory needed to pass the parameters.
ProcessBetweenFrame::ProcessBetweenFrame() {
	int betweenWork = LG::GetParameterInt("Renderer.Ogre.BetweenFrame.WorkItems");
	if (betweenWork == 0) betweenWork = 5000;
	m_numWorkItemsToDoBetweenFrames = betweenWork;

	m_workItemMutex = LGLOCK_ALLOCATE_MUTEX("ProcessBetweenFrames");
	// this is the number of work items to do when between two frames
	m_modified = false;
	// link into the renderer.
	LG::GetOgreRoot()->addFrameListener(this);
	// m_processingThread = LGLOCK_ALLOCATE_THREAD(&ProcessThreadRoutine);
	LG::ProcessBetweenFrame::m_keepProcessing = true;
}

ProcessBetweenFrame::~ProcessBetweenFrame() {
	LGLOCK_RELEASE_MUTEX(m_workItemMutex);
	LG::GetOgreRoot()->removeFrameListener(this);
	m_keepProcessing = false;
}

// SingletonInstance.Shutdown()
void ProcessBetweenFrame::Shutdown() {
	return;
}

// ====================================================================
// refresh a resource
void ProcessBetweenFrame::RefreshResource(float priority, char* resourceName, int rType) {
	RefreshResourceQc* rrq = new RefreshResourceQc(priority, resourceName, resourceName, rType);
	QueueWork((GenericQc*)rrq);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameRefreshResource);
}

void ProcessBetweenFrame::CreateMaterialResource2(float priority, 
			  const char* matName, const char* texName, const float* parms) {
	CreateMaterialResourceQc* cmrq = new CreateMaterialResourceQc(priority, matName, matName, texName, parms);
	QueueWork((GenericQc*)cmrq);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameCreateMaterialResource);
}
void ProcessBetweenFrame::CreateMaterialResource6(float priority, const char* uniq,
			const char* matName1, const char* matName2, const char* matName3, 
			const char* matName4, const char* matName5, const char* matName6, 
			char* textureName1, char* textureName2, char* textureName3, 
			char* textureName4, char* textureName5, char* textureName6, 
			const float* parms) {
	CreateMaterialResource6Qc* cmr6q = new CreateMaterialResource6Qc(priority, uniq, 
			matName1, matName2, matName3, matName4, matName5, matName6, 
			textureName1, textureName2, textureName3, textureName4, textureName5, textureName6, 
			parms);
	QueueWork((GenericQc*)cmr6q);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameCreateMaterialResource);
}

void ProcessBetweenFrame::CreateMeshResource(float priority, 
				 const char* meshName, const int* faceCounts, const float* faceVertices) {
	CreateMeshResourceQc* cmrq = new CreateMeshResourceQc(priority, meshName, meshName, faceCounts, faceVertices);
	QueueWork((GenericQc*)cmrq);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameCreateMeshResource);
}

void ProcessBetweenFrame::CreateMeshSceneNode(float priority,
					Ogre::SceneManager* sceneMgr, 
					char* sceneNodeName,
					Ogre::SceneNode* parentNode,
					char* entityName,
					char* meshName,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz) {
	CreateMeshSceneNodeQc* csnq = new CreateMeshSceneNodeQc(priority, sceneNodeName, 
					sceneMgr, 
					sceneNodeName,
					parentNode,
					entityName,
					meshName,
					inheritScale, inheritOrientation,
					px, py, pz,
					sx, sy, sz,
					ow, ox, oy, oz);
	QueueWork((GenericQc*)csnq);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameCreateMeshSceneNode);
}

void ProcessBetweenFrame::UpdateSceneNode(float priority, char* entName,
					bool setPosition, float px, float py, float pz,
					bool setScale, float sx, float sy, float sz,
					bool setRotation, float ow, float ox, float oy, float oz) {
	UpdateSceneNodeQc* usnq = new UpdateSceneNodeQc(priority, entName,
					entName,
					setPosition, px, py, pz,
					setScale, sx, sy, sz,
					setRotation, ow, ox, oy, oz);
	QueueWork((GenericQc*)usnq);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameUpdateSceneNode);
}

// ====================================================================
// we're between frames, on our own thread so we can do the work without locking
int currentCost;
bool ProcessBetweenFrame::frameEnded(const Ogre::FrameEvent& evt) {
	// currentCost = m_numWorkItemsToDoBetweenFrames;
	if (evt.timeSinceLastFrame < 0.5) {
		currentCost = currentCost * 2;
		if (currentCost > m_numWorkItemsToDoBetweenFrames) currentCost = m_numWorkItemsToDoBetweenFrames;
	}
	else {
		currentCost = currentCost / 2;
	}
	if (currentCost < 100) currentCost = 100;
	ProcessWorkItems(currentCost);
	return true;
}

// static routine to get the thread. Loop around doing work.
// NOTE: THIS DOESN'T WORK
// The problem is that the OpenGL operations have to happen on the main
//  thread so all these between frame operations cannot be done by this
//  thread. Someday test DirectX
void ProcessBetweenFrame::ProcessThreadRoutine() {
	while (LG::ProcessBetweenFrame::m_keepProcessing) {
		LGLOCK_LOCK(LG::RendererOgre::Instance()->SceneGraphLock());
		if (!LG::ProcessBetweenFrame::Instance()->HasWorkItems()) {
			LGLOCK_WAIT(LG::RendererOgre::Instance()->SceneGraphLock());
		}
		LG::ProcessBetweenFrame::Instance()->ProcessWorkItems(100);
		LGLOCK_UNLOCK(LG::RendererOgre::Instance()->SceneGraphLock());
	}
	return;
}

// Add the work itemt to the work list
void ProcessBetweenFrame::QueueWork(GenericQc* wi) {
	LGLOCK_LOCK(m_workItemMutex);
	// Check to see if uniq is specified and remove any duplicates
	if (wi->uniq.length() != 0) {
		// There will be duplicate requests for things. If we already have a request, delete the old
		std::list<GenericQc*>::iterator li;
		for (li = m_betweenFrameWork.begin(); li != m_betweenFrameWork.end(); li++) {
			if (li._Ptr->_Myval->uniq.length() != 0) {
				if (wi->uniq == li._Ptr->_Myval->uniq) {
					m_betweenFrameWork.erase(li,li);
					LG::IncStat(LG::StatBetweenFrameDiscardedDups);
				}
			}
		}
	}
	m_betweenFrameWork.push_back(wi);
	m_modified = true;
	LGLOCK_UNLOCK(m_workItemMutex);
}

// return true if there is still work to do
bool ProcessBetweenFrame::HasWorkItems() {
	return !m_betweenFrameWork.empty();
}


bool XXCompareElements(const GenericQc* e1, const GenericQc* e2) {
	return (e1->priority < e2->priority);
}

int repriorityCount = 10;
void ProcessBetweenFrame::ProcessWorkItems(int numToProcess) {
	// This sort is intended to put the highest priority (ones with lowest numbers) at
	//   the front of the list for processing first.
	// TODO: figure out why uncommenting this line causes exceptions
	if (m_modified) {
		LGLOCK_LOCK(m_workItemMutex);
		if (repriorityCount-- < 0) {
			// periodically ask the items to recalc their priority
			repriorityCount = 10;
			std::list<GenericQc*>::iterator li;
			for (li = m_betweenFrameWork.begin(); li != m_betweenFrameWork.end(); li++) {
				li._Ptr->_Myval->RecalculatePriority();
			}
		}
		m_betweenFrameWork.sort(XXCompareElements);
		LGLOCK_UNLOCK(m_workItemMutex);
		m_modified = false;
	}
	int loopCost = numToProcess;
	while (!m_betweenFrameWork.empty() && (m_betweenFrameWork.size() > 6000 || (loopCost > 0) ) ) {
		LGLOCK_LOCK(m_workItemMutex);
		GenericQc* workGeneric = (GenericQc*)m_betweenFrameWork.front();
		m_betweenFrameWork.pop_front();
		LGLOCK_UNLOCK(m_workItemMutex);
		LG::SetStat(LG::StatBetweenFrameWorkItems, m_betweenFrameWork.size());
		LG::IncStat(LG::StatBetweenFrameTotalProcessed);
		workGeneric->Process();
		loopCost -= workGeneric->cost;
		delete(workGeneric);
	}
	return;
}

}