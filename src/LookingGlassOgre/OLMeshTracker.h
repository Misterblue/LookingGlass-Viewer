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
#pragma once

#include "LGOCommon.h"
#include "SingletonInstance.h"
#include "OgreResourceBackgroundQueue.h"

namespace LG {
/*
NOTE TO THE NEXT PERSON: CODE NOT COMPLETE OR HOOKED INTO MAIN CODE
This code is started but not complete. The idea is to create a routine that
tracks meshes and their state (loaded, unloaded, ...) with the goal of allowing
the actual file access part of a mesh load (the call to mesh->prepare()) be
done outside the frame rendering thread.
*/
// the generic base class that goes in the list
class GenericQm {
public:
	float priority;
	Ogre::String uniq;
	Ogre::String stringParam;
	Ogre::Entity* entityParam;
	virtual void Process() {};
	virtual void Abort() {};
	virtual void RecalculatePriority() {};
	GenericQm() {
		priority = 100;
		uniq.clear();
	};
	~GenericQm() {};
};

// ===================================================================
class MeshQueueNamedList {
public:
	MeshQueueNamedList(int statIndex) {
		m_statIndex = statIndex;
		m_queueLength = 0;
	}
	~MeshQueueNamedList() {
	}

	GenericQm* Find(Ogre::String nam) {
		GenericQm* ret = NULL;
		stdext::hash_map<Ogre::String, GenericQm*>::iterator intr;
		intr = m_hashMap.find(nam);
		if (intr != m_hashMap.end()) {
			ret = intr->second;
		}
		return ret;
	}
	bool isEmpty() {
		return m_hashMap.empty();
	}
	void Remove(GenericQm* mi) {
		Remove(mi->name);
	}
	void Remove(Ogre::String nam) {
		stdext::hash_map<Ogre::String, GenericQm*>::iterator intr;
		intr = m_hashMap.find(nam);
		if (intr != m_hashMap.end()) {
			m_hashMap.erase(intr);
			m_queueLength--;
			LG::SetStat(m_statIndex, m_queueLength);
		}
	}
	void AddLast(GenericQm* mi) {
		m_hashMap.insert(std::pair<Ogre::String, GenericQm*>(mi->name, mi));
		m_queueLength++;
		LG::SetStat(m_statIndex, m_queueLength);
	}
	GenericQm* GetFirst() {
		stdext::hash_map<Ogre::String, GenericQm*>::iterator intr;
		intr = m_hashMap.begin();
		if (intr != m_hashMap.end()) {
			m_hashMap.erase(intr);
			m_queueLength--;
			LG::SetStat(m_statIndex, m_queueLength);
			return intr->second;
		}
		m_queueLength = 0;
		LG::SetStat(m_statIndex, m_queueLength);
		return NULL;
	}
private:
	stdext::hash_map<Ogre::String, GenericQm*> m_hashMap;
	int m_statIndex;
	int m_queueLength;
};

// ===================================================================
class OLMeshTracker : public SingletonInstance {
public:
	OLMeshTracker();
	~OLMeshTracker();

	// SingletonInstance.Instance();
	static OLMeshTracker* Instance() { 
		if (LG::OLMeshTracker::m_instance == NULL) {
			LG::OLMeshTracker::m_instance = new OLMeshTracker();
		}
		return LG::OLMeshTracker::m_instance; 
	}

	// SingletonInstance.Shutdown()
	void Shutdown();

	void MakeLoaded(Ogre::String meshName, Ogre::String, Ogre::String, Ogre::Entity*);
	void MakeUnLoaded(Ogre::String meshName, Ogre::String, Ogre::Entity*);
	void MakePersistant(Ogre::String meshName, Ogre::String entName, Ogre::String, Ogre::Entity*);
	void MakePersistant(Ogre::MeshPtr mesh, Ogre::String entName, Ogre::String, Ogre::Entity*);

private:
	static OLMeshTracker* m_instance;

	Ogre::MeshSerializer* m_meshSerializer;
	Ogre::String m_cacheDir; 

	typedef struct s_meshInfo {
		Ogre::String name;
		Ogre::String groupName;
		Ogre::String contextEntityName;
		Ogre::String fingerprint;
		Ogre::Entity* entityCallbackParam;
		Ogre::String stringCallbackParam;
	} MeshInfo;


	HashedQueueNamedList* m_meshesToLoad;
	HashedQueueNamedList* m_meshesToUnLoad;
	HashedQueueNamedList* m_meshesToSerialize;

	/*
	stdext::hash_map<Ogre::String, MeshInfo*> meshesToLoad;
	stdext::hash_map<Ogre::String, MeshInfo*> meshesToUnload;
	stdext::hash_map<Ogre::String, MeshInfo*> meshesToSerialize;
	*/
};
}