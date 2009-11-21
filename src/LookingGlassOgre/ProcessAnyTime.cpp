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
	m_workItemMutex = LGLOCK_ALLOCATE_MUTEX("ProcessAnyTime");
	m_modified = false;
}

ProcessAnyTime::~ProcessAnyTime() {
	LGLOCK_RELEASE_MUTEX(m_workItemMutex);
}

bool ProcessAnyTime::HasWorkItems(){
	// TODO:
	return false;
}

void ProcessAnyTime::ProcessWorkItems(int amountOfWorkToDo) {
	// TODO:
}

}



