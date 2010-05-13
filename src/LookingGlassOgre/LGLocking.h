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

// #define LGLOCK_PTHREADS
#define LGLOCK_BOOST

#include "LGOCommon.h"
#ifdef LGLOCK_PTHREADS
#include "pthreads.h"
#endif
#ifdef LGLOCK_BOOST
#undef NOMINMAX
#define NOMINMAX
#include "boost/thread/mutex.hpp"
#include "boost/thread/condition.hpp"
#endif

namespace LG {

// Lock wrapper. The usage pattern is:
// ...
// LGLOCK_MUTEX* m_myLock;
// ...
// m_myLock = LGLOCK_ALLOCATION_MUTEX("ProtextXQueue");
// ...
// LGLOCK_LOCK(m_myLock);
// ... critical section
// LGLOCK_UNLOCK(m_myLock);
// ...
// LGLOCK_RELEASE_MUTEX(m_myLock);
// 
// Underlying this wrapper is whatever works on this system
class LGLock {
public:
	LGLock();
	LGLock(Ogre::String nam);
	~LGLock();

	Ogre::String Name;

	void Lock();
	void Unlock();
	void Wait();
	void NotifyOne();
	void NotifyAll();

	static LGLock* LGLock_Allocate_Mutex(Ogre::String);
	static void LGLock_Release_Lock(LGLock*);
	static void LGLock_Sleep(int);

#ifdef LGLOCK_PTHREADS
#endif
#ifdef LGLOCK_BOOST
	// public so we can reference it in Wait()
	boost::mutex* m_mutex;
	boost::condition* m_condition;
#endif
private:
};

extern int LGLockingThreadInitializeCount;

}

// USE THESE DEFINES IN YOUR CODE
// This will allow the underlying implementation to be changed easily
#define LGLOCK_ALLOCATE_MUTEX(name) LG::LGLock::LGLock_Allocate_Mutex(name)
#define LGLOCK_RELEASE_MUTEX(mutex) LG::LGLock::LGLock_Release_Lock(mutex)

// LOCK and UNLOCK OPERATIONS
#define LGLOCK_MUTEX LG::LGLock*
#define LGLOCK_LOCK(mutex) (mutex)->Lock()
#define LGLOCK_UNLOCK(mutex) (mutex)->Unlock()

// WAIT and NOTIFY
#ifdef LGLOCK_BOOST
#define LGLOCK_WAIT(mutex) (mutex)->m_condition->wait(*((mutex)->m_mutex));
#define LGLOCK_NOTIFY_ONE(mutex) (mutex)->m_condition->notify_one();
#define LGLOCK_NOTIFY_ALL(mutex) (mutex)->m_condition->notify_all();
#define LGLOCK_SLEEP(ms) LG::LGLock::LGLock_Sleep(ms);
#else
#define LGLOCK_WAIT(mutex) (mutex)->Wait((mutex)->m_mutex);
#define LGLOCK_NOTIFY_ONE(mutex) (mutex)->NotifyOne();
#define LGLOCK_NOTIFY_ALL(mutex) (mutex)->NotifyAll();
#define LGLOCK_SLEEP 
#endif

// CREATE and RELEASE THREADS
#define LGLOCK_THREAD boost::thread
#define LGLOCK_ALLOCATE_THREAD(func) boost::thread(func);
#define LGLOCK_RELEASE_THREAD(thread) ;

// Kludge to keep track of the number of threads being intialized and to
//   know when they are all initialized
#define LGLOCK_THREAD_INITIALIZING (LG::LGLockingThreadInitializeCount++)
#define LGLOCK_THREAD_INITIALIZED (LG::LGLockingThreadInitializeCount--)
#define LGLOCK_THREADS_AREINITIALIZED (LG::LGLockingThreadInitializeCount == 0)


// A wrapper class for a lock that is a local variable so it will get unlocked when
// destructed.
// Use:
//  LGLOCK_MUTEX myMutex = LGLOCK_ALLOCATE_MUTEX("myMutex");
//	...
// void myMethod() {
//  LGLOCK_ALOCK myLock;
//  ...
//  myLock.Lock(myMutex);
//  ...
//  myLock.Unlock();
//  ...
//  }
// If 'myMethod' gets existed without unlocking, the destruction of the local ALOCK
// variable will unlock the mutex.
class LGLOCK_ALOCK {
public:
	LGLOCK_ALOCK() { m_mutex = NULL; };
	~LGLOCK_ALOCK() {
		if (m_isLocked && m_mutex != NULL) {
			LGLOCK_UNLOCK(m_mutex);
		}
	}
	void Mutex(LGLOCK_MUTEX mtx) { m_mutex = mtx; }
	void Lock() {
		LGLOCK_LOCK(m_mutex);
		m_isLocked = true;
	}
	void Lock(LGLOCK_MUTEX mtx) { // combine Mutex and Lock methods
		m_mutex = mtx;
		LGLOCK_LOCK(m_mutex);
		m_isLocked = true;
	}
	void Unlock() {
		if (m_isLocked) LGLOCK_UNLOCK(m_mutex);
		m_isLocked = false;
	}
private:
	LGLOCK_MUTEX m_mutex;
	bool m_isLocked;
};