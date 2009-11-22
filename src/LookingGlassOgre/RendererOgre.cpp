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
#include "StdAfx.h"
#include <direct.h>
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>
#include <sys/stat.h>
#include "RendererOgre.h"
#include "LookingGlassOgre.h"
#include "OLArchive.h"
#include "OLPreloadArchive.h"
#include "ResourceListeners.h"
#include "ProcessBetweenFrame.h"
#include "ProcessAnyTime.h"
#include "SkyBoxSimple.h"
#include "SkyBoxSkyX.h"
#include "VisCalcNull.h"
#include "VisCalcFrustDist.h"
#include "VisCalcVariable.h"

namespace LG {

	RendererOgre* RendererOgre::m_instance = NULL;

	RendererOgre::RendererOgre() {
	}

	RendererOgre::~RendererOgre() {
		// TODO: there is a lot of rendering to turn off.
		if (m_sky != NULL) {
			m_sky->Stop();
			m_sky = NULL;
		}
		if (m_visCalc != NULL) {
			m_visCalc->Stop();
			m_visCalc = NULL;
		}
	}

	// The main program calls in here with the main window thread. This is required
	// to keep the windows message pump going and so OpenGL is happy with 
	// its creation happening on the same thread as rendering.
	// The frame rate is capped and sleeps are inserted to return control to
	// the windowing system when the max frame rate is reached.
	// If we don't want the thread, return false.
	Ogre::Timer* timeKeeper = new Ogre::Timer();
	bool RendererOgre::renderingThread() {
		LG::Log("RendererOgre::renderingThread: LookingGlassOrge: Starting rendering");
		// m_root->startRendering();

		// new way that tried to control the amount of work between frames to keep
		//   frame rate up
		int maxFPS = LG::GetParameterInt("Renderer.Ogre.FramePerSecMax");
		if (maxFPS < 2 || maxFPS > 100) maxFPS = 20;
		int msPerFrame = 1000 / maxFPS;

		unsigned long now = timeKeeper->getMilliseconds();
		unsigned long timeStartedLastFrame = timeKeeper->getMilliseconds();

		while (m_root->renderOneFrame()) {
			Ogre::WindowEventUtilities::messagePump();
			now = timeKeeper->getMilliseconds();
			/*
			int remaining = msPerFrame - ((int)(now - timeStartedLastFrame));
			while (remaining > 10) {
				if (LG::ProcessBetweenFrame::Instance()->HasWorkItems()) {
					LG::ProcessBetweenFrame::Instance()->ProcessWorkItems(3);
				}
				else {
					Sleep(remaining);
				}
				now = timeKeeper->getMilliseconds();
				remaining = msPerFrame - ((int)(now - timeStartedLastFrame));
			}
			*/
			int totalMSForLastFrame = (int)(timeKeeper->getMilliseconds() - timeStartedLastFrame);
			if (totalMSForLastFrame < 0) totalMSForLastFrame = 1;
			LG::SetStat(LG::StatFramesPerSecond, 1000000/totalMSForLastFrame);
			timeStartedLastFrame = timeKeeper->getMilliseconds();
		}
		LG::Log("RendererOgre::renderingThread: Completed rendering");
		destroyScene();
		// for some reason, often after the exit, m_root is not usable
		// m_root->shutdown();
		m_root = NULL;
		return true;
	}

	// As an alternate to using the above rendering thread entry, the main
	// program can call this to render each frame.
	// Note that his also called the message pump to make screen resizing and
	// movement happen on the Ogre frame.
	// If the passed parameter is 'true' we call the windows message pump
	// and the number of ms this frame should take. We do between frame work
	// with any extra time.
	unsigned long m_lastFrameTime;
	bool RendererOgre::renderOneFrame(bool pump, int len) {
		bool ret = true;
		unsigned long now = timeKeeper->getMilliseconds();
		unsigned long timeStartedLastFrame = timeKeeper->getMilliseconds();
		if (m_root != NULL) {
			// LGLOCK_LOCK(m_sceneGraphLock);
			ret = m_root->renderOneFrame();
			// LGLOCK_UNLOCK(m_sceneGraphLock);
			// LGLOCK_NOTIFY_ALL(m_sceneGraphLock);
			if (pump) Ogre::WindowEventUtilities::messagePump();
		}

		/*
		// The amount of time a frame takes to render is passed to us
		// If we have time left over and there is between frame processing, do them
		int remaining = len - ((int)(now - timeStartedLastFrame));
		while (remaining > 10) {
			if (m_processBetweenFrame->HasWorkItems()) {
				m_processBetweenFrame->ProcessWorkItems(20);
			}
			else {
				break;
			}
			now = timeKeeper->getMilliseconds();
			remaining = len - ((int)(now - timeStartedLastFrame));
		}
		*/
		int totalMSForLastFrame = (int)(timeKeeper->getMilliseconds() - m_lastFrameTime);
		if (totalMSForLastFrame <= 0) totalMSForLastFrame = 1;
		LG::SetStat(LG::StatLastFrameMs, totalMSForLastFrame);
		LG::SetStat(LG::StatFramesPerSecond, 1000000/totalMSForLastFrame);
		m_lastFrameTime = timeKeeper->getMilliseconds();

		if (!ret) {
			// if renderOneFrame returns false, it means we're going down
		}

		return ret;
	}

