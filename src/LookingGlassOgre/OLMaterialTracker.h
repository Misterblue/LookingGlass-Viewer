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

// forward definition
namespace RendererOgre { class RendererOgre; }

namespace OLMaterialTracker {

class OLMaterialTracker : public Ogre::FrameListener {

public:
	OLMaterialTracker(RendererOgre::RendererOgre*);
	~OLMaterialTracker(void);

	// A material needs completing. MaterialManager::CreateOrRetrieve() has been
	// called but the material was created. We check to see if the material file
	// exists but most likely we will fill this instance with default and
	// request the material creator to make one for us.
	void FabricateMaterial(Ogre::String, Ogre::MaterialPtr);

	// Make a passed material into the default material
	void MakeMaterialDefault(Ogre::MaterialPtr);

	// called to get all the meshes that need to be reloaded for materials
	// just addes to the passed list
	void GetMeshesToRefreshForMaterials(std::list<Ogre::MeshPtr>*, const Ogre::String&);
	void GetMeshesToRefreshForTexture(std::list<Ogre::MeshPtr>*, const Ogre::String&, bool);
	void ReloadMeshes(std::list<Ogre::MeshPtr>*);

	// remember the material is modified and reload the using entities
	void MarkMaterialModified(const Ogre::String);

	// remember the texture is modified and reload the using entities
	void MarkTextureModified(const Ogre::String, bool);

	// Ogre::FrameListener
	bool frameRenderingQueued(const Ogre::FrameEvent&);

	// given some parameters, update an existing material with new definitions
	void CreateMaterialResource(const char*, const char*, 
		const float, const float, const float, const float,
		const float, const bool, const int, const int);

	// another version with parameters in an array
	void CreateMaterialResource2(const char*, const char*, const float[]);
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
		CreateMaterialTransparancy
	};

private:
	RendererOgre::RendererOgre* m_ro;
	Ogre::String m_defaultTextureName;
	Ogre::String m_cacheDir;
	Ogre::MaterialSerializer* m_serializer;
	bool m_shouldSerialize;
};

}