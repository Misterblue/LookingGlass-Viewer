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
#include "LookingGlassOgre.h"
#include "OgreResourceBackgroundQueue.h"
#include "LGLocking.h"

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
	int cost;
	Ogre::String uniq;			// the unique used to find the entity
	Ogre::String stringParam;
	Ogre::Entity* entityParam;
	virtual void Process() {};
	virtual void Abort() {};
	virtual void RecalculatePriority() {};
	GenericQm() {
		priority = 100;
		cost = 10;
	};
	~GenericQm() {};
};

// ===================================================================
#ifdef MTUSEHASHEDLIST
class MeshWorkQueue {
public:
	MeshWorkQueue(Ogre::String nam, int statIndex) {
		m_queueName = nam;
		m_statIndex = statIndex;
		m_queueLength = 0;
	}
	~MeshWorkQueue() {
	}

	GenericQm* Find(Ogre::String nam) {
		GenericQm* ret = NULL;
		std::map<Ogre::String, GenericQm*>::const_iterator intr;
		intr = m_workQueue.find(nam);
		if (intr != m_workQueue.end()) {
			ret = intr->second;
		}
		return ret;
	}
	bool isEmpty() {
		return m_workQueue.empty();
	}
	void Remove(Ogre::String nam) {
		std::map<Ogre::String, GenericQm*>::iterator intr;
		intr = m_workQueue.find(nam);
		if (intr != m_workQueue.end()) {
			m_workQueue.erase(intr);
			m_queueLength--;
			LG::SetStat(m_statIndex, m_queueLength);
		}
	}
	void AddLast(GenericQm* gq) {
		m_workQueue.insert(std::pair<Ogre::String, GenericQm*>(gq->uniq, gq));
		m_queueLength++;
		LG::IncStat(LG::StatMeshTrackerTotalQueued);
		LG::SetStat(m_statIndex, m_queueLength);
	}
	GenericQm* GetFirst() {
		GenericQm* ret = NULL;
		std::map<Ogre::String, GenericQm*>::iterator intr;
		intr = m_workQueue.begin();
		if (intr != m_workQueue.end()) {
			ret = intr->second;
			m_workQueue.erase(intr);
			m_queueLength--;
			LG::SetStat(m_statIndex, m_queueLength);
		}
		else {
			m_queueLength = 0;
		}
		return ret;
	}
private:
	std::map<Ogre::String, GenericQm*> m_workQueue;
	int m_statIndex;
	Ogre::String m_queueName;
	int m_queueLength;
};
#else
class MeshWorkQueue {
public:
	MeshWorkQueue(Ogre::String nam, int statIndex) {
		m_queueName = nam;
		m_statIndex = statIndex;
	}
	~MeshWorkQueue() {
	}
	GenericQm* Find(Ogre::String nam) {
		GenericQm* ret = NULL;
		std::list<GenericQm*>::iterator li;
		for (li = m_workQueue.begin(); li != m_workQueue.end(); li++) {
			if (nam == (*li)->uniq) {
				ret = *li;
				break;
			}
		}
		return ret;
	}
	bool isEmpty() {
		return m_workQueue.empty();
	}
	void Remove(Ogre::String nam) {
		std::list<GenericQm*>::iterator li;
		for (li = m_workQueue.begin(); li != m_workQueue.end(); li++) {
			if (nam == (*li)->uniq) {
				GenericQm* found = *li;
				m_workQueue.erase(li);
				delete(found);
				break;
			}
		}
		LG::SetStat(m_statIndex, m_workQueue.size());
	}
	void AddLast(GenericQm* gq) {
		m_workQueue.push_back(gq);
		LG::IncStat(LG::StatMeshTrackerTotalQueued);
		LG::SetStat(m_statIndex, m_workQueue.size());
	}
	GenericQm* GetFirst() {
		GenericQm* ret = m_workQueue.front();
		m_workQueue.pop_front();
		LG::SetStat(m_statIndex, m_workQueue.size());
		return ret;
	}
private:
	Ogre::String m_queueName;
	std::list<GenericQm*> m_workQueue;
	int m_statIndex;
	int m_queueLength;
};

#endif

// ===================================================================
class OLMeshTracker : public SingletonInstance, public Ogre::FrameListener {
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
	bool KeepProcessing;	// true if to keep processing on and on
	LGLOCK_MUTEX MeshTrackerLock;
	void ProcessWorkItems(int);

	// Ogre::FrameListener
	bool frameEnded(const Ogre::FrameEvent&);

	Ogre::MeshSerializer* MeshSerializer;

	// SingletonInstance.Shutdown()
	void Shutdown();

	void RequestMesh(Ogre::String meshName, Ogre::String context);
	void MakeLoaded(Ogre::String meshName, Ogre::String, Ogre::String, Ogre::Entity*);
	void MakeLoaded(Ogre::SceneNode* sceneNode, Ogre::String meshName, Ogre::String entityName);
	void MakeUnLoaded(Ogre::String meshName, Ogre::String, Ogre::Entity*);
	void MakeUnLoadedLocked(Ogre::String meshName, Ogre::String, Ogre::Entity*);
	void DoReload(Ogre::MeshPtr meshP);
	void DoReload(Ogre::String meshName);
	void MakePersistant(Ogre::String meshName, Ogre::String entName, Ogre::String, Ogre::Entity*);
	void DeleteMesh(Ogre::MeshPtr mesh);

	void UpdateSceneNodesForMesh(Ogre::String meshName);
	void UpdateSceneNodesForMesh(Ogre::MeshPtr ptr);

private:
	static OLMeshTracker* m_instance;

	bool m_shouldQueueMeshOperations;	// false if not to queue. Just do it.

#if OGRE_THREAD_SUPPORT > 0
	LGLOCK_THREAD m_processingThread;
#endif
	static void ProcessThreadRoutine();

	Ogre::String m_cacheDir; 

	MeshWorkQueue* m_meshesToLoad;
	MeshWorkQueue* m_meshesToUnload;
	MeshWorkQueue* m_meshesToSerialize;

	typedef std::map<Ogre::String, unsigned long> RequestedMeshHashMap;
	RequestedMeshHashMap m_requestedMeshes;
	Ogre::Timer* m_meshTimeKeeper;
	void UpdateSubNodes(Ogre::Node* regionNode, Ogre::Node* node, bool recurse, Ogre::MeshPtr meshP);
};
}