	// Update the camera position given an location and a direction
	void RendererOgre::updateCamera(float px, float py, float pz, 
				float dw, float dx, float dy, float dz,
				float nearClip, float farClip, float aspect) {
		if (m_camera) {
			LG::Log("RendererOgre::UpdateCamera: pos=<%f, %f, %f>", (double)px, (double)py, (double)pz);
			m_camera->setPosition(px, py, pz);
			m_desiredCameraOrientation = Ogre::Quaternion(dw, dx, dy, dz);
			m_desiredCameraOrientationProgress = 0.0;
			// to do slerped movement, comment out this line and uncomment "XXXX" below
			// m_camera->setOrientation(Ogre::Quaternion(dw, dx, dy, dz));
			if (nearClip != m_camera->getNearClipDistance()) {
				m_camera->setNearClipDistance(nearClip);
			}
			/*	don't fool with far for the moment
			if (farClip != m_camera->getFarClipDistance()) {
				m_camera->setFarClipDistance(farClip);
			}
			*/
			m_visCalc->RecalculateVisibility();
		}
		return;
	}

	// called at the beginning of the frame so we can slrp the camera
#define SECONDS_TO_SLERP 0.2
	void RendererOgre::AdvanceCamera(const Ogre::FrameEvent& evt) {
		// Say time since last frame is .1s. That's 1/10 sec and if we're trying to
		//   to the smooth turn in 1/2 sec, this is 1/5 of our way there.
		float progress = evt.timeSinceLastEvent / SECONDS_TO_SLERP;
		m_desiredCameraOrientationProgress += progress;
		if (m_desiredCameraOrientationProgress < 1.0) {
			Ogre::Quaternion newOrientation = Ogre::Quaternion::Slerp(m_desiredCameraOrientationProgress, 
				m_camera->getOrientation(), m_desiredCameraOrientation, true);
			m_camera->setOrientation(newOrientation); // XXXX
			m_visCalc->RecalculateVisibility(); // XXXX
		}
	}

	// Called from managed code via InitializeOgre().
	// Do all the setup needed in the Ogre environment: all the basic entities
	// (camera, lights, ...), all the resource managers and the user input system.
	void RendererOgre::initialize() {
		LG::Log("RendererOgre::initialize: ");

		m_sceneGraphLock = LGLOCK_ALLOCATE_MUTEX("sceneGraph");

		m_cacheDir = LG::GetParameter("Renderer.Ogre.CacheDir");
		m_preloadedDir = LG::GetParameter("Renderer.Ogre.PreLoadedDir");

		m_defaultTerrainMaterial = LG::GetParameter("Renderer.Ogre.DefaultTerrainMaterial");
		m_serializeMeshes = LG::GetParameterBool("Renderer.Ogre.SerializeMeshes");

		m_root = new Ogre::Root(LG::GetParameter("Renderer.Ogre.PluginFilename"));
		LG::Log("RendererOgre::initialize: after new Ogre::Root()");
		// if detail logging is turned off, I don't want Ogre yakking up a storm either
		if (LG::debugLogCallback == NULL) {
			Ogre::LogManager::getSingleton().setLogDetail(Ogre::LL_LOW);
		}

		try {
			// load the resource info from the Ogre config files
			loadOgreResources(LG::GetParameter("Renderer.Ogre.ResourcesFilename"));
			// set up the render system (window, size, OS connection, ...)
	        configureOgreRenderSystem();

			// setup our special resource groups for meshes and materials
			createLookingGlassResourceGroups();
			// turn on the resource system
	        initOgreResources();
		}
		catch (char* str) {
			LG::Log("RendererOgre::initialize: LookingGlassOrge: exception initializing: {0}", str);
			return;
		}

		// create the viewer components
        createScene();
        createCamera();
        createViewport();
        createSky();
        createVisibilityProcessor();
        createFrameListener();
		if (LG::userIOCallback != NULL) {
	        createInput();
		}

		// force a first time visibility calculation
		m_visCalc->RecalculateVisibility();

		// uncomment this to generate the loading mesh shape (small cube)
		// GenerateLoadingMesh();
		return;
	}

	void RendererOgre::destroyScene() {
		// TODO: write something here
		return;
	}

	// Load all the resource locations from the resource configuration file
	void RendererOgre::loadOgreResources(const char* resourceFile) {
		LG::Log("RendererOgre::loadOgreResources: ");
		Ogre::String secName, typeName, archName;
		Ogre::ConfigFile cf;
		cf.load(resourceFile);
		Ogre::ConfigFile::SectionIterator seci = cf.getSectionIterator();
		while (seci.hasMoreElements()) {
			secName = seci.peekNextKey();
			Ogre::ConfigFile::SettingsMultiMap *settings = seci.getNext();
			Ogre::ConfigFile::SettingsMultiMap::iterator i;
			for (i = settings->begin(); i != settings->end(); ++i) {
				typeName = i->first;
				archName = i->second;
				Ogre::ResourceGroupManager::getSingleton().addResourceLocation(archName, typeName, secName);
			}
		}
	}

