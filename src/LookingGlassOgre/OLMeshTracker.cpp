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

LGLOCK_MUTEX m_mapLock;

OLMeshTracker::OLMeshTracker() {
	m_mapLock = LGLOCK_ALLOCATE_MUTEX("OLMeshTracker");
	m_meshSerializer = NULL;
	m_cacheDir = LG::GetParameter("Renderer.Ogre.CacheDir");
	m_meshesToLoad = new HashedQueueNamedList(LG::StatMeshTrackerLoadQueued);
	m_meshesToUnload = new HashedQueueNamedList(LG::StatMeshTrackerUnloadQueued);
	m_meshesToSerialize = new HashedQueueNamedList(LG::StatMeshTrackerSerializedQueued);
}
OLMeshTracker::~OLMeshTracker() {
	LGLOCK_RELEASE_MUTEX(m_mapLock);
}

// SingletonInstance.Shutdown()
void OLMeshTracker::Shutdown() {
	return;
}

// ===============================================================================
class MakeLoadedQm : public GenericQm {
public:
	Ogre::String meshName;
	Ogre::String contextEntity;
	MakeLoadedQm(float prio, Ogre::String uni, 
				Ogre::String meshNam Ogre::String contextEnt, Ogre::String stringParm, Ogre::Entity* entityParam)
		this->priority = prio
		this->cost = 0;
		this->uniq = uni;
		this->meshName = meshNam;
		this->contextEntity = contextEnt;
		this->stringParam = strintParm;
		this->entityParam = entityParm;
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
// ===============================================================================
// Make the mesh loaded. We find the mesh entry and check if it's loaded. If not
// we do the prepare operation (file IO) on our own thread. Once the IO is complete,
// we schedule a refresh resource to add the mesh to the scene.
// Callback called when mesh loaded. Called on between frame thread and passed the Param
// as the only parameter. If 'callback' NULL, nothing is done;
void OLMeshTracker::MakeLoaded(Ogre::String meshName, Ogre::String contextEntity, Ogre::String stringParam, Ogre::Entity* entityParam) {
	LGLOCK_LOCK(m_mapLock);
	// check to see if in unloaded list, if so, remove it and claim success
	GenericQm* unloadEntry = m_meshesToUnload(meshName);
	if (unloadEntry != NULL) {
		unloadEntry->Abort();
		delete unloadEntry;
	}
	// add this to the loading list
	MakeLoadedQm* mlq = new MakeLoadedQm(meshName, contextEntity, stringParam, entityParam);
	m_meshesToLoad.AddLast(mlq);
	LGLOCK_UNLOCK(m_mapLock);
}

// Make the mesh unloaded. Schedule the unload operation on our own thread
// unloads are quick
void OLMeshTracker::MakeUnLoaded(Ogre::String meshName, Ogre::String stringParam, Ogre::Entity* entityParam) {
	LGLOCK_LOCK(m_mapLock);
	// see if in the loading list. Remove if  there.
	GenericQm* loadEntry = m_meshesToLoad(meshName);
	if (loadEntry != NULL) {
		loadEntry->Abort();
		delete loadEntry;
	}
	// see if in the serialize list. Mark for unload if it's there
	GenericQm* serialEntry = m_meshesToSerialize(meshName);
	if (serialEntry != NULL) {
		serialEntry->stringParam = "unload";
	}
	else {
		Ogre::MeshManager::getSingleton().unload(meshName);
	}
	LGLOCK_UNLOCK(m_mapLock);
}

// Serialize the mesh to it's file on our own thread.
void OLMeshTracker::MakePersistant(Ogre::String meshName, Ogre::String entName, Ogre::String stringParam, Ogre::Entity* entityParam) {
	MakePersistant((Ogre::MeshPtr)Ogre::MeshManager::getSingleton().getByName(meshName), entName, stringParam, entityParam);
}

// TODO: make this inline code happen on it's own thread
void OLMeshTracker::MakePersistant(Ogre::MeshPtr mesh, Ogre::String entName, Ogre::String stringParam, Ogre::Entity* entityParam) {
	LGLOCK_LOCK(m_mapLock);
	// check to see if in unloaded list, if so, remove it and claim success
	GenericQm* unloadEntry = m_meshesToUnload(meshName);
	if (unloadEntry != NULL) {
		unloadEntry->Abort();
		delete unloadEntry;
	}
	MakeSerializedQm* msq = new MakeSerializedQm(meshName, contextEntity, stringParam, entityParam);
	m_meshesToSerialize.AddLast(msq);
	LGLOCK_UNLOCK(m_mapLock);

/*
	Ogre::String targetFilename = LG::RendererOgre::Instance()->EntityNameToFilename(entName, "");

	// Make sure the directory exists -- I wish the serializer did this for me
	LG::RendererOgre::Instance()->CreateParentDirectory(targetFilename);
	
	if (m_meshSerializer == NULL) {
		m_meshSerializer = new Ogre::MeshSerializer();
	}
	m_meshSerializer->exportMesh(mesh.getPointer(), targetFilename);
*/

}

}