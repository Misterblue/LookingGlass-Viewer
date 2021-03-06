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

/*
NOTE TO THE NEXT PERSON: CODE NOT COMPLETE OR HOOKED INTO MAIN CODE
This code is started but not complete. The idea is to create a routine that
tracks meshes and their state (loaded, unloaded, ...) with the goal of allowing
the actual file access part of a mesh load (the call to mesh->prepare()) be
done outside the frame rendering thread.
*/
#include "LGOCommon.h"
#include "LGLocking.h"
#include "SingletonInstance.h"

namespace LG {

// the generic base class that goes in the list
class GenericPc {
public:
public:
	float priority;
	int cost;
	Ogre::String uniq;
	virtual void Process() {};
	virtual void RecalculatePriority() {};
	GenericPc() {
		priority = 100;
		cost = 50;
		uniq.clear();
	};
	~GenericPc() {};
};

class ProcessAnyTime : public SingletonInstance {

public:
	ProcessAnyTime();
	~ProcessAnyTime();

	static ProcessAnyTime* Instance() { 
		if (LG::ProcessAnyTime::m_instance == NULL) {
			LG::ProcessAnyTime::m_instance = new ProcessAnyTime();
		}
		return LG::ProcessAnyTime::m_instance; 
	}

	// SingletonInstance.Shutdown();
	void Shutdown();

	bool HasWorkItems();
	void RemoveWorkItem(Ogre::String);

	LGLOCK_MUTEX m_workQueueMutex;
	bool m_keepProcessing;

private:
	static ProcessAnyTime* m_instance;

	LGLOCK_THREAD m_processingThread;
	LGLOCK_THREAD m_backgroundThread;
	std::list<GenericPc*> m_work;

	bool m_modified;		// true if it's time to sort the work queue

	void ProcessWorkItems(int);

	// forward references
	static void ProcessThreadRoutine();
	static void ProcessBackgroundLoading();
	void QueueWork(GenericPc*);
};

}