	// Create the resource group and group managers for the LookingGlass Ogre extensions
	void RendererOgre::createLookingGlassResourceGroups() {
		LG::Log("RendererOgre::createLookingGlassResourceGroups:");
		Ogre::ResourceGroupManager::getSingleton().createResourceGroup(OLResourceGroupName);

		// Force the creation of the singleton classes
		LG::ProcessBetweenFrame::Instance();
		LG::ProcessAnyTime::Instance();
		LG::OLMaterialTracker::Instance();
		LG::OLMeshTracker::Instance();

		// listener to catch references to materials in meshes when they are read in
		Ogre::MeshManager::getSingleton().setListener(new LG::OLMeshSerializerListener());
		// Ogre::ScriptCompilerManager::getSingleton().setListener(new OLScriptCompilerListener(this));

		// Create the archive system that will find the predefined meshes/textures
		Ogre::ArchiveManager::getSingleton().addArchiveFactory(new LG::OLPreloadArchiveFactory() );
		LG::Log("RendererOgre::createLookingGlassResourceGroups: addResourceLocation %s", m_preloadedDir.c_str());
		Ogre::ResourceGroupManager::getSingleton().addResourceLocation(m_preloadedDir,
						OLPreloadTypeName, OLResourceGroupName, true);

		// Create the archive system that will find our meshes
		Ogre::ArchiveManager::getSingleton().addArchiveFactory(new LG::OLArchiveFactory() );
		LG::Log("RendererOgre::createLookingGlassResourceGroups: addResourceLocation %s", m_cacheDir.c_str());
		Ogre::ResourceGroupManager::getSingleton().addResourceLocation(m_cacheDir,
						OLArchiveTypeName, OLResourceGroupName, true);
		return;
	}

	void RendererOgre::configureOgreRenderSystem() {
		LG::Log("RendererOgre::configureOgreRenderSystem:");
		Ogre::String rsystem = LG::GetParameter("Renderer.Ogre.Renderer");
		Ogre::RenderSystem* rs = m_root->getRenderSystemByName(rsystem);
		if (rs == NULL) {
			LG::Log("RendererOgre::configureOgreRenderingSystem: CANNOT INITIALIZE RENDERING SYSTEM '%s'", rsystem);
			return;
		}
		m_root->setRenderSystem(rs);
        rs->setConfigOption("Full Screen", "No");
		rs->setConfigOption("Video Mode", LG::GetParameter("Renderer.Ogre.VideoMode"));

		// I am running the background thread
		Ogre::ResourceBackgroundQueue::getSingleton().setStartBackgroundThread(false);

		// Two types of initialization here. Get own window or use a passed window
		Ogre::String windowHandle = LG::GetParameter("Renderer.Ogre.ExternalWindow.Handle");
		if (windowHandle.length() == 0) {
			m_window = m_root->initialise(true, LG::GetParameter("Renderer.Ogre.Name"));
		}
		else {
			m_window = m_root->initialise(false);
			Ogre::NameValuePairList createParams;
			createParams["externalWindowHandle"] = windowHandle;
			createParams["title"] = LG::GetParameter("Renderer.Ogre.Name");
			// createParams["left"] = something;
			// createParams["right"] = something;
			// createParams["depthBuffer"] = something;
			// createParams["parentWindowHandle"] = something;
			m_window = m_root->createRenderWindow("MAINWINDOW", 
				LG::GetParameterInt("Renderer.Ogre.ExternalWindow.Width"),
				LG::GetParameterInt("Renderer.Ogre.ExternalWindow.Height"),
				false, &createParams);

			Ogre::ResourceBackgroundQueue::getSingleton().initialise();
		}
	}

	void RendererOgre::initOgreResources() {
		LG::Log("RendererOgre::initOgreResources");
		Ogre::TextureManager::getSingleton().setDefaultNumMipmaps(
			LG::GetParameterInt("Renderer.Ogre.DefaultNumMipmaps"));
		Ogre::ResourceGroupManager::getSingleton().initialiseAllResourceGroups();
	}

