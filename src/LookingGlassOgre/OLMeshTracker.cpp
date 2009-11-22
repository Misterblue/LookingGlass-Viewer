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

typedef struct s_meshInfo {
	int state;
	Ogre::String name;
	Ogre::String groupName;
	Ogre::String contextEntityName;
	Ogre::String fingerprint;
} MeshInfo;

typedef stdext::hash_map<Ogre::String, MeshInfo> MeshMap;
typedef std::pair<Ogre::String, MeshInfo> MeshMapPair;
typedef stdext::hash_map<Ogre::String, MeshInfo>::iterator MeshMapIterator;
MeshMap* m_meshMap;
LGLOCK_MUTEX m_mapLock;

OLMeshTracker::OLMeshTracker() {
	m_mapLock = LGLOCK_ALLOCATE_MUTEX("OLMeshTracker");
	m_meshSerializer = NULL;
	m_cacheDir = LG::GetParameter("Renderer.Ogre.CacheDir");
}
OLMeshTracker::~OLMeshTracker() {
	LGLOCK_RELEASE_MUTEX(m_mapLock);
}

// SingletonInstance.Shutdown()
void OLMeshTracker::Shutdown() {
	return;
}

// we have complete information about the mesh. Add or update the table info
void OLMeshTracker::TrackMesh(Ogre::String meshNameP, Ogre::String meshGroupP, Ogre::String contextEntNameP, Ogre::String fingerprintP) {
	MeshInfo* meshToTrack;
	LGLOCK_LOCK(m_mapLock);
	// Look in tracking list to see if are already tracking mesh. 
	MeshMapIterator meshI = m_meshMap->find(meshNameP);
	if (meshI == m_meshMap->end()) {
		// not tracking. Add a new entry
		meshToTrack = (MeshInfo*)malloc(sizeof(MeshInfo));
		meshToTrack->name = meshNameP;
		m_meshMap->insert(MeshMapPair(meshNameP, *meshToTrack));
		meshToTrack->state = MESH_STATE_UNKNOWN;
	}
	else {
		// already tracking
		meshToTrack = &(meshI->second);
	}
	// update the tracking information
	meshToTrack->groupName = meshGroupP;
	meshToTrack->contextEntityName = contextEntNameP;
	meshToTrack->fingerprint = fingerprintP;
	LGLOCK_UNLOCK(m_mapLock);

	// TODO:
	Ogre::MeshPtr meshEnt = (Ogre::MeshPtr)Ogre::MeshManager::getSingleton().getByName(meshNameP);
	if (meshEnt.isNull()) {
		// huh?
	}

}
void OLMeshTracker::UnTrackMesh(Ogre::String meshName) {
}

// Make the mesh loaded. We find the mesh entry and check if it's loaded. If not
// we do the prepare operation (file IO) on our own thread. Once the IO is complete,
// we schedule a refresh resource to add the mesh to the scene.
// Callback called when mesh loaded. Called on between frame thread and passed the Param
// as the only parameter. If 'callback' NULL, nothing is done;
void OLMeshTracker::MakeLoaded(Ogre::String meshName, void(*callback)(void*), void* callbackParam) {
	MeshInfo* meshInfo;
	bool shouldCallback = false;
	LGLOCK_LOCK(m_mapLock);
	MeshMapIterator meshI = m_meshMap->find(meshName);
	if (meshI == m_meshMap->end()) {
		// mesh not found, request the mesh and track what we know
		// LG::RequestResource(meshName.c_str(), contextEntName.c_str(), LookingGlassOgr::ResourceTypeMesh);
		// add mesh to tracking map
		// TODO:
		return;
	}
	meshInfo = &(meshI->second);
	switch (meshInfo->state) {
		case MESH_STATE_UNKNOWN:
		case MESH_STATE_UNLOADED:
			// load the mesh
		case MESH_STATE_REQUESTING:
			// mesh is being requested and will be loaded later
			break;
		case MESH_STATE_BEING_SERIALIZED:
		case MESH_STATE_BEING_PREPARED:
		case MESH_STATE_PREPARED:
		case MESH_STATE_LOADED:
			// mesh is already loaded
			shouldCallback = true;
			break;
		case MESH_STATE_SERIALIZE_THEN_UNLOAD:
			// asked for load after we'd been asked to unload. For get  the unload
			meshInfo->state = MESH_STATE_BEING_SERIALIZED;
			shouldCallback = true;
			break;
	}
	LGLOCK_UNLOCK(m_mapLock);

	if (shouldCallback && callback != NULL) {
		callback(callbackParam);
	}
}

// Make the mesh unloaded. Schedule the unload operation on our own thread
// TODO: replace this inline code with something that happens on a different thread
// Callback called when mesh unloaded. Called on between frame thread and passed the Param
// as the only parameter. If 'callback' NULL, nothing is done;
void OLMeshTracker::MakeUnLoaded(Ogre::String meshName, void(*callback)(void*), void* callbackParam) {
	Ogre::MeshManager::getSingleton().unload(meshName);
	if (callback != NULL) {
		callback(callbackParam);
	}
}

// Serialize the mesh to it's file on our own thread.
void OLMeshTracker::MakePersistant(Ogre::String meshName, Ogre::String entName) {
	MakePersistant((Ogre::MeshPtr)Ogre::MeshManager::getSingleton().getByName(meshName), entName);
}

// TODO: make this inline code happen on it's own thread
void OLMeshTracker::MakePersistant(Ogre::MeshPtr mesh, Ogre::String entName) {
	Ogre::String targetFilename = LG::RendererOgre::Instance()->EntityNameToFilename(entName, "");

	// Make sure the directory exists -- I wish the serializer did this for me
	LG::RendererOgre::Instance()->CreateParentDirectory(targetFilename);
	
	if (m_meshSerializer == NULL) {
		m_meshSerializer = new Ogre::MeshSerializer();
	}
	m_meshSerializer->exportMesh(mesh.getPointer(), targetFilename);
}


Ogre::String OLMeshTracker::GetMeshContext(Ogre::String meshName) {
	return Ogre::String();
}
Ogre::String OLMeshTracker::GetSimilarMesh(Ogre::String fingerprint) {
	return Ogre::String();
}


}