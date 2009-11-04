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

#define MESH_STATE_UNKNOWN 0
#define MESH_STATE_BEING_PREPARED 1
#define MESH_STATE_PREPARED 2
#define MESH_STATE_LOADED 3
#define MESH_STATE_UNLOADED 4

namespace OLMeshTracker {
	typedef struct s_meshInfo {
		int state;
		Ogre::String name;
		Ogre::String contextEntityName;
		Ogre::String fingerprint;
	} MeshInfo;

	stdext::hash_map<Ogre::String, MeshInfo> m_meshMap;
	typedef std::pair<Ogre::String, MeshInfo> MapPair;
	LGLOCK_MUTEX m_mapLock;

	OLMeshTracker::OLMeshTracker(RendererOgre::RendererOgre* ro) {
		m_ro = ro;
		m_mapLock = LGLOCK_ALLOCATE_MUTEX("OLMeshTracker");
	}
	OLMeshTracker::~OLMeshTracker() {
		LGLOCK_RELEASE_MUTEX(m_mapLock);
	}

	void OLMeshTracker::TrackMesh(Ogre::String meshNameP, Ogre::String contextEntNameP, Ogre::String fingerprintP) {
		MeshInfo* meshToTrack = (MeshInfo*)malloc(sizeof(MeshInfo));
		meshToTrack->name = meshNameP;
		meshToTrack->contextEntityName = contextEntNameP;
		meshToTrack->fingerprint = fingerprintP;
		LGLOCK_LOCK(m_mapLock);
		m_meshMap.insert(MapPair(meshNameP, *meshToTrack));
		LGLOCK_UNLOCK(m_mapLock);

	}
	void OLMeshTracker::UnTrackMesh(Ogre::String meshName) {
	}
	void OLMeshTracker::MakeLoaded(Ogre::String meshName) {
		// already in work list?
		//     MESH_STATE_BEING_PREPARED
		//         return
		//     MESH_STATE_PREPARED
		//         return
		//     MESH_STATE_LOADED
		//         return
		//     MESH_STATE_UNLOADED
		//         break
		// otherwise, set state to BEING_PREPARED and queue prepare op
	}
	void OLMeshTracker::MakeUnLoaded(Ogre::String meshName) {
	}
	Ogre::String OLMeshTracker::GetMeshContext(Ogre::String meshName) {
	}
	Ogre::String OLMeshTracker::GetSimilarMesh(Ogre::String fingerprint) {
	}
}