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
#include "RegionTracker.h"
#include "RendererOgre.h"
#include "ProcessBetweenFrame.h"

// The switch yard from the managed to unmanaged.
// All of the external entry points are declared in this module.
// Some small amount of work is done here if it's just a line or two (like the
// wrappers for Ogre classes) but most work is passed into instances of other
// classes.
namespace LG {

DebugLogCallback* debugLogCallback;
FetchParameterCallback* fetchParameterCallback;
CheckKeepRunningCallback* checkKeepRunningCallback;
UserIOCallback* userIOCallback;
RequestResourceCallback* requestResourceCallback;
BetweenFramesCallback* betweenFramesCallback;

int* statsBlock;

// ==========================================================
extern "C" DLLExport void InitializeOgre() {
	LG::RendererOgre::Instance()->initialize();
	return;
}

extern "C" DLLExport void ShutdownOgre() {
}
// pass the main thread into the renderer
extern "C" DLLExport bool RenderingThread() {
	return LG::RendererOgre::Instance()->renderingThread();
}

// a call that renders only one frams
extern "C" DLLExport bool RenderOneFrame(bool pump, int length) {
	return LG::RendererOgre::Instance()->renderOneFrame(pump, length);
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
extern "C" DLLExport void SetStatsBlock(int* sb) {
	statsBlock = sb;
}
// ==========================================================
// update the camera position with a position and a direction
extern "C" DLLExport void UpdateCameraBF(double px, double py, double pz, 
									   float dw, float dx, float dy, float dz,
									   float nearClip, float farClip, float aspect) {
	LG::ProcessBetweenFrame::Instance()->UpdateCamera(px, py, pz, dw, dx, dy, dz, nearClip, farClip, aspect);
}
extern "C" DLLExport bool AttachCamera(const char* parentNode, float offsetX, float offsetY, float offsetZ,
									   float ow, float ox, float oy, float oz) {
   return LG::RendererOgre::Instance()->m_camera->AttachCamera(parentNode, offsetX, offsetY, offsetZ, ow, ox, oy, oz);
}
extern "C" DLLExport void RefreshResourceBF(float pri, int rType, char* resourceName) {
	LG::ProcessBetweenFrame::Instance()->RefreshResource(pri, resourceName, rType);
}
extern "C" DLLExport void CreateMeshResourceBF(float pri, const char* meshName, char* contextSceneNode, 
											   const int* faceCounts, const float* faceVertices) {
	LG::ProcessBetweenFrame::Instance()->CreateMeshResource(pri, meshName, contextSceneNode, faceCounts, faceVertices);
}
extern "C" DLLExport void CreateMaterialResource(const char* matName, char* textureName,
		 const float colorR, const float colorG, const float colorB, const float colorA,
		 const float glow, const bool fullBright, const int shiny, const int bump) {
	 LG::OLMaterialTracker::Instance()->CreateMaterialResource(matName, textureName, 
		 colorR, colorG, colorB, colorA, glow, fullBright, shiny, bump);
}
extern "C" DLLExport void CreateMaterialResource2(const char* matName, char* textureName, 
												  const float* parms) {
	  LG::OLMaterialTracker::Instance()->CreateMaterialResource2(matName, textureName, parms);
}
extern "C" DLLExport void CreateMaterialResource2BF(float pri, const char* matName, char* textureName, 
												  const float* parms) {
	  LG::ProcessBetweenFrame::Instance()->CreateMaterialResource2(pri, matName, textureName, parms);
}

extern "C" DLLExport void CreateMaterialResource7BF(float prio, char* uniq,
			const char* matName1, const char* matName2, const char* matName3, 
			const char* matName4, const char* matName5, const char* matName6, 
			const char* matName7,
			char* textureName1, char* textureName2, char* textureName3, 
			char* textureName4, char* textureName5, char* textureName6, 
			char* textureName7,
			const float* parms) {
	LG::ProcessBetweenFrame::Instance()->CreateMaterialResource7(prio, uniq,
						matName1, matName2, matName3, 
						matName4, matName5, matName6, matName7,
						textureName1, textureName2, textureName3, 
						textureName4, textureName5, textureName6, textureName7,
						parms);
}

extern "C" DLLExport void DiagnosticAction(int flag) {
	return;
}

// ================================================================
extern "C" DLLExport void AddRegionBF(float prio, const char* regionNodeName, 
		 double globalX, double globalY, double globalZ,
		 const float sizeX, const float sizeY, const float waterHeight) {
	 LG::ProcessBetweenFrame::Instance()->AddRegion(prio, regionNodeName, 
		 globalX, globalY, globalZ, sizeX, sizeY, waterHeight);
}
extern "C" DLLExport void UpdateTerrainBF(float prio, const char* regionName, 
											const int width, const int length, const float* heights) {
	 LG::ProcessBetweenFrame::Instance()->UpdateTerrain(prio, regionName, width, length, heights);
}
extern "C" DLLExport void SetFocusRegionBF(const char* regionName) {
	// LG::RegionTracker::Instance()->SetFocusRegion(regionName);
	 LG::ProcessBetweenFrame::Instance()->SetFocusRegion(0.0, regionName);
}
extern "C" DLLExport void SetRegionDetailBF(const char* regionName, const RegionRezCode LODLevel) {
	// LG::RegionTracker::Instance()->SetFocusRegion(regionName);
	 LG::ProcessBetweenFrame::Instance()->SetRegionDetail(0.0, regionName, LODLevel);
}

// ================================================================
// SceneNode operations
extern "C" DLLExport Ogre::SceneManager* GetSceneMgr() {
	return LG::RendererOgre::Instance()->m_sceneMgr;
}
extern "C" DLLExport Ogre::SceneNode* RootNode(Ogre::SceneManager* sceneMgr) {
	return sceneMgr->getRootSceneNode();
}
extern "C" DLLExport bool CreateMeshSceneNodeBF(float pri,
					Ogre::SceneManager* sceneMgr, 
					char* sceneNodeName,
					char* parentNodeName,
					char* entityName,
					char* meshName,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz) {
	Ogre::SceneNode* parentNode = NULL;
	if (parentNodeName != 0) {
		Ogre::String parentNodeNameS = Ogre::String(parentNodeName);
		if (sceneMgr->hasSceneNode(parentNodeNameS)) {
			parentNode = sceneMgr->getSceneNode(parentNodeNameS);
		}
		else {
			return false;	// cannot create it now
		}
	}
	LG::ProcessBetweenFrame::Instance()->CreateMeshSceneNode(pri, sceneMgr, 
			sceneNodeName, parentNode, entityName, meshName,
			inheritScale, inheritOrientation,
			px, py, pz, sx, sy, sz,
			ow, ox, oy, oz);
	return true;	// successful creation
}

extern "C" DLLExport Ogre::SceneNode* CreateSceneNode(
					Ogre::SceneManager* sceneMgr, 
					char* nodeName,
					Ogre::SceneNode* parentNode,
					bool inheritScale, bool inheritOrientation,
					float px, float py, float pz,
					float sx, float sy, float sz,
					float ow, float ox, float oy, float oz) {
	return LG::RendererOgre::Instance()->CreateSceneNode(sceneMgr, nodeName, parentNode,
		inheritScale, inheritOrientation, px, py, pz, sx, sy, sz, ow, ox, oy, oz);
}
extern "C" DLLExport void UpdateSceneNodeBF(float pri,
					char* nodeName,
					bool setPosition, float px, float py, float pz, float pd,
					bool setScale, float sx, float sy, float sz, float sd,
					bool setRotation, float ow, float ox, float oy, float oz, float od) {
	LG::ProcessBetweenFrame::Instance()->UpdateSceneNode(pri, nodeName,
					setPosition, px, py, pz, pd,
					setScale, sx, sy, sz, sd,
					setRotation, ow, ox, oy, oz, od);
	return;
}
extern "C" DLLExport void RemoveSceneNodeBF(float prio, char* sceneNodeName) {
	LG::ProcessBetweenFrame::Instance()->RemoveSceneNode(prio, sceneNodeName);
}
// ================================================================
extern "C" DLLExport void UpdateAnimationBF(float prio, char* sceneNodeName, float X, float Y, float Z, float rate) {
	LG::ProcessBetweenFrame::Instance()->UpdateAnimation(prio, sceneNodeName, X, Y, Z, rate);
}

// ================================================================
Ogre::Root* GetOgreRoot() {
	return LG::RendererOgre::Instance()->m_root;
}

// ================================================================
void LG::SetStat(int cod, int val) {
	if (LG::statsBlock != NULL) {
		LG::statsBlock[cod] = val;
	}
}

void LG::IncStat(int cod) {
	if (LG::statsBlock != NULL) {
		LG::statsBlock[cod]++;
	}
}

void LG::DecStat(int cod) {
	if (LG::statsBlock != NULL) {
		LG::statsBlock[cod]--;
	}
}

// Routine which calls back into the managed world to fetch a string/value configuration
// parameter.
const char* LG::GetParameter(const char* paramName) {
	if (LG::fetchParameterCallback != NULL) {
		return (*LG::fetchParameterCallback)(paramName);
	}
	else {
		LG::Log("DEBUG: LookingGlassOrge: could not get parameter %s", paramName);
	}
	return NULL;
}

const int LG::GetParameterInt(const char* paramName) {
	int inum = 0;
	if (sscanf(GetParameter(paramName), "%d", &inum)) {
		return inum;
	}
	return 0;
}

const bool LG::GetParameterBool(const char* paramName) {
	return isTrue(GetParameter(paramName));
}

const float LG::GetParameterFloat(const char* paramName) {
	float fnum = 0;
	if (sscanf(GetParameter(paramName), "%f", &fnum)) {
		return fnum;
	}
	return 0.0;
}

const Ogre::ColourValue LG::GetParameterColor(const char* paramName) {
	float rnum;
	float gnum;
	float bnum;
	if (sscanf(GetParameter(paramName), "<%f,%f,%f>", &rnum, &gnum, &bnum)) {
		return Ogre::ColourValue(rnum, gnum, bnum);
	}
	return Ogre::ColourValue::Red;
}

// Print out a message of the pointer thing is null. At least the log will know
// of the problem
void LG::AssertNonNull(void* thing, const char* msg) {
	if (thing == NULL) {
		LG::Log(msg);
	}
}

// return 'true' if the passed text is either "true" or "yes". Return false otherwise.
const bool LG::isTrue(const char* txt) {
	if ((stricmp(txt, "true") == 0) || (stricmp(txt, "yes") == 0)) return true;
	return false;
}


// Call back into the managed world to output a log message with formatting
void LG::Log(const char* msg, ...) {
	char buff[2048];
	if (LG::debugLogCallback != NULL) {
		va_list args;
		va_start(args, msg);
		vsprintf(buff, msg, args);
		va_end(args);
		(*LG::debugLogCallback)(buff);
	}
}

// call out to the main program and make sure we should keep running
const bool LG::checkKeepRunning() {
	if (LG::checkKeepRunningCallback != NULL) {
		return (*LG::checkKeepRunningCallback)();
	}
	return false;
}

// Routine which calls back into the managed world to request a texture be loaded
void LG::RequestResource(const char* contextEntName, const char* paramName, const int type) {
	if (LG::requestResourceCallback != NULL) {
		(*LG::requestResourceCallback)(contextEntName, paramName, type);
	}
}


}