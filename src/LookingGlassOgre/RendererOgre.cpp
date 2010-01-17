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
#include "LGLocking.h"
#include "RendererOgre.h"
#include "LookingGlassOgre.h"
#include "OLArchive.h"
#include "OLPreloadArchive.h"
#include "RegionTracker.h"
#include "ResourceListeners.h"
#include "ProcessBetweenFrame.h"
#include "ProcessAnyTime.h"
#include "ShadowBase.h"
#include "ShadowSimple.h"
#include "Shadow02.h"
#include "Shadow06.h"
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
	Ogre::Timer* rendererTimeKeeper = new Ogre::Timer();
	bool RendererOgre::renderingThread() {
		LG::Log("RendererOgre::renderingThread: LookingGlassOrge: Starting rendering");
		// m_root->startRendering();

		// new way that tried to control the amount of work between frames to keep
		//   frame rate up
		int maxFPS = LG::GetParameterInt("Renderer.Ogre.FramePerSecMax");
		if (maxFPS < 2 || maxFPS > 100) maxFPS = 20;
		int msPerFrame = 1000 / maxFPS;

		unsigned long now = rendererTimeKeeper->getMilliseconds();
		unsigned long timeStartedLastFrame = rendererTimeKeeper->getMilliseconds();

		while (m_root->renderOneFrame()) {
			Ogre::WindowEventUtilities::messagePump();
			now = rendererTimeKeeper->getMilliseconds();
			/*
			int remaining = msPerFrame - ((int)(now - timeStartedLastFrame));
			while (remaining > 10) {
				if (LG::ProcessBetweenFrame::Instance()->HasWorkItems()) {
					LG::ProcessBetweenFrame::Instance()->ProcessWorkItems(3);
				}
				else {
					Sleep(remaining);
				}
				now = rendererTimeKeeper->getMilliseconds();
				remaining = msPerFrame - ((int)(now - timeStartedLastFrame));
			}
			*/
			int totalMSForLastFrame = (int)(rendererTimeKeeper->getMilliseconds() - timeStartedLastFrame);
			if (totalMSForLastFrame < 0) totalMSForLastFrame = 1;
			LG::SetStat(LG::StatFramesPerSecond, 1000000/totalMSForLastFrame);
			timeStartedLastFrame = rendererTimeKeeper->getMilliseconds();
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
	bool RendererOgre::renderOneFrame(bool pump, int len) {
		bool ret = true;
		unsigned long now = rendererTimeKeeper->getMilliseconds();
		unsigned long timeStartedLastFrame = rendererTimeKeeper->getMilliseconds();
		if (m_root != NULL) {
			LGLOCK_LOCK(m_sceneGraphLock);
			try {
				ret = m_root->renderOneFrame();
			}
			catch (Ogre::Exception e) {
				LG::Log("RendererOgre::renderOneFrame: m_root->renderOneFrame() threw: %s", e.getFullDescription());
			}
			catch (...) {
				LG::Log("RendererOgre::renderOneFrame: m_root->renderOneFrame() threw");
			}
			LGLOCK_UNLOCK(m_sceneGraphLock);
			LGLOCK_NOTIFY_ALL(m_sceneGraphLock);
			try {
				if (pump) Ogre::WindowEventUtilities::messagePump();
			}
			catch (...) {
				LG::Log("RendererOgre::renderOneFrame: messagePump threw");
			}
		}

		/*
		// The amount of time a frame takes to render is passed to us
		// If we have time left over and there is between frame processing, do them
		// DEBUG NOTE: removed here in lue of ProcessBetweenFrames using a fixed
		// amount of time of processing.
		int remaining = len - ((int)(now - timeStartedLastFrame));
		while (remaining > 10) {
			if (m_processBetweenFrame->HasWorkItems()) {
				m_processBetweenFrame->ProcessWorkItems(20);
			}
			else {
				break;
			}
			now = rendererTimeKeeper->getMilliseconds();
			remaining = len - ((int)(now - timeStartedLastFrame));
		}
		*/
		try {
			int totalMSForLastFrame = (int)(rendererTimeKeeper->getMilliseconds() - m_lastFrameTime);
			if (totalMSForLastFrame <= 0) totalMSForLastFrame = 1;
			LG::SetStat(LG::StatLastFrameMs, totalMSForLastFrame);
			LG::SetStat(LG::StatFramesPerSecond, 1000000/totalMSForLastFrame);
			m_lastFrameTime = rendererTimeKeeper->getMilliseconds();

			if (!ret) {
				// if renderOneFrame returns false, it means we're going down
			}
		}
		catch (...) {
			LG::Log("RendererOgre::renderOneFrame: calculating frame interval threw");
		}

		return ret;
	}

	// Update the camera position given an location and a direction
	void RendererOgre::updateCamera(double px, double py, double pz, 
				float dw, float dx, float dy, float dz,
				float nearClip, float farClip, float aspect) {
		if (m_camera) {
			LG::Log("RendererOgre::UpdateCamera: pos=<%f, %f, %f>", (double)px, (double)py, (double)pz);
			m_camera->setPosition(px, py, pz);
			m_desiredCameraOrientation = Ogre::Quaternion(dw, dx, dy, dz);
			m_desiredCameraOrientationProgress = 0.0;
			// to do slerped movement, comment the next line and uncomment "XXXX" below
			// m_camera->setOrientation(Ogre::Quaternion(dw, dx, dy, dz));
			/*	don't fool with far and clip for the moment
			if (nearClip != m_camera->getNearClipDistance()) {
				m_camera->setNearClipDistance(nearClip);
			}
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
		float progress = evt.timeSinceLastFrame / SECONDS_TO_SLERP;
		m_desiredCameraOrientationProgress += progress;
		if (m_desiredCameraOrientationProgress > 0) {
			// if greater than zero we're working on progress
			if (m_desiredCameraOrientationProgress < 1.0) {
				// still within the progress area
				// Ogre::Quaternion newOrientation = Ogre::Quaternion::Slerp(m_desiredCameraOrientationProgress, 
				Ogre::Quaternion newOrientation = Ogre::Quaternion::nlerp(m_desiredCameraOrientationProgress, 
					m_camera->getOrientation(), m_desiredCameraOrientation, true);
				m_camera->setOrientation(newOrientation); // XXXX
				m_visCalc->RecalculateVisibility(); // XXXX
			}
			else {
				// we've advanced to progress. Make sure we get the last event in
				m_camera->setOrientation(m_desiredCameraOrientation);
				m_desiredCameraOrientationProgress = -1.0;	// flag to say done
			}
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
			LG::Log("RendererOgre::initialize: successfully initialized resource groups");
			// turn on the resource system
	        initOgreResources();
			LG::Log("RendererOgre::initialize: successfully initialized ogre resources");
		}
		catch (char* str) {
			LG::Log("RendererOgre::initialize: LookingGlassOrge: exception initializing: %s", str);
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
		// The singleton classes might create threads so let the render system know that's coming
#if OGRE_THREAD_SUPPORT > 0
		LG::Log("RendererOgre::createLookingGlassResourceGroups: THREAD SUPPORT ON = %d", OGRE_THREAD_SUPPORT);
		m_root->getRenderSystem()->preExtraThreadsStarted();
#endif
		LG::ProcessBetweenFrame::Instance();
		// LG::ProcessAnyTime::Instance();
		LG::OLMaterialTracker::Instance();
		LG::OLMeshTracker::Instance();
		LG::RegionTracker::Instance();
#if OGRE_THREAD_SUPPORT > 0
		while (!LGLOCK_THREADS_AREINITIALIZED) {
			// wait for any initializing threads to do their thing before doing post...
			LGLOCK_SLEEP(1);
		}
		m_root->getRenderSystem()->postExtraThreadsStarted();
#endif

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
		LG::Log("RendererOgre::createLookingGlassResourceGroups: all resource archives added");
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
			m_sceneMgr->setCameraRelativeRendering(true);
			const char* shadowName = LG::GetParameter("Renderer.Ogre.ShadowTechnique");
			if (strlen(shadowName) == 0 || stricmp(shadowName, "none") == 0) {
				this->Shadow = new ShadowBase();
			}
			else if (stricmp(shadowName, "shadow02") == 0) {
				this->Shadow = new Shadow02(shadowName);
			}
			else if (stricmp(shadowName, "shadow06") == 0) {
				this->Shadow = new Shadow06(shadowName);
			}
			else {
				this->Shadow = new ShadowSimple(shadowName);
			}
		}
		catch (std::exception e) {
			LG::Log("RendererOgre::createScene: Exception %s", e.what());
			return;
		}
	}

	void RendererOgre::createCamera() {
		LG::Log("RendererOgre::createCamera");
		m_camera = new LG::LGCamera("MainCamera", m_sceneMgr);
	}

	void RendererOgre::createViewport() {
		LG::Log("RendererOgre::createViewport");
		m_viewport = m_window->addViewport(m_camera->Cam);
		m_viewport->setBackgroundColour(Ogre::ColourValue(0.0f, 0.0f, 0.25f));
		m_camera->Cam->setAspectRatio((float)m_viewport->getActualWidth() / (float)m_viewport->getActualHeight());
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
			try {
				if ((betweenFrameCounter % 10) == 0) {
					return (*LG::betweenFramesCallback)();
				}
			}
			catch (...) {
				LG::Log("RendererOgre: EXCEPTION FRAMEENDED:");
			}
		}
		return true;
	}

	// ========== end of Ogre::FrameListener

	// ============= REQUESTS TO DO WORK
	// BETWEEN FRAME OPERATION
	void RendererOgre::AddEntity(Ogre::SceneManager* sceneMgr, Ogre::SceneNode* sceneNode,
							const char* entName, const char* meshNam) {
		// LG::Log("RendererOgre::AddEntity: declare %s, t=%s, g=%s", meshNam, "Mesh", OLResourceGroupName);
		Ogre::String meshName = Ogre::String(meshNam);
		Ogre::ResourceGroupManager::getSingleton().declareResource(meshName,
								"Mesh", OLResourceGroupName
								);
		try {
			// This createEntity call does a 'load' operation on the mesh
			// Really shouldn't be doing this on the between frame thread.
			// DEVELOPMENT NOTE: because createEntity causes a load (and the delay waiting
			// for the file to load) the commenting out below means that AddEntity always
			// happens in the queued, non-between frame thread. When Ogre is compiled with
			// threading on, this makes the loading happen on the other thread.
			// The code in the 'if' is duplicated in OLMeshTracker.
			/*
			Ogre::ResourcePtr theMesh = Ogre::MeshManager::getSingleton().getByName(meshName);
			if ((!theMesh.isNull()) && theMesh->isLoaded()) {
				// LG::Log("RendererOgre::AddEntity: immediate create of %s", meshName.c_str());
				Ogre::MovableObject* ent = sceneMgr->createEntity(entName, meshName);
				// it's not scenery
				ent->removeQueryFlags(Ogre::SceneManager::WORLD_GEOMETRY_TYPE_MASK);	
				Shadow->AddCasterShadow(ent);
				sceneNode->attachObject(ent);
				m_visCalc->RecalculateVisibility();
			}
			else {
				// LG::Log("RendererOgre::AddEntity: request loading of %s", meshName.c_str());
				LG::OLMeshTracker::Instance()->MakeLoaded(sceneNode, meshName, Ogre::String(entName));
			}
			*/
			LG::OLMeshTracker::Instance()->MakeLoaded(sceneNode, meshName, Ogre::String(entName));
		}
		catch (Ogre::Exception e) {
			// we presume this is because the entity already exists
		}
		return;
	}

	// BETWEEN FRAME OPERATION
	Ogre::SceneNode* RendererOgre::CreateSceneNode(const char* nodeName,
					Ogre::SceneNode* parentNode,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz) {
		return CreateSceneNode(this->m_sceneMgr, nodeName, parentNode,
				inheritScale, inheritOrientation, px, py, pz, sx, sy, sz, ow, ox ,oy, oz);
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
			sceneNode->needUpdate(true);
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
		Ogre::String entName = Ogre::String(eName);
		Ogre::String manualObjectName = "MO/" + entName;
		Ogre::String baseMaterialName = entName;
		const int* fC = &faceCounts[1];
		const float* fV = &faceVertices[1];
		const int faces = *fC;
		fC += 1;

		Ogre::ManualObject* mo = m_sceneMgr->createManualObject(manualObjectName);
		Shadow->AddCasterShadow(mo);
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
			const float* vColor = fVf;
			fVf += 4;
			// LG::Log("RendererOgre::CreateMeshResource: F%d: vertices %d, %d, %d", iface, fC[0], fC[1], fC[2]);
			for (iv=0; iv < fC[1]; iv++) {
				// LG::Log("RendererOgre::CreateMeshResource: %f, %f, %f, %f, %f", fVf[0], fVf[1], fVf[2], fVf[3], fVf[4] );
				mo->position(fVf[0], fVf[1], fVf[2]);
				mo->colour(vColor[0], vColor[1], vColor[2], vColor[3]);
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
		// 
		try {
			// Ogre::MeshManager::getSingleton().load(entName);
			Ogre::MeshPtr mesh = mo->convertToMesh(entName , OLResourceGroupName);
			mo->clear();
			m_sceneMgr->destroyManualObject(mo);
			mo = 0;
			mesh->buildEdgeList();

			std::vector<Ogre::Real> m_lodDistances(3);
			m_lodDistances[0] = 100;
			m_lodDistances[1] = 200;
			m_lodDistances[2] = 400;
			// DEBUG NOTE: uncommenting this causes a crash. Why?
			// mesh->generateLodLevels(m_lodDistances, Ogre::ProgressiveMesh::VRQ_PROPORTIONAL, 0.5f);

			if (m_serializeMeshes) {
				// serialize the mesh to the filesystem
				// DEBUG NOTE: The call to MakePersistant causes a crash. Not sure why doing the op
				//   on another thread and not here (between frames) causes  the crash -- shouldn't with
				//   Ogre threading turned on. The old, inline code is currently still here and being used.
				// LG::OLMeshTracker::Instance()->MakePersistant(mesh->getName(), entName, Ogre::String(), NULL);
				Ogre::String targetFilename = LG::RendererOgre::Instance()->EntityNameToFilename(mesh->getName(), "");

				// Make sure the directory exists -- I wish the serializer did this for me
				LG::RendererOgre::Instance()->CreateParentDirectory(targetFilename);
				
				LG::Log("RendererOgre::CreateMeshResource: serializing mesh to %s", targetFilename.c_str());
				LG::OLMeshTracker::Instance()->MeshSerializer->exportMesh(mesh.getPointer(), targetFilename);
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

	// given an entity, free up it's meshes and pieces and then the entity itself
	void RendererOgre::CleanAndDeleteEntity(Ogre::MovableObject* mo) {
		if (mo->getMovableType() == "Entity") {
			Ogre::Entity* ent = (Ogre::Entity*)mo;
			Ogre::MeshPtr mesh = ent->getMesh();
			if (! mesh.isNull()) {
				LG::OLMeshTracker::Instance()->DeleteMesh(mesh);
			}
			LG::RendererOgre::Instance()->m_sceneMgr->destroyEntity(ent);
		}
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

