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
#include "SingletonInstance.h"

namespace LG {

class RendererOgre;	// forward definition

// the generic base class that goes in the list
class GenericQc {
public:
	float priority;
	int cost;
	Ogre::String type;
	Ogre::String uniq;
	virtual void Process() {};
	virtual void RecalculatePriority() {};
	GenericQc() {
		priority = 100;
		cost = 50;
		uniq.clear();
	};
	~GenericQc() {};
};

class ProcessBetweenFrame : public Ogre::FrameListener, public SingletonInstance {

public:
	ProcessBetweenFrame();
	~ProcessBetweenFrame();

	static ProcessBetweenFrame* Instance() { 
		if (LG::ProcessBetweenFrame::m_instance == NULL) {
			LG::ProcessBetweenFrame::m_instance = new ProcessBetweenFrame();
		}
		return LG::ProcessBetweenFrame::m_instance; 
	}

	// SingletonInstance.Shutdown();
	void Shutdown();

	bool HasWorkItems();
	void ProcessWorkItems(int);

	// Ogre::FrameListener
	bool frameEnded(const Ogre::FrameEvent&);

	void RefreshResource(float, char*, int);
	void CreateMaterialResource2(float, const char*, const char*, const float*);
	void CreateMaterialResource7(float, const char*, 
			const char* matName1, const char* matName2, const char* matName3, 
			const char* matName4, const char* matName5, const char* matName6, 
			const char* matName7,
			char* textureName1, char* textureName2, char* textureName3, 
			char* textureName4, char* textureName5, char* textureName6, 
			char* textureName7,
			const float* parms);
	void CreateMeshResource(float, const char*, const char*, const int*, const float*);
	void CreateMeshSceneNode(float,  Ogre::SceneManager* sceneMgr, 
					char* sceneNodeName, 
					Ogre::SceneNode* parentNode,
					char* entityName,
					char* meshName,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz);
	void UpdateSceneNode(float, char* nodeName,
					bool setPosition, float px, float py, float pz,
					bool setScale, float sx, float sy, float sz,
					bool setRotation, float ow, float ox, float oy, float oz);
	void AddRegion(float, const char*, const double, const double, const double, 
					const float, const float, const float);
	void UpdateTerrain(float, const char*, const int, const int, const float*);

	LGLOCK_MUTEX m_workItemMutex;
	static bool m_keepProcessing;	// true if to keep processing on and on

private:
	static ProcessBetweenFrame* m_instance;

	int m_numWorkItemsToDoBetweenFrames;
	std::list<GenericQc*> m_betweenFrameWork;

	LGLOCK_THREAD m_processingThread;

	// Forward definition
	void QueueWork(GenericQc*);
	static void ProcessThreadRoutine();

	bool m_modified;		// true if it's time to sort the work queue
};

}
