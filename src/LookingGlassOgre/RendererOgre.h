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
#include "LGCamera.h"
#include "UserIO.h"
#include "OLMaterialTracker.h"
#include "OLMeshTracker.h"
#include "SkyBoxBase.h"
#include "ShadowBase.h"
#include "VisCalcBase.h"

namespace LG {

class RendererOgre : Ogre::FrameListener {

public:
	RendererOgre(void);
	~RendererOgre(void);

	static RendererOgre* Instance() { 
		if (LG::RendererOgre::m_instance == NULL) {
			LG::RendererOgre::m_instance = new RendererOgre();
		}
		return LG::RendererOgre::m_instance; 
	}

	void initialize();
	bool renderingThread();
	bool renderOneFrame(bool, int);

	// Ogre::FrameListener
	bool frameStarted(const Ogre::FrameEvent&);
	bool frameRenderingQueued(const Ogre::FrameEvent&);
	bool frameEnded(const Ogre::FrameEvent&);

	// OGRE DATA FOR THE RENDERING
	Ogre::Root* m_root;				// the root of all Ogre's stuff
	Ogre::RenderWindow* m_window;	// the window we're rendering in
	Ogre::SceneManager* m_sceneMgr;	// the overall scene manager
	LG::LGCamera* m_camera;			// handle to the camera
	Ogre::Viewport* m_viewport;		// viewport the camera is using

	// update objects anvironment routines
	void updateCamera(float, float, float, float, float, float, float, float, float, float);
	void AdvanceCamera(const Ogre::FrameEvent&);

	void AddEntity(Ogre::SceneManager*, Ogre::SceneNode*, const char*, const char*);
	Ogre::SceneNode* RendererOgre::CreateSceneNode(const char* nodeName,
					Ogre::SceneNode* parentNode, bool inheritScale, bool inheritOrientation,
					float px, float py, float pz, float sx, float sy, float sz,
					float ow, float ox, float oy, float oz);
	Ogre::SceneNode* RendererOgre::CreateSceneNode(Ogre::SceneManager* sceneMgr, const char* nodeName,
					Ogre::SceneNode* parentNode, bool inheritScale, bool inheritOrientation,
					float px, float py, float pz, float sx, float sy, float sz,
					float ow, float ox, float oy, float oz);
	void UpdateSceneNode(const char* entName,
					bool updatePosition, float px, float py, float pz, 
					bool updateScale, float sx, float sy, float sz,
					bool updateRotation, float ow, float ox, float oy, float oz);
	void CreateMeshResource(const char*, const int[], const float[]);
	void CreateMeshResource2(const char*, const int[], const float[]);	// experimental
	
	// Resource groups
	Ogre::String EntityNameToFilename(const Ogre::String, const Ogre::String);
	void CreateParentDirectory(const Ogre::String);
	void MakeParentDir(const Ogre::String);

	// mutex  that is locked when the scene graph is in use
	LGLOCK_MUTEX SceneGraphLock() { return m_sceneGraphLock; }

	// Utility functions
	void Log(const char*, ...);
	const char* GetParameter(const char*);
	char* formatIt(const char*, ...);
	void formatIt(Ogre::String&, const char*, ...);
	const bool checkKeepRunning();

	Ogre::ColourValue SceneAmbientColor;
	Ogre::ColourValue MaterialAmbientColor;
	LG::SkyBoxBase* m_sky;
	LG::VisCalcBase* m_visCalc;	// an routine for calculating visibility
	LG::ShadowBase* Shadow;

private:
	static RendererOgre* m_instance;

	// OGRE INITIALIZATION ROUTINES
    void loadOgreResources(const char*);
    void configureOgreRenderSystem();
    void createLookingGlassResourceGroups();
    void initOgreResources();
    void createScene();
    void createSky();
	void createVisibilityProcessor();
    void createCamera();
    void createLight();
    // void createDefaultTerrain();
    void createViewport();
    void createFrameListener();
    void createInput();
	void destroyScene();

	// UTILITY ROUTINES
	void AssertNonNull(void*, const char*);
	void GenerateLoadingMesh();

	// USER IO
	UserIO* m_userio;

	// Lock for the scene graph. Locked when doing RenderOneFrame.
	LGLOCK_MUTEX m_sceneGraphLock;

	Ogre::String m_cacheDir; 
	Ogre::String m_preloadedDir; 
	bool m_serializeMeshes;

	Ogre::Quaternion m_desiredCameraOrientation;
	float m_desiredCameraOrientationProgress;

};
}
