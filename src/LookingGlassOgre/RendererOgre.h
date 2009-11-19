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
#include "UserIO.h"
#include "OLMaterialTracker.h"
#include "OLMeshTracker.h"
#include "ProcessBetweenFrame.h"
#include "SkyBoxBase.h"
#include "VisCalcBase.h"

namespace RendererOgre {

class RendererOgre : Ogre::FrameListener {

public:
	RendererOgre(void);
	~RendererOgre(void);

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
	Ogre::Camera* m_camera;			// handle to the camera
	Ogre::Viewport* m_viewport;		// viewport the camera is using

	// update objects anvironment routines
	void updateCamera(float, float, float, float, float, float, float, float, float, float);
	void AdvanceCamera(const Ogre::FrameEvent&);

	void AddEntity(Ogre::SceneManager*, Ogre::SceneNode*, const char*, const char*);
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
	void GenTerrainMesh(Ogre::SceneManager*, Ogre::SceneNode*, const int, const int, const float*);
	void AddOceanToRegion(Ogre::SceneManager* , Ogre::SceneNode* , const float, const float, const float, const char*);
	
	// Resource groups
	Ogre::String EntityNameToFilename(const Ogre::String, const Ogre::String);
	void CreateParentDirectory(const Ogre::String);
	void MakeParentDir(const Ogre::String);

	// when a material resource is changed, tell Ogre to reload the things that use it
	OLMaterialTracker::OLMaterialTracker* MaterialTracker() { return m_materialTracker; }
	ProcessBetweenFrame::ProcessBetweenFrame* ProcessBetweenFrame() { return m_processBetweenFrame; }
	OLMeshTracker::OLMeshTracker* MeshTracker() { return m_meshTracker; }

	// Utility functions
	void Log(const char*, ...);
	const char* GetParameter(const char*);
	char* formatIt(const char*, ...);
	void formatIt(Ogre::String&, const char*, ...);
	const bool checkKeepRunning();

	Ogre::ColourValue SceneAmbientColor;
	Ogre::ColourValue MaterialAmbientColor;
	LGSky::SkyBoxBase* m_sky;
	VisCalc::VisCalcBase* m_visCalc;	// an routine for calculating visibility

private:
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
	OLMaterialTracker::OLMaterialTracker* m_materialTracker;
	ProcessBetweenFrame::ProcessBetweenFrame* m_processBetweenFrame;
	OLMeshTracker::OLMeshTracker* m_meshTracker;

	Ogre::String m_cacheDir; 
	Ogre::String m_preloadedDir; 
	Ogre::String m_defaultTerrainMaterial; 
	bool m_serializeMeshes;

	Ogre::Quaternion m_desiredCameraOrientation;
	float m_desiredCameraOrientationProgress;

};
}
