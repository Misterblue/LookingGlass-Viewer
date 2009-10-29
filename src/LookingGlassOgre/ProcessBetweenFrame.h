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
#include "LGLocking.h"

// forward definition
namespace RendererOgre { class RendererOgre; }

namespace ProcessBetweenFrame {

// the generic base class that goes in the list
class GenericQc {
public:
	int priority;
	int cost;
	Ogre::String uniq;
	virtual void Process() {};
	GenericQc() {
		priority = 100;
		cost = 50;
		uniq.clear();
	};
	~GenericQc() {};
};

class ProcessBetweenFrame : public Ogre::FrameListener {

public:
	ProcessBetweenFrame(RendererOgre::RendererOgre*, int workItems);
	~ProcessBetweenFrame();

	bool HasWorkItems();
	void ProcessWorkItems(int);

	// Ogre::FrameListener
	bool frameEnded(const Ogre::FrameEvent&);

	void RefreshResource(int, char*, int);
	void CreateMaterialResource2(int, const char*, const char*, const float*);
	void CreateMeshResource(int, const char*, const int*, const float*);
	void CreateMeshSceneNode(int,  Ogre::SceneManager* sceneMgr, 
					char* sceneNodeName, 
					Ogre::SceneNode* parentNode,
					char* entityName,
					char* meshName,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz);
	void UpdateSceneNode(int, char* nodeName,
					bool setPosition, float px, float py, float pz,
					bool setScale, float sx, float sy, float sz,
					bool setRotation, float ow, float ox, float oy, float oz);


private:
	int m_numWorkItemsToDoBetweenFrames;
	LGLOCK_MUTEX m_workItemMutex;

	bool m_modified;		// true if it's time to sort the work queue

	// Forward definition
	void QueueWork(GenericQc*);
};

}
