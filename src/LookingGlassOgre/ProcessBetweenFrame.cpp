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
#include "RegionTracker.h"

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
		this->priority = prio;
		this->cost = 40;
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
class CreateMaterialResource7Qc : public GenericQc {
public:
	Ogre::String matName1;
	Ogre::String matName2;
	Ogre::String matName3;
	Ogre::String matName4;
	Ogre::String matName5;
	Ogre::String matName6;
	Ogre::String matName7;
	Ogre::String textureName1;
	Ogre::String textureName2;
	Ogre::String textureName3;
	Ogre::String textureName4;
	Ogre::String textureName5;
	Ogre::String textureName6;
	Ogre::String textureName7;
	const float* matParams;
	CreateMaterialResource7Qc(float prio, Ogre::String uni, 
			const char* matName1p, const char* matName2p, const char* matName3p, 
			const char* matName4p, const char* matName5p, const char* matName6p,
			const char* matName7p,
			char* textureName1p, char* textureName2p, char* textureName3p, 
			char* textureName4p, char* textureName5p, char* textureName6p, 
			char* textureName7p,
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
		if (matName7p != 0) {
			this->matName7 = Ogre::String(matName7p);
			this->textureName7 = Ogre::String(textureName7p);
		}
		int blocksize = (*parmsp * 7 + 1 ) * sizeof(float);
		this->matParams = (float*)malloc(blocksize);
		memcpy((void*)this->matParams, parmsp, blocksize);
	}
	~CreateMaterialResource7Qc(void) {
		this->uniq.clear();
		this->matName1.clear(); this->matName2.clear(); this->matName3.clear();
		this->matName4.clear(); this->matName5.clear(); this->matName6.clear();
		this->matName7.clear();
		this->textureName1.clear(); this->textureName2.clear(); this->textureName3.clear();
		this->textureName4.clear(); this->textureName5.clear(); this->textureName6.clear();
		this->textureName7.clear();
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
		if (!this->matName7.empty())
			LG::OLMaterialTracker::Instance()->CreateMaterialResource2(this->matName7.c_str(), this->textureName7.c_str(), &(this->matParams[1 + stride * 6]));
	}
};