	void RendererOgre::createScene() {
		LG::Log("RendererOgre::createScene");
		try {
			const char* sceneName = LG::GetParameter("Renderer.Ogre.Name");
			m_sceneMgr = m_root->createSceneManager(Ogre::ST_EXTERIOR_CLOSE, sceneName);
			// m_sceneMgr = m_root->createSceneManager(Ogre::ST_GENERIC, sceneName);
			// ambient has to be adjusted for time of day. Set it initially
			// m_sceneMgr->setAmbientLight(Ogre::ColourValue(0.5, 0.5, 0.5));
			SceneAmbientColor = LG::GetParameterColor("Renderer.Ogre.Ambient.Scene");
			MaterialAmbientColor = LG::GetParameterColor("Renderer.Ogre.Ambient.Material");
			m_sceneMgr->setAmbientLight(SceneAmbientColor);
			const char* shadowName = LG::GetParameter("Renderer.Ogre.ShadowTechnique");
			if (stricmp(shadowName, "texture-modulative") == 0) {
				m_sceneMgr->setShadowTechnique(Ogre::SHADOWTYPE_TEXTURE_MODULATIVE);	// hardest
				LG::Log("RendererOgre::createScene: setting shadow to 'texture-modulative'");
			}
			if (stricmp(shadowName, "stencil-modulative") == 0) {
				m_sceneMgr->setShadowTechnique(Ogre::SHADOWTYPE_STENCIL_MODULATIVE);
				LG::Log("RendererOgre::createScene: setting shadow to 'stencil-modulative'");
			}
			if (stricmp(shadowName, "texture-additive") == 0) {
				m_sceneMgr->setShadowTechnique(Ogre::SHADOWTYPE_TEXTURE_ADDITIVE);	// easiest
				LG::Log("RendererOgre::createScene: setting shadow to 'texture-additive'");
			}
			if (stricmp(shadowName, "stencil-additive") == 0) {
				m_sceneMgr->setShadowTechnique(Ogre::SHADOWTYPE_STENCIL_ADDITIVE);
				LG::Log("RendererOgre::createScene: setting shadow to 'stencil-additive'");
			}
			int shadowFarDistance = LG::GetParameterInt("Renderer.Ogre.ShadowFarDistance");
			m_sceneMgr->setShadowFarDistance((float)shadowFarDistance);
			m_sceneMgr->setShadowColour(Ogre::ColourValue(0.2, 0.2, 0.2));
		}
		catch (std::exception e) {
			LG::Log("RendererOgre::createScene: Exception %s", e.what());
			return;
		}
	}

	void RendererOgre::createCamera() {
		LG::Log("RendererOgre::createCamera");
		m_camera = m_sceneMgr->createCamera("MainCamera");
		m_camera->setPosition(0.0, 0.0, 0.0);
		m_camera->setDirection(0.0, 0.0, -1.0);
		m_camera->setNearClipDistance(2.0);
		m_camera->setFarClipDistance(10000.0);
		// m_camera->setFarClipDistance(0.0);
		m_camera->setAutoAspectRatio(true);
		if (m_camera == NULL) {
			LG::Log("RendererOgre::createCamera: CAMERA FAILED TO CREATE");
		}
	}

	void RendererOgre::createViewport() {
		LG::Log("RendererOgre::createViewport");
		m_viewport = m_window->addViewport(m_camera);
		m_viewport->setBackgroundColour(Ogre::ColourValue(0.0f, 0.0f, 0.25f));
		m_camera->setAspectRatio((float)m_viewport->getActualWidth() / (float)m_viewport->getActualHeight());
	}

	void RendererOgre::createSky() {
		LG::Log("RendererOgre::createsky");
		const char* skyName = LG::GetParameter("Renderer.Ogre.Sky");
		if (stricmp(skyName, "SkyX") == 0) {
			LG::Log("RendererOgre::createSky: using SkyBoxSkyX");
			m_sky = new LG::SkyBoxSkyX();
		}
		else {
			LG::Log("RendererOgre::createSky: using SkyBoxSimple");
			m_sky = new LG::SkyBoxSimple();
		}
		m_sky->Initialize();
		m_sky->Start();
	}

	void RendererOgre::createVisibilityProcessor() {
		LG::Log("RendererOgre::createVisibilityProcessor");
		const char* visName = LG::GetParameter("Renderer.Ogre.Visibility.Processor");
		if (stricmp(visName, "FrustrumDistance") == 0) {
			LG::Log("RendererOgre::createVisibilityProcessor: using VisCalcFrustDist");
			m_visCalc = new LG::VisCalcFrustDist();
		}
		else if (stricmp(visName, "VariableFrustDist") == 0) {
				LG::Log("RendererOgre::createVisibilityProcessor: using VisCalcVariable");
				m_visCalc = new LG::VisCalcVariable();
		}
		else {
			LG::Log("RendererOgre::creteVisibilityProcessor: using VisCalcNull");
			m_visCalc = new LG::VisCalcNull();
		}
		m_visCalc->Initialize();
		m_visCalc->Start();
	}

	void RendererOgre::createFrameListener() {
		LG::Log("RendererOgre::createFrameListener");
		// this creates two pointers to our base object. 
		// Might need to manage if we ever get dynamic.
		m_root->addFrameListener(this);
	}

	void RendererOgre::createInput() {
		m_userio = new UserIO();
	}

	// ========== Ogre::FrameListener
	bool RendererOgre::frameStarted(const Ogre::FrameEvent& evt) {
		AdvanceCamera(evt);
		return true;
	}

	bool RendererOgre::frameRenderingQueued(const Ogre::FrameEvent& evt) {
		return true;
	}

	int betweenFrameCounter = 0;
	bool RendererOgre::frameEnded(const Ogre::FrameEvent& evt) {
		if (m_window->isClosed()) return false;	// if you close the window we leave
		LG::IncStat(LG::StatTotalFrames);
		betweenFrameCounter++;
		if (LG::betweenFramesCallback != NULL) {
			// the C# code uses this for terrain and regions so don't do it often
			if ((betweenFrameCounter % 10) == 0) {
				return (*LG::betweenFramesCallback)();
			}
		}
		return true;
	}

	// ========== end of Ogre::FrameListener

