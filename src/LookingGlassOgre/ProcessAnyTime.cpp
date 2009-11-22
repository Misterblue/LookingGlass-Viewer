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

/*
NOTE TO THE NEXT PERSON: CODE NOT COMPLETE OR HOOKED INTO MAIN CODE
This code is started but not complete. The idea is to create a routine that
tracks meshes and their state (loaded, unloaded, ...) with the goal of allowing
the actual file access part of a mesh load (the call to mesh->prepare()) be
done outside the frame rendering thread.
*/

#include "stdafx.h"
#include <stdarg.h>
#include "ProcessAnyTime.h"
#include "LookingGlassOgre.h"
#include "RendererOgre.h"
#include "OLMaterialTracker.h"

namespace LG {

ProcessAnyTime* ProcessAnyTime::m_instance = NULL;

// ====================================================================
// PrepareMesh
// Given a meshname, call the prepare routine to get it loaded.
// Once loaded, we can do the refresh between frame
class PrepareMeshPc : public GenericPc {
	Ogre::String meshName;
	Ogre::String meshGroup;
	PrepareMeshPc(Ogre::String meshN, Ogre::String meshG) {
		this->meshName = meshN;
		this->meshGroup = meshG;
	}
	~PrepareMeshPc() {
		this->meshName.clear();
		this->meshGroup.clear();
	}
	void Process() {
		Ogre::ResourceManager::ResourceCreateOrRetrieveResult theMeshResult = 
					Ogre::MeshManager::getSingleton().createOrRetrieve(this->meshName, this->meshGroup);
		Ogre::MeshPtr theMesh = (Ogre::MeshPtr)theMeshResult.first;
		if (!theMesh->isPrepared()) {
			// read the mesh in from the disk
			theMesh->prepare();
			// when complete, 
			// TODO:
		}

	}
};

// ====================================================================
// Queue of work to do independent of the renderer thread
// Useful for loading meshes and doing other time expensive operations.
// To add a between frame operation, you write a subclass of GenericPc like those
// above, write a routine to create and instance of the class and put it in the
// queue and later, between frames, the Process() routine will be called.
// The constructors and destructors of the *Pc class handles all the allocation
// and deallocation of memory needed to pass the parameters.
ProcessAnyTime::ProcessAnyTime() {
	m_workQueueMutex = LGLOCK_ALLOCATE_MUTEX("ProcessAnyTime");
	m_processingThread = LGLOCK_ALLOCATE_THREAD(&ProcessThreadRoutine);
	m_keepProcessing = true;
	m_modified = false;
}

ProcessAnyTime::~ProcessAnyTime() {
	m_keepProcessing = false;
	LGLOCK_RELEASE_MUTEX(m_workQueueMutex);
}

// static routine to get the thread. Loop around doing work.
void ProcessAnyTime::ProcessThreadRoutine() {
	while (LG::ProcessAnyTime::Instance()->m_keepProcessing) {
		LGLOCK_LOCK(LG::ProcessAnyTime::Instance()->m_workQueueMutex);
		if (!LG::ProcessAnyTime::Instance()->HasWorkItems()) {
			LGLOCK_WAIT(LG::ProcessAnyTime::Instance()->m_workQueueMutex);
		}
		LG::ProcessAnyTime::Instance()->ProcessWorkItems(100);
		LGLOCK_UNLOCK(LG::ProcessAnyTime::Instance()->m_workQueueMutex);
	}
	return;
}


bool ProcessAnyTime::HasWorkItems(){
	// TODO:
	return false;
}

// Add the work itemt to the work list
void ProcessAnyTime::QueueWork(GenericPc* wi) {
	LGLOCK_LOCK(m_workQueueMutex);
	// Check to see if uniq is specified and remove any duplicates
	if (wi->uniq.length() != 0) {
		// There will be duplicate requests for things. If we already have a request, delete the old
		std::list<GenericPc*>::iterator li;
		for (li = m_work.begin(); li != m_work.end(); li++) {
			if (li._Ptr->_Myval->uniq.length() != 0) {
				if (wi->uniq == li._Ptr->_Myval->uniq) {
					m_work.erase(li,li);
					LG::IncStat(LG::StatProcessAnyTimeDiscardedDups);
				}
			}
		}
	}
	m_work.push_back(wi);
	m_modified = true;
	LGLOCK_UNLOCK(m_workQueueMutex);
	LGLOCK_NOTIFY_ONE(m_workQueueMutex);
}

void ProcessAnyTime::ProcessWorkItems(int amountOfWorkToDo) {
	// This sort is intended to put the highest priority (ones with lowest numbers) at
	//   the front of the list for processing first.
	// TODO: figure out why uncommenting this line causes exceptions
	int loopCost = amountOfWorkToDo;
	while (!m_work.empty() && (m_work.size() > 2000 || (loopCost > 0) ) ) {
		LGLOCK_LOCK(m_workQueueMutex);
		GenericPc* workGeneric = (GenericPc*)m_work.front();
		m_work.pop_front();
		LGLOCK_UNLOCK(m_workQueueMutex);
		LG::SetStat(LG::StatProcessAnyTimeWorkItems, m_work.size());
		LG::IncStat(LG::StatProcessAnyTimeTotalProcessed);
		workGeneric->Process();
		loopCost -= workGeneric->cost;
		delete(workGeneric);
	}
	return;
}

}