// ====================================================================
class CreateMeshResourceQc : public GenericQc {
public:
	Ogre::String meshName;
	Ogre::String contextSceneNodeName;
	float px;
	float py;
	float pz;
	int* faceCounts;
	float* faceVertices;
	float origPriority;
	CreateMeshResourceQc(float prio, Ogre::String uni, 
					const char* mName, const char* contextSN, const int* faceC, const float* faceV) {
		Ogre::SceneNode* contextSceneNode;

		this->priority = prio;
		this->origPriority = prio;
		this->cost = 100;
		this->uniq = uni;
		this->meshName = Ogre::String(mName);
		this->contextSceneNodeName = Ogre::String(contextSN);
		// if there is a context node, use that to get the location of the mesh for later reprioritization
		if (LG::RendererOgre::Instance()->m_sceneMgr->hasSceneNode(contextSN)) {
			contextSceneNode = LG::RendererOgre::Instance()->m_sceneMgr->getSceneNode(contextSN);
			px = contextSceneNode->getPosition().x;
			py = contextSceneNode->getPosition().y;
			pz = contextSceneNode->getPosition().z;
		}
		else {
			px = 10.0;
			py = 10.0;
			pz = 10.0;
		}
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
		Ogre::Vector3 ourLoc = Ogre::Vector3(this->px, this->py, this->pz);
		// this->priority = ourLoc.distance(LG::RendererOgre::Instance()->m_camera->getPosition());
		// for the moment use orig priority until camera is in local coordinates
		this->priority = this->origPriority;
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
		// this->priority = ourLoc.distance(LG::RendererOgre::Instance()->m_camera->getPosition());
		// for the moment use orig priority until camera is local coordinates
		this->priority = this->origPriority;
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
		this->priority = prio;
		this->cost = 3;
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
class AddRegionQc : public GenericQc {
public:
	Ogre::String regionName;
	double px; double py; double pz;
	float sizeX; float sizeY;
	float waterHeight;
	AddRegionQc(float prio,
					const char* regionNm,
					const double gX, const double gY, const double gZ,
					const float szX, const float szY, const float waterHt) {
		this->priority = prio;
		this->cost = 0;
		this->uniq = "";
		this->regionName = Ogre::String(regionNm);
		this->px = gX; this->py = gY; this->pz = gZ;
		this->sizeX = szX; this->sizeY = szY;
		this->waterHeight = waterHt;
	}
	~AddRegionQc(void) {
		this->uniq.clear();
		this->regionName.clear();
	}
	void Process() {
		LG::RegionTracker::Instance()->AddRegion(this->regionName.c_str(),
			this->px, this->py, this->pz, this->sizeX, this->sizeY, this->waterHeight);
	}
};

// ====================================================================
class UpdateTerrainQc : public GenericQc {
public:
	Ogre::String regionName;
	int width; int length;
	float* heightMap;
	UpdateTerrainQc(float prio,
					const char* regionNm,
					const int w, const int l, const float* hm) {
		this->priority = prio;
		this->cost = 0;
		this->uniq = "";
		this->regionName = Ogre::String(regionNm);
		this->width = w; this->length = l;
		this->heightMap = (float*)malloc(w * l * sizeof(float));
		memcpy(this->heightMap, hm, w * l * sizeof(float));
	}
	~UpdateTerrainQc(void) {
		this->uniq.clear();
		this->regionName.clear();
		free(this->heightMap);
	}
	void Process() {
		LG::RegionTracker::Instance()->UpdateTerrain(this->regionName.c_str(),
			this->width, this->length, this->heightMap);
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
	LGLOCK_LOCK(m_workItemMutex);
	RefreshResourceQc* rrq = new RefreshResourceQc(priority, resourceName, resourceName, rType);
	QueueWork((GenericQc*)rrq);
	LGLOCK_UNLOCK(m_workItemMutex);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameRefreshResource);
}

void ProcessBetweenFrame::CreateMaterialResource2(float priority, 
			  const char* matName, const char* texName, const float* parms) {
	LGLOCK_LOCK(m_workItemMutex);
	CreateMaterialResourceQc* cmrq = new CreateMaterialResourceQc(priority, matName, matName, texName, parms);
	QueueWork((GenericQc*)cmrq);
	LGLOCK_UNLOCK(m_workItemMutex);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameCreateMaterialResource);
}
void ProcessBetweenFrame::CreateMaterialResource7(float priority, const char* uniq,
			const char* matName1, const char* matName2, const char* matName3, 
			const char* matName4, const char* matName5, const char* matName6, 
			const char* matName7,
			char* textureName1, char* textureName2, char* textureName3, 
			char* textureName4, char* textureName5, char* textureName6, 
			char* textureName7,
			const float* parms) {
	LGLOCK_LOCK(m_workItemMutex);
	CreateMaterialResource7Qc* cmr7q = new CreateMaterialResource7Qc(priority, uniq, 
			matName1, matName2, matName3, matName4, matName5, matName6, matName7,
			textureName1, textureName2, textureName3, textureName4, textureName5, textureName6, textureName7,
			parms);
	QueueWork((GenericQc*)cmr7q);
	LGLOCK_UNLOCK(m_workItemMutex);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameCreateMaterialResource);
}

void ProcessBetweenFrame::CreateMeshResource(float priority, 
				 const char* meshName, const char* contextSceneNode,
				 const int* faceCounts, const float* faceVertices) {
	LGLOCK_LOCK(m_workItemMutex);
	CreateMeshResourceQc* cmrq = new CreateMeshResourceQc(priority, meshName, meshName, contextSceneNode, faceCounts, faceVertices);
	QueueWork((GenericQc*)cmrq);
	LGLOCK_UNLOCK(m_workItemMutex);
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
	LGLOCK_LOCK(m_workItemMutex);
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
	LGLOCK_UNLOCK(m_workItemMutex);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameCreateMeshSceneNode);
}

void ProcessBetweenFrame::UpdateSceneNode(float priority, char* entName,
					bool setPosition, float px, float py, float pz,
					bool setScale, float sx, float sy, float sz,
					bool setRotation, float ow, float ox, float oy, float oz) {
	LGLOCK_LOCK(m_workItemMutex);
	UpdateSceneNodeQc* usnq = new UpdateSceneNodeQc(priority, entName,
					entName,
					setPosition, px, py, pz,
					setScale, sx, sy, sz,
					setRotation, ow, ox, oy, oz);
	QueueWork((GenericQc*)usnq);
	LGLOCK_UNLOCK(m_workItemMutex);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
	LG::IncStat(LG::StatBetweenFrameUpdateSceneNode);
}

