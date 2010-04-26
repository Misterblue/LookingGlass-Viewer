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
#include "SingletonInstance.h"
#include "LGLocking.h"

namespace LG {

class OLMaterialTracker : public Ogre::FrameListener, public SingletonInstance {

public:
	OLMaterialTracker();
	~OLMaterialTracker(void);

	static OLMaterialTracker* Instance() { 
		if (LG::OLMaterialTracker::m_instance == NULL) {
			LG::OLMaterialTracker::m_instance = new OLMaterialTracker();
		}
		return LG::OLMaterialTracker::m_instance; 
	}

	// SingletonInstance.Shutdown()
	void Shutdown();

	// A material needs completing. MaterialManager::CreateOrRetrieve() has been
	// called but the material was created. We check to see if the material file
	// exists but most likely we will fill this instance with default and
	// request the material creator to make one for us.
	void FabricateMaterial(Ogre::String, Ogre::MaterialPtr);

	// Make a passed material into the default material
	void MakeMaterialDefault(Ogre::MaterialPtr);

	// called to get all the meshes that need to be reloaded for materials
	// just addes to the passed list
	typedef std::map<Ogre::String, Ogre::MeshPtr> MeshPtrHashMap;
	void GetMeshesToRefreshForMaterials(MeshPtrHashMap*, const Ogre::String&);
	void GetMeshesToRefreshForTexture(MeshPtrHashMap*, const Ogre::String&, bool);
	void ReloadMeshes(MeshPtrHashMap*);

	// refresh the material of specified type
	void RefreshResource(const Ogre::String&, const int);

	// remember the material is modified and reload the using entities
	void MarkMaterialModified(const Ogre::String);

	// remember the texture is modified and reload the using entities
	void MarkTextureModified(const Ogre::String, bool);

	// Ogre::FrameListener
	bool frameEnded(const Ogre::FrameEvent&);

	// given some parameters, update an existing material with new definitions
	void CreateMaterialResource(const char*, const char*, 
		const float, const float, const float, const float,
		const float, const bool, const int, const int);

	// another version with parameters in an array
	void CreateMaterialResource2(const char*, const char*, const float[]);
	void CreateMaterialSetTransparancy(Ogre::Pass*, float);
	void CreateMaterialResource3(const char*, const char*, const float[]);
	void CreateMaterialDecorateTus(Ogre::TextureUnitState* tus, const float[]);
	// the order of the parameters in the CreateMaterialResource2 parameter array
	enum CreateMaterialParams {
		CreateMaterialColorR,
		CreateMaterialColorG,
		CreateMaterialColorB,
		CreateMaterialColorA,
		CreateMaterialGlow,
		CreateMaterialFullBright,
		CreateMaterialShiny,
		CreateMaterialBump,
		CreateMaterialScrollU,
		CreateMaterialScrollV,
		CreateMaterialScaleU,
		CreateMaterialScaleV,
		CreateMaterialRotate,
		CreateMaterialMappingType,
		CreateMaterialMediaFlags,
		CreateMaterialTransparancy,
		CreateMaterialAnimationFlag,
		CreateMaterialAnimSizeX,
		CreateMaterialAnimSizeY,
		CreateMaterialAnimStart,
		CreateMaterialAnimLength,
		CreateMaterialAnimRate,
		CreateMaterialSize
	};
#define CreateMaterialAnimFlagOff (0x00)
#define CreateMaterialAnimFlagOn (0x01)
#define CreateMaterialAnimFlagLoop (0x02)
#define CreateMaterialAnimFlagReverse (0x04)
#define CreateMaterialAnimFlagPingPong (0x08)
#define CreateMaterialAnimFlagSmooth (0x10)
#define CreateMaterialAnimFlagRotate (0x20)
#define CreateMaterialAnimFlagScale (0x40)

private:
	static OLMaterialTracker* m_instance;

	Ogre::String m_defaultTextureName;
	Ogre::String m_whiteTextureName;
	Ogre::String m_cacheDir;
	Ogre::MaterialSerializer* m_serializer;
	bool m_shouldSerialize;
	bool m_shouldUseShaders;

	typedef std::map<Ogre::String, unsigned long> RequestedMaterialHashMap;
	RequestedMaterialHashMap m_requestedMaterials;
	Ogre::Timer* m_materialTimeKeeper;

	// queue to hold the names of the materials that were reloaded. At FrameListener time,
	//   go through all the Entities and find the ones that contain this material. If found,
	//   reload the entity. This will cause the reapplication of the changed material.
	std::list<Ogre::String> m_materialsModified;
	std::list<Ogre::String> m_texturesModified;
	LGLOCK_MUTEX m_modifiedMutex;

	int m_slowCount;	// the number of frames until we check meshes

};

}