	// ============= REQUESTS TO DO WORK
	// BETWEEN FRAME OPERATION
	void RendererOgre::AddEntity(Ogre::SceneManager* sceneMgr, Ogre::SceneNode* sceneNode,
							const char* entName, const char* meshName) {
		// LG::Log("RendererOgre::AddEntity: declare %s, t=%s, g=%s", meshName,
		// 						"Mesh", meshResourceGroupName);
		Ogre::ResourceGroupManager::getSingleton().declareResource(meshName,
								"Mesh", OLResourceGroupName
								);
		try {
			Ogre::MovableObject* ent = sceneMgr->createEntity(entName, meshName);
			// it's not scenery
			ent->removeQueryFlags(Ogre::SceneManager::WORLD_GEOMETRY_TYPE_MASK);	
			// Ogre::MovableObject* ent = sceneMgr->createEntity(entName, Ogre::SceneManager::PT_SPHERE);	// DEBUG
			// this should somehow be settable
			ent->setCastShadows(true);
			sceneNode->attachObject(ent);
			m_visCalc->RecalculateVisibility();
		}
		catch (Ogre::Exception e) {
			// we presume this is because the entity already exists
		}
		return;
	}

	// BETWEEN FRAME OPERATION
	Ogre::SceneNode* RendererOgre::CreateSceneNode( Ogre::SceneManager* sceneMgr, const char* nodeName,
					Ogre::SceneNode* parentNode,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz) {
		Ogre::SceneNode* node = NULL;
		Ogre::Quaternion rot = Ogre::Quaternion(ow, ox, oy, oz);

		try {
			if (parentNode == 0) {
				node = sceneMgr->getRootSceneNode()->createChildSceneNode(nodeName);
			}
			else {
				node = parentNode->createChildSceneNode(nodeName);
			}
			node->setInheritScale(inheritScale);
			node->setInheritOrientation(inheritOrientation);
			node->setScale(sx, sy, sz);
			node->translate(px, py, pz);
			node->rotate(rot);
			node->setVisible(true);
			node->setInitialState();
		}
		catch (Ogre::Exception e) {
			if (sceneMgr->hasSceneNode(nodeName)) {
				// There is a race condition high up in LG and there can be two requests
				//   to create the same scene node. This prevents that from being a bad thing.
				node = sceneMgr->getSceneNode(nodeName);
			}
		}
		return node;
	}

	// BETWEEN FRAME OPERATION
	void RendererOgre::UpdateSceneNode(const char* entName,
					bool updatePosition, float px, float py, float pz, 
					bool updateScale, float sx, float sy, float sz,
					bool updateRotation, float ow, float ox, float oy, float oz) {
		LG::Log("RendererOgre::UpdateSceneNode: update %s", entName);
		if (m_sceneMgr->hasSceneNode(entName)) {
			Ogre::SceneNode* sceneNode = m_sceneMgr->getSceneNode(entName);
			if (updatePosition) {
				sceneNode->setPosition(px, py, pz);
			}
			if (updateScale) {
				sceneNode->setScale(sx, sy, sz);
			}
			if (updateRotation) {
				LG::Log("RendererOgre::UpdateSceneNode: update rotation: w%f, x%f, y%f, z%f", ow, ox, oy, oz);
				sceneNode->setOrientation(ow, ox, oy, oz);
			}
			sceneNode->needUpdate(false);
		}
		else {
			LG::Log("RendererOgre::UpdateSceneNode: entity not found. Did not update entity %s", entName);
		}
		return;
	}

