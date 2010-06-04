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
#include "OLMeshTracker.h"
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>
#include <sys/stat.h>
#include "RendererOgre.h"
#include "ProcessBetweenFrame.h"
#include "LGLocking.h"

/*
NOTE TO THE NEXT PERSON: CODE NOT COMPLETE OR HOOKED INTO MAIN CODE
This code is started but not complete. The idea is to create a routine that
tracks meshes and their state (loaded, unloaded, ...) with the goal of allowing
the actual file access part of a mesh load (the call to mesh->prepare()) be
done outside the frame rendering thread.
*/

#define MESH_STATE_UNKNOWN	0			// no one knows
#define MESH_STATE_REQUESTING 1			// request to managed code to define the mesh
#define MESH_STATE_BEING_SERIALIZED 3	// request to serialize mesh is scheduled
#define MESH_STATE_SERIALIZE_THEN_UNLOAD 4	// mesh is being serialized but then it should be unloaded
#define MESH_STATE_BEING_PREPARED 5		// request in queue to do file IO for loading
#define MESH_STATE_PREPARED 6			// all information in memory
#define MESH_STATE_LOADED 7				// mesh is loaded and being rendered
#define MESH_STATE_UNLOADED 8			// mesh is unloaded and unrenderable

namespace LG {

OLMeshTracker* OLMeshTracker::m_instance = NULL;

OLMeshTracker::OLMeshTracker() {
	MeshTrackerLock = LGLOCK_ALLOCATE_MUTEX("OLMeshTracker");
	m_cacheDir = LG::GetParameter("Renderer.Ogre.CacheDir");

	// This routine can do fancy queuing and delayed loading and loading of meshes if
	// the next variable is 'true'. If the variable is 'false', all operations are done
	// immediately. I have found that the queuing creates delays in display without
	// appreciatably speeding processing up.
	m_shouldQueueMeshOperations = LG::GetParameterBool("Renderer.Ogre.ShouldQueueMeshOperations");

	m_meshesToLoad = new MeshWorkQueue("MeshesToLoad", LG::StatMeshTrackerLoadQueued);
	m_meshesToUnload = new MeshWorkQueue("MeshesToUnload", LG::StatMeshTrackerUnloadQueued);
	m_meshesToSerialize = new MeshWorkQueue("MeshesToSerialize", LG::StatMeshTrackerSerializedQueued);
	MeshSerializer = new Ogre::MeshSerializer();
	m_meshTimeKeeper = new Ogre::Timer();
	// for the moment, don't try to use an extra thread
	LGLOCK_THREAD_INITIALIZING;
#if OGRE_THREAD_SUPPORT > 0
	m_processingThread = LGLOCK_ALLOCATE_THREAD(&ProcessThreadRoutine);
#else
	LG::GetOgreRoot()->addFrameListener(this);
	LGLOCK_THREAD_INITIALIZED;
#endif
	LG::OLMeshTracker::KeepProcessing = true;
}
OLMeshTracker::~OLMeshTracker() {
	this->KeepProcessing = false;
	LGLOCK_RELEASE_MUTEX(MeshTrackerLock);
}

// SingletonInstance.Shutdown()
void OLMeshTracker::Shutdown() {
	this->KeepProcessing = false;
	return;
}

// ====================================================================
// we're between frames, on our own thread so we can do the work without locking
bool OLMeshTracker::frameEnded(const Ogre::FrameEvent& evt) {
	LG::StatIn(LG::InOutMeshTracker);
	LGLOCK_ALOCK trackerLock;		// a lock that will be released if we have an exception
	LG::OLMeshTracker* inst = LG::OLMeshTracker::Instance();
	trackerLock.Lock(inst->MeshTrackerLock);
	ProcessWorkItems(100);
	trackerLock.Unlock();
	LG::StatOut(LG::InOutMeshTracker);
	return true;
}

void OLMeshTracker::ProcessThreadRoutine() {
	// Ogre needs to know about our thrread context
	LG::Log("OLMeshTracker::ProcessThreadRoutine: Registering mesh tracker thread with render system");
	try {
		LG::RendererOgre::Instance()->m_root->getRenderSystem()->registerThread();
	}
	catch (Ogre::Exception e) {
		LG::Log("OLMeshTracker::ProcessThreadRoutine: thread register threw: %s", e.getDescription().c_str());
	}
	LGLOCK_THREAD_INITIALIZED;

	LG::OLMeshTracker* inst = LG::OLMeshTracker::Instance();
	while (inst->KeepProcessing) {
		LGLOCK_LOCK(inst->MeshTrackerLock);
		if (inst->m_meshesToLoad->isEmpty() && inst->m_meshesToUnload->isEmpty() && inst->m_meshesToSerialize->isEmpty()) {
			LGLOCK_WAIT(inst->MeshTrackerLock);
		}
		try {
			inst->ProcessWorkItems(100);
		}
		catch (...) {
		}
		LGLOCK_UNLOCK(inst->MeshTrackerLock);
	}
}

void OLMeshTracker::ProcessWorkItems(int totalCost) {
	int runningCost = totalCost;
	GenericQm* operate;
	LG::OLMeshTracker* inst = LG::OLMeshTracker::Instance();
	while (runningCost > 0) {
		operate = NULL;
		// get an work entry from one of the lists
		if (!inst->m_meshesToLoad->isEmpty()) {
			operate = inst->m_meshesToLoad->GetFirst();
		}
		else {
			if (!inst->m_meshesToUnload->isEmpty()) {
				operate = inst->m_meshesToUnload->GetFirst();
			}
			else {
				if (!inst->m_meshesToSerialize->isEmpty()) {
					operate = inst->m_meshesToSerialize->GetFirst();
				}
			}
		}
		if (operate != NULL) {
			try {
				operate->Process();
			}
			catch (...) {
				LG::Log("ProcessBetweenFrame: EXCEPTION PROCESSING: %s", operate->uniq.c_str());
			}
			runningCost -= operate->cost;
			LG::Log("OLMeshTracker::ProcessWorkItems: c=%d, u=%s", runningCost, operate->uniq.c_str());

			delete(operate);
		}
		else {
			// nothing more to do, bail
			runningCost = 0;
		}
	}
	return;
}

// ===============================================================================
class MakeMeshLoadedQm : public GenericQm {
public:
	Ogre::String meshName;
	Ogre::String contextEntity;
	MakeMeshLoadedQm(float prio, Ogre::String meshNam, Ogre::String contextEnt, 
					Ogre::String stringParm, Ogre::Entity* entityParm) {
		this->priority = prio;
		this->meshName = meshNam;
		this->uniq = meshNam;
		this->contextEntity = contextEnt;
		this->stringParam = stringParm;
		this->entityParam = entityParm;
	}
	~MakeMeshLoadedQm(void) {
		this->meshName.clear();
		this->uniq.clear();
		this->contextEntity.clear();
		this->stringParam.clear();
	}
	void Process() {
		LG::Log("OLMeshTracker::MakeLoadedQm: loading: %s (%s)", meshName.c_str(), this->stringParam.c_str());
		Ogre::MeshManager::getSingleton().load(this->meshName, OLResourceGroupName);
		if ((stringParam == "visible") && (this->entityParam != NULL)) {
			LG::Log("OLMeshTracker::MakeLoadedQm: making visible");
			this->entityParam->setVisible(true);
		}
	}
};

// ===============================================================================
class MakeMeshLoaded2Qm : public GenericQm {
public:
	Ogre::SceneNode* sceneNode;
	Ogre::String meshName;
	Ogre::String entityName;
	MakeMeshLoaded2Qm(float prio, Ogre::String uniq, Ogre::SceneNode* sceneNod,
							Ogre::String meshNam, Ogre::String entNam) {
		this->priority = prio;
		this->uniq = uniq;
		this->sceneNode = sceneNod;
		this->meshName = meshNam;
		this->entityName = entNam;
	}
	~MakeMeshLoaded2Qm(void) {
		this->uniq.clear();
	}
	void Process() {
		// LG::Log("OLMeshTracker::MakeLoaded2Qm: loading: %s", meshName.c_str());
		Ogre::MeshManager::getSingleton().load(this->meshName, OLResourceGroupName);
		LG::ProcessBetweenFrame::Instance()->AddLoadedMesh(0, Ogre::String(""), entityName, meshName, sceneNode);
		/*
		// functionality moved into between frame processing
		Ogre::MovableObject* ent = LG::RendererOgre::Instance()->m_sceneMgr->createEntity(this->entityName, this->meshName);
		// it's not scenery
		ent->removeQueryFlags(Ogre::SceneManager::WORLD_GEOMETRY_TYPE_MASK);	
		LG::RendererOgre::Instance()->Shadow->AddCasterShadow(ent);
		this->sceneNode->attachObject(ent);
		LG::RendererOgre::Instance()->m_visCalc->RecalculateVisibility();
		*/
	}
};

// ===============================================================================
class MakeMeshSerializedQm : public GenericQm {
public:
	Ogre::String meshName;
	Ogre::String contextEntity;
	MakeMeshSerializedQm(float prio, Ogre::String meshNam, Ogre::String contextEnt, 
					Ogre::String stringParm, Ogre::Entity* entityParm) {
		this->priority = prio;
		this->meshName = meshNam;
		this->uniq = meshNam;
		this->contextEntity = contextEnt;
		this->stringParam = stringParm;
		this->entityParam = entityParm;
	}
	~MakeMeshSerializedQm(void) {
		this->meshName.clear();
		this->uniq.clear();
		this->contextEntity.clear();
		this->stringParam.clear();
	}
	void Process() {
		Ogre::String targetFilename = LG::RendererOgre::Instance()->EntityNameToFilename(this->meshName, Ogre::String(""));

		// Make sure the directory exists -- I wish the serializer did this for me
		LG::RendererOgre::Instance()->CreateParentDirectory(targetFilename);
		LG::Log("OLMeshTracker::MakePersistant: persistance to %s", targetFilename.c_str());
		
		Ogre::MeshPtr meshHandle = (Ogre::MeshPtr)Ogre::MeshManager::getSingleton().getByName(meshName);
		LG::OLMeshTracker::Instance()->MeshSerializer->exportMesh(meshHandle.getPointer(), targetFilename);
		if (this->stringParam == "unload") {
			LG::Log("OLMeshTracker::MakePersistant: queuing unload after persistance");
			// if we're supposed to unload after serializing, schedule that to happen
			LG::OLMeshTracker::Instance()->MakeUnLoaded(this->meshName, Ogre::String(), NULL);
		}
	}
};

// ===============================================================================
class ReloadMeshQm : public GenericQm {
public:
	Ogre::String meshName;
	Ogre::MeshPtr meshP;
	ReloadMeshQm(float prio, Ogre::MeshPtr meshpp) {
		this->priority = prio;
		this->meshP = meshpp;
		this->uniq = meshpp->getName();
	}
	~ReloadMeshQm(void) {
		this->meshName.clear();
		this->uniq.clear();
	}
	void Process() {
		meshP->reload();
		LG::OLMeshTracker::Instance()->UpdateSceneNodesForMesh(meshP);
	}
};

// ===============================================================================
// Called from the filesystem when it is discovered that there is not a mesh file.
// Suppress multiple requests for the same mesh.
void OLMeshTracker::RequestMesh(Ogre::String meshName, Ogre::String context) {
	unsigned long now = m_meshTimeKeeper->getMilliseconds();
	RequestedMeshHashMap::iterator intr = m_requestedMeshes.find(meshName);
	if (intr == m_requestedMeshes.end()) {
		// we haven't seen this material before. Remember and request
		m_requestedMeshes.insert(std::pair<Ogre::String,unsigned long>(meshName, now));
		LG::RequestResource(meshName.c_str(), context.c_str(), LG::ResourceTypeMesh);
	}
	else {
		// see if it's been 10 seconds since we asked for this material
		if ((intr->second + 10000) > now) {
			// been a while. Reset timer and ask for the material
			intr->second = now;
			LG::RequestResource(meshName.c_str(), context.c_str(), LG::ResourceTypeMesh);
		}
	}
}

// ===============================================================================
// Make the mesh loaded. 
// Make sure the mesh we're loading is not in the unloading list.
// 'stringParam' is the group name for the mesh. If the entity pointer is non-NULL
// we do a 'setVisible(true)' on it once the mesh is loaded.
// we do the prepare operation (file IO) on our own thread. Once the IO is complete,
// we schedule a refresh resource to add the mesh to the scene.
void OLMeshTracker::MakeLoaded(Ogre::String meshName, Ogre::String contextEntity, Ogre::String stringParam, Ogre::Entity* entityParam) {
	if (!m_shouldQueueMeshOperations) {
		MakeMeshLoadedQm* mmlq = new MakeMeshLoadedQm(10, meshName, contextEntity, stringParam, entityParam);
		mmlq->Process();
		delete(mmlq);
		return;
	}
	LGLOCK_ALOCK trackerLock;
	trackerLock.Lock(MeshTrackerLock);
	// check to see if in unloaded list, if so, remove it and claim success
	GenericQm* unloadEntry = m_meshesToUnload->Find(meshName);
	if (unloadEntry != NULL) {
		unloadEntry->Abort();
		m_meshesToUnload->Remove(meshName);
		LG::Log("OLMeshTracker::MakeLoaded: removing one from unload list: %s", meshName.c_str());
	}
	// add this to the loading list
	LG::Log("OLMeshTracker::MakeLoaded: queuing loading: %s (%s)", meshName.c_str(), stringParam.c_str());
	GenericQm* loadEntry = m_meshesToLoad->Find(meshName);
	if (loadEntry == NULL) {
		MakeMeshLoadedQm* mmlq = new MakeMeshLoadedQm(10, meshName, contextEntity, stringParam, entityParam);
		m_meshesToLoad->AddLast(mmlq);
	}
	trackerLock.Unlock();
	LGLOCK_NOTIFY_ALL(MeshTrackerLock);
}

// ===============================================================================
// Make the mesh loaded then create an entity for the mesh. This is used
// when creating the scene graph with the mesh since the 'createEntity' call
// forces a load of the mesh. This does the load on the other thread and then
// creates the entity.
void OLMeshTracker::MakeLoaded(Ogre::SceneNode* sceneNode, Ogre::String meshName, Ogre::String entityName) {
	if (!m_shouldQueueMeshOperations) {
		MakeMeshLoaded2Qm* mmlq = new MakeMeshLoaded2Qm(10, meshName, sceneNode, meshName, entityName);
		mmlq->Process();
		delete(mmlq);
		return;
	}
	LGLOCK_ALOCK trackerLock;
	trackerLock.Lock(MeshTrackerLock);
	// check to see if in unloaded list, if so, remove it and claim success
	GenericQm* unloadEntry = m_meshesToUnload->Find(meshName);
	if (unloadEntry != NULL) {
		unloadEntry->Abort();
		m_meshesToUnload->Remove(meshName);
		LG::Log("OLMeshTracker::MakeLoaded: removing one from unload list: %s", meshName.c_str());
	}
	// add this to the loading list
	// LG::Log("OLMeshTracker::MakeLoaded: queuing loading: %s", meshName.c_str());
	GenericQm* loadEntry = m_meshesToLoad->Find(meshName);
	if (loadEntry == NULL) {
		MakeMeshLoaded2Qm* mmlq = new MakeMeshLoaded2Qm(10, meshName, sceneNode, meshName, entityName);
		m_meshesToLoad->AddLast(mmlq);
	}
	trackerLock.Unlock();
	LGLOCK_NOTIFY_ALL(MeshTrackerLock);
}


// ===============================================================================
// Make the mesh unloaded. Schedule the unload operation on our own thread
// unloads are quick
void OLMeshTracker::MakeUnLoaded(Ogre::String meshName, Ogre::String stringParam, Ogre::Entity* entityParam) {
	LGLOCK_ALOCK trackerLock;
	trackerLock.Lock(MeshTrackerLock);
	// see if in the loading list. Remove if  there.
	GenericQm* loadEntry = m_meshesToLoad->Find(meshName);
	if (loadEntry != NULL) {
		loadEntry->Abort();
		m_meshesToLoad->Remove(meshName);
	}
	// see if in the serialize list. Mark for unload if it's there
	GenericQm* serialEntry = m_meshesToSerialize->Find(meshName);
	if (serialEntry != NULL) {
		serialEntry->stringParam = "unload";
	}
	else {
		Ogre::MeshPtr meshP = Ogre::MeshManager::getSingleton().getByName(meshName);
		if (!meshP.isNull()) {
			if (meshP.useCount() == 1) {
				meshP->unload();
			}
			else {
				LG::Log("OLMeshTracker::MakeUnLoaded: Didn't unload mesh because count = %d", meshP.useCount());
			}
		}
	}
	trackerLock.Unlock();
	UpdateSceneNodesForMesh(meshName);
}

// ===============================================================================
// Reload the mesh if we should
// This is usually called when a resource needs to be refreshed
void OLMeshTracker::DoReload(Ogre::String meshName) {
	Ogre::MeshPtr meshP = (Ogre::MeshPtr)Ogre::MeshManager::getSingleton().getByName(meshName);
	if (!meshP.isNull()) {
		DoReload(meshP);
	}
}

void OLMeshTracker::DoReload(Ogre::MeshPtr meshP) {
	if (!m_shouldQueueMeshOperations) {
		ReloadMeshQm* rmq = new ReloadMeshQm(10, meshP);
		rmq->Process();
		delete(rmq);
		return;
	}
	LGLOCK_ALOCK trackerLock;
	trackerLock.Lock(MeshTrackerLock);
	// check to see if in unloaded list, if so, remove it and claim success
	Ogre::String meshName = meshP->getName();
	GenericQm* reloadEntry = m_meshesToLoad->Find(meshName);
	if (reloadEntry != NULL) {
		// if already being loaded, we're done
		LG::Log("OLMeshTracker::DoReload: asked to reload but already loading: %s", meshName.c_str());
		trackerLock.Unlock();
		return;
	}
	// add this to the loading list
	LG::Log("OLMeshTracker::DoReload: queuing reloading: %s", meshName.c_str());
	ReloadMeshQm* rmq = new ReloadMeshQm(10, meshP);
	m_meshesToLoad->AddLast(rmq);
	trackerLock.Unlock();
	LGLOCK_NOTIFY_ALL(MeshTrackerLock);
	/*	OLD CODE
	// for the moment, don't do anything fancy
	if (meshP->isLoaded()) {
		meshP->reload();
	}
	UpdateSceneNodesForMesh(meshP);
	// do we need to call update on all scene nodes that use this mesh?
	*/
}

// ===============================================================================
// Serialize the mesh to it's file on our own thread.
void OLMeshTracker::MakePersistant(Ogre::String meshName, Ogre::String entName, Ogre::String stringParm, Ogre::Entity* entityParm) {
	if (!m_shouldQueueMeshOperations) {
		MakeMeshSerializedQm* msq = new MakeMeshSerializedQm(10, meshName, entName, stringParm, entityParm);
		msq->Process();
		delete(msq);
		return;
	}
	LGLOCK_ALOCK trackerLock;
	trackerLock.Lock(MeshTrackerLock);
	// check to see if in unloaded list, if so, remove it
	GenericQm* unloadEntry = m_meshesToUnload->Find(meshName);
	if (unloadEntry != NULL) {
		LG::Log("OLMeshTracker::MakePersistant: removing one from unload list: %s", meshName.c_str());
		unloadEntry->Abort();
		m_meshesToUnload->Remove(meshName);
	}
	LG::Log("OLMeshTracker::MakePersistant: queuing persistance for %s", meshName.c_str());
	MakeMeshSerializedQm* msq = new MakeMeshSerializedQm(10, meshName, entName, stringParm, entityParm);
	m_meshesToSerialize->AddLast(msq);
	trackerLock.Unlock();
	LGLOCK_NOTIFY_ALL(MeshTrackerLock);
}

// ===============================================================================
// Someone wants to delete this mesh. Check to see if it's a shared mesh and decide if we
// should actually delete it or not.
void OLMeshTracker::DeleteMesh(Ogre::MeshPtr mesh) {
	Ogre::MeshManager::getSingleton().remove(mesh->getName());
}

// ===============================================================================
// We updated a mesh. Now we have to find all the scene nodes that use this mesh
// and tell them something changed.
void OLMeshTracker::UpdateSceneNodesForMesh(Ogre::String meshName) {
	Ogre::MeshPtr meshP = (Ogre::MeshPtr)Ogre::MeshManager::getSingleton().getByName(meshName);
	if (!meshP.isNull()) {
		UpdateSceneNodesForMesh(meshP);
	}
}

void OLMeshTracker::UpdateSceneNodesForMesh(Ogre::MeshPtr meshP) {
	Ogre::SceneNode* nodeRoot = LG::RendererOgre::Instance()->m_sceneMgr->getRootSceneNode();
	if (nodeRoot == NULL) return;
	// Hanging off the root node will be a node for each 'region'. A region has
	// terrain and then content nodes
	Ogre::SceneNode::ChildNodeIterator rootChildIterator = nodeRoot->getChildIterator();
	while (rootChildIterator.hasMoreElements()) {
		Ogre::Node* nodeRegion = rootChildIterator.getNext();
		// a region node has the nodes of its contents.
		UpdateSubNodes(nodeRegion, nodeRegion, true, meshP);
	}
	return;
}

void OLMeshTracker::UpdateSubNodes(Ogre::Node* regionNode, Ogre::Node* node, bool recurse, Ogre::MeshPtr meshP) {
	if (recurse && node->numChildren() > 0) {
		// if node has more children nodes, visit them recursivily
		Ogre::SceneNode::ChildNodeIterator nodeChildIterator = node->getChildIterator();
		while (nodeChildIterator.hasMoreElements()) {
			Ogre::Node* nodeChild = nodeChildIterator.getNext();
			// 'false' causes it to not visit sub-children which are included in parent and pos relative to parent
			UpdateSubNodes(regionNode, nodeChild, true, meshP);
		}
	}
	// children taken care of... check for attached objects to this node
	Ogre::SceneNode* snode = (Ogre::SceneNode*)node;
	Ogre::SceneNode::ObjectIterator snodeObjectIterator = snode->getAttachedObjectIterator();
	while (snodeObjectIterator.hasMoreElements()) {
		Ogre::MovableObject* snodeObject = snodeObjectIterator.getNext();
		if (snodeObject->getMovableType() == "Entity") {
			Ogre::Entity* snodeEntity = (Ogre::Entity*)snodeObject;
			Ogre::MeshPtr entityMesh = snodeEntity->getMesh();
			if (entityMesh == meshP) {
				snode->needUpdate(true);
				// LG::Log("OLMeshTracker::UpdateSubNodes: setting node update for %s", snode->getName().c_str());
			}
		}
	}
	return;
}
}