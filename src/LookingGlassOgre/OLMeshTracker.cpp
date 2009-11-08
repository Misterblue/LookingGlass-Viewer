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
#include "RendererOgre.h"
#include "LGLocking.h"

/*
NOTE TO THE NEXT PERSON: CODE NOT COMPLETE OR HOOKED INTO MAIN CODE
This code is started but not complete. The idea is to create a routine that
tracks meshes and their state (loaded, unloaded, ...) with the goal of allowing
the actual file access part of a mesh load (the call to mesh->prepare()) be
done outside the frame rendering thread.
*/

#define MESH_STATE_UNKNOWN	0
#define MESH_STATE_REQUESTING 1
#define MESH_STATE_BEING_PREPARED 2
#define MESH_STATE_PREPARED 3
#define MESH_STATE_LOADED 4
#define MESH_STATE_UNLOADED 5

namespace OLMeshTracker {
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

	OLMeshTracker::OLMeshTracker(RendererOgre::RendererOgre* ro) {
		m_ro = ro;
		m_mapLock = LGLOCK_ALLOCATE_MUTEX("OLMeshTracker");
	}
	OLMeshTracker::~OLMeshTracker() {
		LGLOCK_RELEASE_MUTEX(m_mapLock);
	}

	// we have complete information about the mesh. Add or update the table info
	void OLMeshTracker::TrackMesh(Ogre::String meshNameP, Ogre::String meshGroupP, Ogre::String contextEntNameP, Ogre::String fingerprintP) {
		MeshInfo* meshToTrack;
		LGLOCK_LOCK(m_mapLock);
		MeshMapIterator meshI = m_meshMap->find(meshNameP);
		if (meshI == m_meshMap->end()) {
			meshToTrack = (MeshInfo*)malloc(sizeof(MeshInfo));
			meshToTrack->name = meshNameP;
			m_meshMap->insert(MeshMapPair(meshNameP, *meshToTrack));
			meshToTrack->state = MESH_STATE_UNKNOWN;
		}
		else {
			meshToTrack = &(meshI->second);
		}
		// update other entries
		meshToTrack->groupName = meshGroupP;
		meshToTrack->contextEntityName = contextEntNameP;
		meshToTrack->fingerprint = fingerprintP;
		Ogre::ResourceManager::ResourceCreateOrRetrieveResult theMeshResult = 
					Ogre::MeshManager::getSingleton().createOrRetrieve(meshNameP, meshGroupP);
		Ogre::MeshPtr meshEnt = (Ogre::MeshPtr)theMeshResult.first;
		// TODO:
		LGLOCK_UNLOCK(m_mapLock);

	}
	void OLMeshTracker::UnTrackMesh(Ogre::String meshName) {
	}
	void OLMeshTracker::MakeLoaded(Ogre::String meshName) {
		/*
		if (we aren't tracking this mesh) {
			LookingGlassOgr::RequestResource(meshName.c_str(), contextEntName.c_str(), LookingGlassOgr::ResourceTypeMesh);
			add mesh to tracking map
			TODO:
		}
		*/
	}
	void OLMeshTracker::MakeUnLoaded(Ogre::String meshName) {
	}
	Ogre::String OLMeshTracker::GetMeshContext(Ogre::String meshName) {
		return Ogre::String();
	}
	Ogre::String OLMeshTracker::GetSimilarMesh(Ogre::String fingerprint) {
		return Ogre::String();
	}
}