	// Passed a bunch of vertices and index information, create the mesh that goes with it.
	// The mesh is created and serialized to a .mesh file which just happens to be in the 
	// same spot as the resource looker-upper will look to find it when the mesh is reloaded.
	// BETWEEN FRAME OPERATION
	void RendererOgre::CreateMeshResource(const char* eName, const int faceCounts[], const float faceVertices[]) {
		Ogre::String entName = eName;
		Ogre::String manualObjectName = "MO/" + entName;
		Ogre::String baseMaterialName = entName;
		const int* fC = &faceCounts[1];
		const float* fV = &faceVertices[1];
		const int faces = *fC;
		fC += 1;

		Ogre::ManualObject* mo = m_sceneMgr->createManualObject(manualObjectName);
		mo->setCastShadows(true);
		// Ogre::ManualObject* mo = new Ogre::ManualObject(manualObjectName);
		LG::Log("RendererOgre::CreateMeshResource: Creating mo. f = %d, %s", faces, manualObjectName.c_str());

		int iface, iv;
		const float* fVf;
		char faceName[10];
		Ogre::String materialName;
		for (iface = 0; iface < faces; iface++) {
			itoa(iface, faceName, 10);
			materialName = baseMaterialName + "-" + faceName + ".material";
			mo->begin(materialName);
			fVf = fV + fC[0];
			// LG::Log("RendererOgre::CreateMeshResource: F%d: vertices %d, %d, %d", iface, fC[0], fC[1], fC[2]);
			for (iv=0; iv < fC[1]; iv++) {
				// LG::Log("RendererOgre::CreateMeshResource: %f, %f, %f, %f, %f", fVf[0], fVf[1], fVf[2], fVf[3], fVf[4] );
				mo->position(fVf[0], fVf[1], fVf[2]);
				mo->textureCoord(fVf[3], fVf[4]);
				mo->normal(fVf[5], fVf[6], fVf[7]);
				fVf += fC[2];
			}
			fC += 3;
			fVf = fV + fC[0];
			// LG::Log("RendererOgre::CreateMeshResource: F%d: indices %d, %d, %d", iface, fC[0], fC[1], fC[2]);
			for (iv=0; iv < fC[1]; iv += 3) {
				// LG::Log("RendererOgre::CreateMeshResource: %f, %f, %f", fVf[0], fVf[1], fVf[2]);
				mo->triangle((Ogre::uint32)fVf[0], (Ogre::uint32)fVf[1], (Ogre::uint32)fVf[2]);
				// mo->index((Ogre::uint32)fVf[0]);
				// mo->index((Ogre::uint32)fVf[1]);
				// mo->index((Ogre::uint32)fVf[2]);
				fVf += fC[2];
			}
			fC += 3;
			mo->end();
		}

		LG::Log("RendererOgre::CreateMeshResource: converting to mesh: %s", entName.c_str());
		// I thought I should have to find and unload the old mesh but
		// these do not the right things and don't know why. Removing comments causes crashes.
		// if (Ogre::MeshManager::getSingleton().resourceExists(entName)) {
		// 	Ogre::MeshManager::getSingleton().unload(entName);
		// 	Ogre::MeshManager::getSingleton().remove(entName);
		// }
		try {
			// Ogre::MeshManager::getSingleton().load(entName);
			Ogre::MeshPtr mesh = mo->convertToMesh(entName , OLResourceGroupName);
			mo->clear();
			m_sceneMgr->destroyManualObject(mo);
			mo = 0;

			// mesh->buildEdgeList();
			// mesh->generateLodLevels(m_lodDistances, Ogre::ProgressiveMesh::VertexReductionQuota::VRQ_PROPORTIONAL, 0.25f);

			if (m_serializeMeshes) {
				// serialize the mesh to the filesystem
				mesh->setBackgroundLoaded(true);
				LG::OLMeshTracker::Instance()->MakePersistant(mesh, entName);
			}
			// you'd think doing  the unload here would be the right thing but it causes crashes
			// Ogre::MeshManager::getSingleton().unload(entName);
		}
		catch (Ogre::Exception &e) {
			LG::Log("RendererOgre::CreateMeshResource: failure generating mesh: %s", e.getDescription().c_str());
			// This will leave the mesh as the default loading shape
			// and potentially create an ManualObject leak
		}


		return;
	}

