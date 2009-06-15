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

// LookingGlassOgre.cpp : Defines the exported functions for the DLL application.

#include "stdafx.h"
#include <stdarg.h>
#include "LookingGlassOgre.h"
#include "RendererOgre.h"

// The switch yard from the managed to unmanaged.
// All of the external entry points are declared in this module.
// Some small amount of work is done here if it's just a line or two (like the
// wrappers for Ogre classes) but most work is passed into instances of other
// classes.
namespace LookingGlassOgr {

DebugLogCallback* debugLogCallback;
FetchParameterCallback* fetchParameterCallback;
CheckKeepRunningCallback* checkKeepRunningCallback;
UserIOCallback* userIOCallback;
RequestResourceCallback* requestResourceCallback;
BetweenFramesCallback* betweenFramesCallback;

RendererOgre::RendererOgre* m_ro;

// ==========================================================
extern "C" DLLExport void InitializeOgre() {
	m_ro = new RendererOgre::RendererOgre();
	m_ro->initialize();
	return;
}

extern "C" DLLExport void ShutdownOgre() {
}
// pass the main thread into the renderer
extern "C" DLLExport bool RenderingThread() {
	return m_ro->renderingThread();
}

// ==========================================================
extern "C" DLLExport void SetFetchParameterCallback(FetchParameterCallback* fpc) {
	fetchParameterCallback = fpc;
}
extern "C" DLLExport void SetDebugLogCallback(DebugLogCallback* dlc) {
	debugLogCallback = dlc;
}
extern "C" DLLExport void SetCheckKeepRunningCallback(CheckKeepRunningCallback* ckrc) {
	checkKeepRunningCallback = ckrc;
}
extern "C" DLLExport void SetUserIOCallback(UserIOCallback* uioc) {
	userIOCallback = uioc;
}
extern "C" DLLExport void SetRequestResourceCallback(RequestResourceCallback* rr) {
	requestResourceCallback = rr;
}
extern "C" DLLExport void SetBetweenFramesCallback(BetweenFramesCallback* bf) {
	betweenFramesCallback = bf;
}
// ==========================================================
// update the camera position with a position and a direction
extern "C" DLLExport void UpdateCamera(float px, float py, float pz, 
									   float dw, float dx, float dy, float dz,
									   float nearClip, float farClip, float aspect) {
	m_ro->updateCamera(px, py, pz, dw, dx, dy, dz, nearClip, farClip, aspect);
	return;
}
extern "C" DLLExport void RefreshResource(int rType, char* resourceName) {
	Ogre::String resName = resourceName;
	RefreshResourceI(resName, rType);
}
extern "C" DLLExport void CreateMeshResource(const char* meshName, const int* faceCounts, const float* faceVertices) {
	m_ro->CreateMeshResource(meshName, faceCounts, faceVertices);
}
extern "C" DLLExport void CreateMaterialResource(const char* meshName, char* textureName,
		 const float colorR, const float colorG, const float colorB, const float colorA,
		 const float glow, const bool fullBright, const int shiny, const int bump) {
	 m_ro->MaterialTracker()->CreateMaterialResource(meshName, textureName, 
		 colorR, colorG, colorB, colorA, glow, fullBright, shiny, bump);
}
extern "C" DLLExport void CreateMaterialResource2(const char* meshName, char* textureName, 
												  const float* parms) {
	m_ro->MaterialTracker()->CreateMaterialResource2(meshName, textureName, parms);
}

// ================================================================
extern "C" DLLExport void GenTerrainMesh(Ogre::SceneManager* smgr, Ogre::SceneNode* terrainNode, 
	 const int width, const int length, const float* heights) {
		 m_ro->GenTerrainMesh(smgr, terrainNode, width, length, heights);
}
extern "C" DLLExport void AddOceanToRegion(Ogre::SceneManager* smgr, Ogre::SceneNode* regionNode, 
			   const float width, const float length, const float waterHeight, const char* waterName) {
	m_ro->AddOceanToRegion(smgr, regionNode, width, length, waterHeight, waterName);
	return;
}

// ================================================================
// SceneNode operations
extern "C" DLLExport Ogre::SceneManager* GetSceneMgr() {
	return m_ro->m_sceneMgr;
}
extern "C" DLLExport Ogre::SceneNode* RootNode(Ogre::SceneManager* sceneMgr) {
	return sceneMgr->getRootSceneNode();
}
extern "C" DLLExport Ogre::SceneNode* CreateSceneNode(
					Ogre::SceneManager* sceneMgr, 
					char* nodeName,
					Ogre::SceneNode* parentNode,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz) {
	Ogre::SceneNode* node = NULL;

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
	node->rotate(Ogre::Quaternion(ow, ox, oy, oz));
	node->setVisible(true);
	node->setInitialState();
	return node;

}
extern "C" DLLExport Ogre::SceneNode* CreateChildSceneNode(Ogre::SceneNode* node) {
	return node->createChildSceneNode();
}
extern "C" DLLExport void AddEntity(Ogre::SceneManager* sceneMgr, Ogre::SceneNode* sceneNode, 
									char* entName, char* meshName) {
	m_ro->AddEntity(sceneMgr, sceneNode, entName, meshName);
}
extern "C" DLLExport void SceneNodeScale(Ogre::SceneNode* sceneNode, float sX, float sY, float sZ) {
	sceneNode->setScale(sX, sY, sZ);
}
extern "C" DLLExport void SceneNodePosition(Ogre::SceneNode* sceneNode, float pX, float pY, float pZ) {
	sceneNode->setPosition(pX, pY, pZ);
}
extern "C" DLLExport void SceneNodePitch(Ogre::SceneNode* sceneNode, float pitch, int ts) {
	sceneNode->pitch(Ogre::Radian(pitch), (Ogre::SceneNode::TransformSpace)ts);
}
extern "C" DLLExport void SceneNodeYaw(Ogre::SceneNode* sceneNode, float yaw, int ts) {
	sceneNode->yaw(Ogre::Radian(yaw), (Ogre::SceneNode::TransformSpace)ts);
}
// ================================================================
Ogre::Root* GetOgreRoot() {
	return m_ro->m_root;
}

// ================================================================
// ================================================================
// Routine which calls back into the managed world to fetch a string/value configuration
// parameter.
const char* LookingGlassOgr::GetParameter(const char* paramName) {
	if (LookingGlassOgr::fetchParameterCallback != NULL) {
		return (*LookingGlassOgr::fetchParameterCallback)(paramName);
	}
	else {
		LookingGlassOgr::Log("DEBUG: LookingGlassOrge: could not get parameter %s", paramName);
	}
	return NULL;
}

// Print out a message of the pointer thing is null. At least the log will know
// of the problem
void LookingGlassOgr::AssertNonNull(void* thing, const char* msg) {
	if (thing == NULL) {
		LookingGlassOgr::Log(msg);
	}
}

// return 'true' if the passed text is either "true" or "yes". Return false otherwise.
const bool LookingGlassOgr::isTrue(const char* txt) {
	if ((stricmp(txt, "true") == 0) || (stricmp(txt, "yes") == 0)) return true;
	return false;
}


// Call back into the managed world to output a log message with formatting
void LookingGlassOgr::Log(const char* msg, ...) {
	char buff[1024];
	if (LookingGlassOgr::debugLogCallback != NULL) {
		va_list args;
		va_start(args, msg);
		vsprintf(buff, msg, args);
		va_end(args);
		(*LookingGlassOgr::debugLogCallback)(buff);
	}
}

// call out to the main program and make sure we should keep running
const bool LookingGlassOgr::checkKeepRunning() {
	if (LookingGlassOgr::checkKeepRunningCallback != NULL) {
		return (*LookingGlassOgr::checkKeepRunningCallback)();
	}
	return false;
}

// Routine which calls back into the managed world to request a texture be loaded
void LookingGlassOgr::RequestResource(const char* contextEntName, const char* paramName, const int type) {
	if (LookingGlassOgr::requestResourceCallback != NULL) {
		(*LookingGlassOgr::requestResourceCallback)(contextEntName, paramName, type);
	}
}

// Internal request to refresh a resource
void LookingGlassOgr::RefreshResourceI(const Ogre::String& resName, const int rType) {
	if (rType == LookingGlassOgr::ResourceTypeMesh) {
		Ogre::MeshPtr theMesh = (Ogre::MeshPtr)Ogre::MeshManager::getSingleton().getByName(resName);
		// unload it and let the renderer decide if it needs to be loaded again
		if (!theMesh.isNull()) theMesh->unload();
	}
	if (rType == LookingGlassOgr::ResourceTypeMaterial) {
		// mark it so the work happens later between frames (more queues to manage correctly someday)
		m_ro->MaterialTracker()->MarkMaterialModified(resName);
	}
	if (rType == LookingGlassOgr::ResourceTypeTexture) {
		Ogre::TextureManager::getSingleton().unload(resName);
		m_ro->MaterialTracker()->MarkTextureModified(resName);
	}
}

}