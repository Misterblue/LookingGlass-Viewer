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

#define DLLExport __declspec( dllexport )

namespace LookingGlassOgr {

typedef void DebugLogCallback(const char*);
typedef const char* FetchParameterCallback(const char*);
typedef const bool CheckKeepRunningCallback();
typedef void UserIOCallback(int, int, float, float);
typedef void RequestResourceCallback(const char*, const char*, int);
typedef bool BetweenFramesCallback();

extern DebugLogCallback* debugLogCallback;
extern FetchParameterCallback* fetchParameterCallback;
extern CheckKeepRunningCallback* checkKeepRunningCallback;
extern UserIOCallback* userIOCallback;
extern RequestResourceCallback* requestResourceCallback;
extern BetweenFramesCallback* betweenFramesCallback;

static const int ResourceTypeUnknown = 0;	//
static const int ResourceTypeMesh = 1;		// Mesh
static const int ResourceTypeTexture = 2;	// Texture
static const int ResourceTypeMaterial = 3;	// Material
static const int ResourceTypeTransparentTexture = 4;	// A texture with some transparancy

#define OLArchiveTypeName "OLFileSystem"
#define OLPreloadTypeName "OLPreloadFileSystem"
#define OLResourceGroupName "OLResource"

// #define OLMeshResourceGroupName "OLMeshResource"
// #define	OLTextureResourceGroupName "OLTextureResource"
// #define	OLMaterialResourceGroupName "OLMaterialResource"
// #define OLMaterialTypeName "Material"

// Utility functions
extern void AssertNonNull(void*, const char*);
extern const bool isTrue(const char*);
extern void Log(const char*, ...);
extern const char* GetParameter(const char*);
extern const int GetParameterInt(const char*);
extern const bool GetParameterBool(const char*);
extern const float GetParameterFloat(const char*);
extern const Ogre::ColourValue GetParameterColor(const char*);
extern const bool checkKeepRunning();
// Request the loading of a resource (call to network)
extern void RequestResource(const char*, const char*, const int);
// When a resource is updated, tell Ogre to redisplay it
extern void RefreshResourceI(const Ogre::String&, const int);

extern Ogre::Root* GetOgreRoot();


class LookingGlassOgre {

public:
	LookingGlassOgre();
	~LookingGlassOgre();

};

}