	/*
	// Create a simple cube to be the loading mesh representation
	void RendererOgre::GenerateLoadingMesh() {
		Ogre::String loadingMeshName = "LookingGlass/LoadingShape";
		Ogre::String loadingMaterialName = "LookingGlass/LoadingShape";
		Ogre::String targetFilename = LG::GetParameter("Renderer.Ogre.DefaultMeshFilename");

		Ogre::String loadingMeshManualObjectName = "MO/" + loadingMeshName;

		Ogre::ManualObject* mo = m_sceneMgr->createManualObject(loadingMeshManualObjectName);
		LG::Log("RendererOgre::GenerateLoadingMesh: ");

		mo->begin(loadingMaterialName);
		// top
		mo->position(0.0, 1.0, 0.0);
		mo->position(1.0, 1.0, 0.0);
		mo->position(1.0, 0.0, 0.0);
		mo->position(0.0, 0.0, 0.0);
		mo->triangle(0, 1, 2);
		mo->triangle(0, 2, 3);
		
		// bottom
		mo->position(1.0, 1.0, 1.0);
		mo->position(0.0, 1.0, 1.0);
		mo->position(0.0, 0.0, 1.0);
		mo->position(1.0, 0.0, 1.0);
		mo->triangle(4, 5, 6);
		mo->triangle(4, 6, 7);

		// sides
		mo->triangle(5, 0, 3);
		mo->triangle(5, 3, 6);

		mo->triangle(1, 0, 5);
		mo->triangle(1, 5, 4);

		mo->triangle(7, 1, 4);
		mo->triangle(7, 2, 1);

		mo->triangle(3, 2, 7);
		mo->triangle(3, 7, 6);

		mo->end();

		Ogre::MeshPtr mesh = mo->convertToMesh(loadingMeshName , OLResourceGroupName);
		mo->clear();
		m_sceneMgr->destroyManualObject(mo);

		if (m_meshSerializer == NULL) {
			m_meshSerializer = new Ogre::MeshSerializer();
		}
		CreateParentDirectory(targetFilename);
		m_meshSerializer->exportMesh(mesh.getPointer(), targetFilename);

		// since this is called only once, we don't bother freeing the mesh
		return;
	}
*/
	// Passed a bunch of vertices and index information, create the mesh that goes with it.
	// The mesh is created and serialized to a .mesh file which just happens to be in the 
	// same spot as the resource looker-upper will look to find it when the mesh is reloaded.
	// NOTE: IN PROGRESS: an attempt to build the mesh directly rather than using ManualObject.
	// BETWEEN FRAME OPERATION
	void RendererOgre::CreateMeshResource2(const char* eName, const int faceCounts[], const float faceVertices[]) {
		Ogre::String entName = eName;
		Ogre::String manualObjectName = "MO/" + entName;
		Ogre::String baseMaterialName = entName;
		const int* fC = &faceCounts[1];
		const float* fV = &faceVertices[1];
		const int faces = *fC;
		fC += 1;

		Ogre::MeshPtr manualMesh = Ogre::MeshManager::getSingleton().createManual(manualObjectName, OLResourceGroupName);
		LG::Log("RendererOgre::CreateMeshResource2: Creating mo. f = %d, %s", 
				faces, manualMesh->getName().c_str());

			/*
		int iface, iv;
		const float* fVf;
		char faceName[10];
		Ogre::String materialName;
		for (iface = 0; iface < faces; iface++) {
			itoa(iface, faceName, 10);
			materialName = baseMaterialName + "-" + faceName + ".material";
			Ogre::SubMesh* faceMesh = manualMesh->createSubMesh();
			faceMesh->setMaterialName(materialName);
			Ogre::VertexData faceVerts = new Ogre::VertexData(fC[2]);

			faceMesh->vertexData = newVertexData;

			fVf = fV + fC[0];
			// LG::Log("RendererOgre::CreateMeshResource2: F%d: vertices %d, %d, %d", iface, fC[0], fC[1], fC[2]);
			for (iv=0; iv < fC[1]; iv++) {
				// LG::Log("RendererOgre::CreateMeshResource2: %f, %f, %f, %f, %f", fVf[0], fVf[1], fVf[2], fVf[3], fVf[4] );
				mo->position(fVf[0], fVf[1], fVf[2]);
				mo->textureCoord(fVf[3], fVf[4]);
				fVf += fC[2];
			}
			fC += 3;
			fVf = fV + fC[0];
			// LG::Log("RendererOgre::CreateMeshResource2: F%d: indices %d, %d, %d", iface, fC[0], fC[1], fC[2]);
			for (iv=0; iv < fC[1]; iv += 3) {
				// LG::Log("RendererOgre::CreateMeshResource2: %f, %f, %f", fVf[0], fVf[1], fVf[2]);
				mo->index((Ogre::uint32)fVf[0]);
				mo->index((Ogre::uint32)fVf[1]);
				mo->index((Ogre::uint32)fVf[2]);
				fVf += fC[2];
			}
			fC += 3;
			mo->end();
		}

		if (m_serializeMeshes) {
			// serialize the mesh to the filesystem
			m_meshTracker->MakePersistant(mesh, entName);
		}
			*/

		return;
	}

// Given a scene node for a terrain, find the manual object on that scene node and
// update the manual object with the heightmap passed. If  there is no manual object on
// the scene node, remove all it's attachments and add the manual object.
// The heightmap is passed in a 1D array ordered by width rows (for(width) {for(length) {hm[w,l]}})
// This must be called between frames since it touches the scene graph
// BETWEEN FRAME OPERATION
void RendererOgre::GenTerrainMesh(Ogre::SceneManager* sceneMgr, Ogre::SceneNode* node, 
								  const int hmWidth, const int hmLength, const float* hm) {

	// Find the movable object attached to the scene node. If not found remove all.
	if (node->numAttachedObjects() > 0) {
		Ogre::MovableObject* attached = node->getAttachedObject(0);
		if (attached->getMovableType() != "ManualObject") {
            // don't know why this would ever happen but clean out the odd stuff
            LG::Log("Found extra stuff on terrain scene node");
			node->detachAllObjects();
		}
	}
	// if there is not a manual object on the node, create a new one
	if (node->numAttachedObjects() == 0) {
		LG::Log("GenTerrainMesh: creating terrain ManualObject");
        // if no attached objects, we add our dynamic ManualObject
		Ogre::ManualObject* mob = sceneMgr->createManualObject("ManualObject/" + node->getName());
		mob->addQueryFlags(Ogre::SceneManager::WORLD_GEOMETRY_TYPE_MASK);
		mob->setDynamic(true);
		mob->setCastShadows(true);
		mob->setVisible(true);
		mob->setQueryFlags(Ogre::SceneManager::WORLD_GEOMETRY_TYPE_MASK);
		node->attachObject(mob);
		m_visCalc->RecalculateVisibility();
	}

	Ogre::ManualObject* mo = (Ogre::ManualObject*)node->getAttachedObject(0);

	// stuff our heightmap information into the dynamic manual object
	mo->estimateVertexCount(hmWidth * hmLength);
	mo->estimateIndexCount(hmWidth * hmLength * 6);

	if (mo->getNumSections() == 0) {
		mo->begin(m_defaultTerrainMaterial);	// if first time
	}
	else {
		mo->beginUpdate(0);					// we've been here before
	}

	int loc = 0;
	for (int xx = 0; xx < hmWidth; xx++) {
		for (int yy = 0; yy < hmLength; yy++) {
			mo->position(xx, yy, hm[loc++]);
			mo->textureCoord((float)xx / (float)hmWidth, (float)yy / (float)hmLength);
			mo->normal(0.0, 1.0, 0.0);	// always up (for the moment)
		}
	}

	for (int px = 0; px < hmLength-1; px++) {
		for (int py = 0; py < hmWidth-1; py++) {
			mo->quad(px      + py       * hmWidth,
					 px      + (py + 1) * hmWidth,
					(px + 1) + (py + 1) * hmWidth,
					(px + 1) + py       * hmWidth
					 );
		}
	}

	mo->end();

	return;
}

// BETWEEN FRAME OPERATION
void RendererOgre::AddOceanToRegion(Ogre::SceneManager* sceneMgr, Ogre::SceneNode* regionNode,
									const float width, const float length, const float waterHeight, const char* wName) {
	if (sceneMgr == 0) {
		LG::Log("AddOceanToRegion: passed null scene manager");
		return;
	}
	Ogre::String waterName = wName;
	Ogre::Plane* oceanPlane = new Ogre::Plane(0.0, 0.0, 1.0, 0);
	Ogre::MeshPtr oceanMesh = Ogre::MeshManager::getSingleton().createPlane(waterName, OLResourceGroupName, 
					*oceanPlane, width, length,
					2, 2, true,
					2, 2.0, 2.0, Ogre::Vector3::UNIT_Y);
	Ogre::String oceanMaterialName = LG::GetParameter("Renderer.Ogre.OceanMaterialName");
	LG::Log("AddOceanToRegion: r=%s, h=%f, n=%s, m=%s", 
		regionNode->getName().c_str(), waterHeight, wName, oceanMaterialName.c_str());
	oceanMesh->getSubMesh(0)->setMaterialName(oceanMaterialName);
	Ogre::Entity* oceanEntity = sceneMgr->createEntity("WaterEntity/" + waterName, oceanMesh->getName());
	oceanEntity->addQueryFlags(Ogre::SceneManager::WORLD_GEOMETRY_TYPE_MASK);
	oceanEntity->setCastShadows(false);
	Ogre::SceneNode* oceanNode = regionNode->createChildSceneNode("WaterSceneNode/" + waterName);
	oceanNode->setInheritOrientation(true);
	oceanNode->setInheritScale(false);
	oceanNode->translate(width/2.0, length/2.0, waterHeight);
	oceanNode->attachObject(oceanEntity);
	m_visCalc->RecalculateVisibility();
	return;
}

// ============= UTILITY ROUTINES
Ogre::String RendererOgre::EntityNameToFilename(const Ogre::String entName, const Ogre::String suffix) {
	Ogre::String fullFilename = m_cacheDir;
	fullFilename += "/";
	fullFilename += entName;
	fullFilename += suffix;
	return fullFilename;
}
// Given a filename, make sure all it's parent directories exist
void RendererOgre::CreateParentDirectory(const Ogre::String filename) {
	// make any backslashes into forward slashes
	Ogre::String fn = filename;
	Ogre::String::size_type ii;
	while ((ii = fn.find_first_of("\\")) != Ogre::String::npos) {
		fn.replace(ii, 1, 1, '/');
	}
	MakeParentDir(fn);
	return;
}

void RendererOgre::MakeParentDir(const Ogre::String filename) {
	Ogre::String::size_type lastSlash = filename.find_last_of('/');
	Ogre::String dirName = filename.substr(0, lastSlash);
	int iResult = _mkdir(dirName.c_str());			// try to make the directory
	if (iResult != 0) {							// if it couldn't be made
		if (errno == ENOENT) {					// if it couldn't make because no parents
			// LG::Log("RendererOgre::MakeParentDir: recursing for %s", dirName.c_str());
			MakeParentDir(dirName);				// create the parent directory
			_mkdir(dirName.c_str());				// make the directory this time
		}
	}
	return;
}

/*
// call out to the main program and make sure we should keep running
const bool RendererOgre::checkKeepRunning() {
	if (LG::checkKeepRunningCallback != NULL) {
		return (*LG::checkKeepRunningCallback)();
	}
	return false;
}

// Routine which calls back into the managed world to fetch a string/value configuration
// parameter.
const char* RendererOgre::GetParameter(const char* paramName) {
	if (LG::fetchParameterCallback != NULL) {
		return (*LG::fetchParameterCallback)(paramName);
	}
	else {
		LG::Log("RendererOgre::GetParameter: could not get parameter %s", paramName);
	}
	return NULL;
}

// Print out a message of the pointer thing is null. At least the log will know
// of the problem
void RendererOgre::AssertNonNull(void* thing, const char* msg) {
	if (thing == NULL) {
		LG::Log(msg);
	}
}

// Call back into the managed world to output a log message with formatting
void RendererOgre::Log(const char* msg, ...) {
	char buff[1024];
	if (LG::debugLogCallback != NULL) {
		va_list args;
		va_start(args, msg);
		vsprintf(buff, msg, args);
		va_end(args);
		(*LG::debugLogCallback)(buff);
	}
}

// Do a printf and return a newly allocated buffer (caller has to free it)
char* RendererOgre::formatIt(const char* msg, ...) {
	char* buff = (char*)OGRE_MALLOC(256, Ogre::MEMCATEGORY_GENERAL);
	va_list args;
	va_start(args, msg);
	vsnprintf(buff, 256, msg, args);
	va_end(args);
	return buff;
}

// Do a printf and return a newly allocated buffer (caller has to free it)
void RendererOgre::formatIt(Ogre::String& dst, const char* msg, ...) {
	char buff[1024];
	va_list args;
	va_start(args, msg);
	vsnprintf(buff, 256, msg, args);
	va_end(args);
	dst = buff;
	return;
}
*/

}

