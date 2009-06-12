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

// uncomment to use Caelum as the sky
// #define CAELUM

#include "LGOCommon.h"
#include "UserIO.h"
#include "OLMaterialTracker.h"
#ifdef CAELUM
#include "Caelum.h"
#endif

namespace RendererOgre {

class RendererOgre : Ogre::FrameListener {

public:
	RendererOgre(void);
	~RendererOgre(void);

	void initialize();
	bool renderingThread();

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

	// Diddle with the scene graph
	Ogre::SceneNode* getRootNode();

	// update objects anvironment routines
	void updateCamera(float, float, float, float, float, float, float, float, float, float);

	void AddEntity(Ogre::SceneManager*, Ogre::SceneNode*, char*, char*);
	void CreateMeshResource(const char*, const int[], const float[]);
	void CreateMeshResource2(const char*, const int[], const float[]);	// experimental
	void GenTerrainMesh(Ogre::SceneManager*, Ogre::SceneNode*, const int, const int, const float*);
	void AddOceanToRegion(Ogre::SceneManager* , Ogre::SceneNode* , const float, const float, const float, const char*);
	
	// Resource groups
	Ogre::String EntityNameToFilename(const Ogre::String, const Ogre::String);
	void CreateParentDirectory(const Ogre::String);
	void meshToResource(Ogre::MeshPtr, const Ogre::String);
	void MakeParentDir(const Ogre::String);

	// when a material resource is changed, tell Ogre to reload the things that use it
	OLMaterialTracker::OLMaterialTracker* MaterialTracker() { return m_materialTracker; }

	// Utility functions
	void Log(const char*, ...);
	const char* GetParameter(const char*);
	char* formatIt(const char*, ...);
	void formatIt(Ogre::String&, const char*, ...);
	const bool checkKeepRunning();

private:
	// OGRE INITIALIZATION ROUTINES
    void loadOgreResources(const char*);
    void configureOgreRenderSystem();
    void createLookingGlassResourceGroups();
    void initOgreResources();
    void createScene();
    void createSky();
    void createCamera();
    void createLight();
    // void createDefaultTerrain();
    void createViewport();
    void createFrameListener();
    void createInput();
	void destroyScene();

	// UTILITY ROUTINES
	void AssertNonNull(void*, const char*);

	// USER IO
	UserIO* m_userio;
	OLMaterialTracker::OLMaterialTracker* m_materialTracker;

#ifdef CAELUM
	Caelum::CaelumSystem* m_caelumSystem;
#else
	Ogre::Light* m_sun;				// the light that is the sun
	Ogre::Light* m_moon;			// the light that is the moon
#endif // CAELUM

	Ogre::String m_cacheDir; 
	Ogre::String m_preloadedDir; 
	Ogre::String m_defaultTerrainMaterial; 
	bool m_serializeMeshes;
	Ogre::MeshSerializer* m_meshSerializer;
};
}