void ProcessBetweenFrame::AddRegion(float priority, const char* rn,
					const double gx, const double gy, const double gz, 
					const float sx, const float sy, const float wh) {
	LGLOCK_LOCK(m_workItemMutex);
	AddRegionQc* arq = new AddRegionQc(priority, rn, gx, gy, gz, sx, sy, wh);
	QueueWork((GenericQc*)arq);
	LGLOCK_UNLOCK(m_workItemMutex);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
}

void ProcessBetweenFrame::UpdateTerrain(float priority, const char* rn, 
										const int w, const int l, const float* ht) {
	LGLOCK_LOCK(m_workItemMutex);
	UpdateTerrainQc* utq = new UpdateTerrainQc(priority, rn, w, l, ht);
	QueueWork((GenericQc*)utq);
	LGLOCK_UNLOCK(m_workItemMutex);
	LG::IncStat(LG::StatBetweenFrameWorkItems);
}

// ====================================================================
// we're between frames, on our own thread so we can do the work without locking
int currentCost;
bool ProcessBetweenFrame::frameEnded(const Ogre::FrameEvent& evt) {
	currentCost = m_numWorkItemsToDoBetweenFrames;
	/*
	if (evt.timeSinceLastFrame < 0.25) {
		currentCost = currentCost + 50;
		if (currentCost > m_numWorkItemsToDoBetweenFrames) currentCost = m_numWorkItemsToDoBetweenFrames;
	}
	else {
		currentCost = currentCost - 50;
	}
	if (currentCost < 100) currentCost = 100;
	*/
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
	// Check to see if uniq is specified and remove any duplicates
	if (!wi->uniq.empty()) {
		// There will be duplicate requests for things. If we already have a request, delete the old
		std::list<GenericQc*>::iterator li;
		for (li = m_betweenFrameWork.begin(); li != m_betweenFrameWork.end(); li++) {
			if (!li._Ptr->_Myval->uniq.empty()) {
				if (wi->uniq == li._Ptr->_Myval->uniq) {
					m_betweenFrameWork.erase(li,li);
					LG::IncStat(LG::StatBetweenFrameDiscardedDups);
					break;
				}
			}
		}
	}
	m_betweenFrameWork.push_back(wi);
	m_modified = true;
}

// return true if there is still work to do
bool ProcessBetweenFrame::HasWorkItems() {
	return !m_betweenFrameWork.empty();
}


bool XXCompareElements(const GenericQc* e1, const GenericQc* e2) {
	return (e1->priority < e2->priority);
}

int repriorityCount = 10;
Ogre::Timer* betweenFrameTimeKeeper = new Ogre::Timer();
void ProcessBetweenFrame::ProcessWorkItems(int numToProcess) {
	unsigned long startTime = betweenFrameTimeKeeper->getMilliseconds();
	unsigned long endTime = startTime + 200;
	// This sort is intended to put the highest priority (ones with lowest numbers) at
	//   the front of the list for processing first.
	if (m_modified) {
		LGLOCK_LOCK(m_workItemMutex);
		if (repriorityCount-- < 0) {
			// periodically ask the items to recalc their priority
			repriorityCount = 10;
			/*
			// temp remove priority recalc. I think this is getting some order dependent
			//    operations (createMesh/refreshMesh for instance) out of order
			std::list<GenericQc*>::iterator li;
			for (li = m_betweenFrameWork.begin(); li != m_betweenFrameWork.end(); li++) {
				li._Ptr->_Myval->RecalculatePriority();
			}
			*/
			m_betweenFrameWork.sort(XXCompareElements);
		}
		LGLOCK_UNLOCK(m_workItemMutex);
		m_modified = false;
	}
	int loopCost = numToProcess;
	// while (!m_betweenFrameWork.empty() && (loopCost > 0) && (betweenFrameTimeKeeper->getMilliseconds() < endTime) ) {
	while (!m_betweenFrameWork.empty() && (betweenFrameTimeKeeper->getMilliseconds() < endTime) ) {
		LGLOCK_LOCK(m_workItemMutex);
		GenericQc* workGeneric = (GenericQc*)m_betweenFrameWork.front();
		m_betweenFrameWork.pop_front();
		LGLOCK_UNLOCK(m_workItemMutex);
		LG::SetStat(LG::StatBetweenFrameWorkItems, m_betweenFrameWork.size());
		LG::IncStat(LG::StatBetweenFrameTotalProcessed);
		try {
			workGeneric->Process();
		}
		catch (...) {
			LG::Log("ProcessBetweenFrame: EXCEPTION PROCESSING:");
		}
		loopCost -= workGeneric->cost;
		delete(workGeneric);
	}
	return;
}

}