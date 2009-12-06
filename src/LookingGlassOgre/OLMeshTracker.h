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
		stdext::hash_map<Ogre::String, GenericQm*>::const_iterator intr;
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
		stdext::hash_map<Ogre::String, GenericQm*>::iterator intr;
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
		stdext::hash_map<Ogre::String, GenericQm*>::iterator intr;
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
	stdext::hash_map<Ogre::String, GenericQm*> m_workQueue;
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
			if (nam == li._Ptr->_Myval->uniq) {
				ret = li._Ptr->_Myval;
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
			if (nam == li._Ptr->_Myval->uniq) {
				m_workQueue.erase(li);
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

	void MakeLoaded(Ogre::String meshName, Ogre::String, Ogre::String, Ogre::Entity*);
	void MakeUnLoaded(Ogre::String meshName, Ogre::String, Ogre::Entity*);
	void MakePersistant(Ogre::String meshName, Ogre::String entName, Ogre::String, Ogre::Entity*);

private:
	static OLMeshTracker* m_instance;

	LGLOCK_THREAD m_processingThread;
	static void ProcessThreadRoutine();

	Ogre::String m_cacheDir; 

	MeshWorkQueue* m_meshesToLoad;
	MeshWorkQueue* m_meshesToUnload;
	MeshWorkQueue* m_meshesToSerialize;

	/*
	stdext::hash_map<Ogre::String, MeshInfo*> meshesToLoad;
	stdext::hash_map<Ogre::String, MeshInfo*> meshesToUnload;
	stdext::hash_map<Ogre::String, MeshInfo*> meshesToSerialize;
	*/
};
}