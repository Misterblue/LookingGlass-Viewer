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

// #define LGLOCK_SPINLOCK
// #define LGLOCK_MSGPUMP
// #define LGLOCK_PTHREADS
#define LGLOCK_BOOST

#include "LGOCommon.h"
#ifdef LGLOCK_PTHREADS
#include "pthreads.h"
#endif
#ifdef LGLOCK_BOOST
#include "boost/thread/mutex.hpp"
#endif

namespace LGLocking {

class LGLock {
public:
	LGLock();
	LGLock(Ogre::String nam);
	~LGLock();

	Ogre::String Name;

	void Lock();
	void Unlock();
private:
#ifdef LGLOCK_SPINLOCK
	int flag;
	int x;
#endif
#ifdef LGLOCK_MSGPUMP
	int flag;
#endif
#ifdef LGLOCK_PTHREADS
#endif
#ifdef LGLOCK_BOOST
	boost::mutex* m_mutex;
#endif
};

extern LGLock* LGLock_Allocate_Mutex(Ogre::String name);
extern void LGLock_Release_Lock(LGLock* lock);

// USE THESE DEFINES IN YOUR CODE
// This will allow the underlying implementation to be changed easily
#define LGLOCK_ALLOCATE_MUTEX(name) LGLocking::LGLock_Allocate_Mutex(name)
#define LGLOCK_RELEASE_MUTEX(mutex) LGLocking::LGLock_Release_Lock(mutex)

#define LGLOCK_MUTEX LGLocking::LGLock*
#define LGLOCK_LOCK(mutex) (mutex)->Lock()
#define LGLOCK_UNLOCK(mutex) (mutex)->Unlock()

}