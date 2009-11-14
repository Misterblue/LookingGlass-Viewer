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
#include "OLMaterialTracker.h"
#include "LookingGlassOgre.h"
#include "RendererOgre.h"
#include "BadImageCodec.h"

// Materials go through several states:
//  unknown: no definition
//  waiting to load: waiting for a between from slot to be loaded
//  waiting for reload: waiting for a between frame slot to be reloaded (refreshed)
//  loaded:
//  waiting for unload: waiting for a between frame slot to unload and unmap
//  unloaded: much the same as 'unknown'

// there are several operations on materials:
//   load: load/build the material
//   refresh: reload the material so it is refreshed on the screen
//   unload for culling: unload but we will want it back soon
//   unload: unload and remove all pointers

// We build a state machine for materials from these two sets:
//          unknown	wload	wreload	loaded	wunload	unloaded
//   load	A		B		B		B		D		A
// refresh	A		B		B		C		D		A
// unloadc  E		F		F		D		E		E
// unload	E		G		G		H		E		E
// loaddone	I		I		I		E		X		I
// unloaddn	J		X		X		J		J		E
// A: queue load request in between frame queue. =>wload
// B: ignore this request since it's already in the queue.
// C: queue refresh request on material. =>wreload
// D: queue an unload request =>wunload
// E; ignore request 
// F: remove waiting load request and schedule unload. =>wunload
// G: remove waiting load request and schedule complete unload request. =>wunload
// H: queue a complete unload request. =>wunload
// I: =>loaded
// J: =>unloaded
// X: should never get here

namespace OLMaterialTracker {

// queue to hold the names of the materials that were reloaded. At FrameListener time,
//   go through all the Entities and find the ones that contain this material. If found,
//   reload the entity. This will cause the reapplication of the changed material.
std::queue<Ogre::String> m_materialsModified;
std::queue<Ogre::String> m_texturesModified;

OLMaterialTracker::OLMaterialTracker(RendererOgre::RendererOgre* ro) {
	m_defaultTextureName = LookingGlassOgr::GetParameter("Renderer.Ogre.DefaultTextureResourceName");
	m_cacheDir = LookingGlassOgr::GetParameter("Renderer.Ogre.CacheDir");
	m_shouldSerialize = LookingGlassOgr::isTrue(LookingGlassOgr::GetParameter("Renderer.Ogre.SerializeMaterials"));

	m_ro = ro;
	LookingGlassOgr::GetOgreRoot()->addFrameListener(this);
	if (m_shouldSerialize) {
		m_serializer = new Ogre::MaterialSerializer();
	}

	// more kludge processing for corrupt data files
	Ogre::ImageCodec* codec = OGRE_NEW BadImageCodec::BadImageCodec(m_ro);
	Ogre::Codec::registerCodec(codec);
}

OLMaterialTracker::~OLMaterialTracker() {
}

// We have been passed a material that needs to be filled in. We try to read it in from
// the file. If the file is not there, we fill it with the default texture and request
// the loading of the real material. When that real material is loaded, someone will call
// RefreshMaterialUsers.
// History Note: Origionally all materials were serialized to files. This created thousands
// of files which was not practical. The second try doesn't store the material info at all
// but rebuilds it from the prim information. This works for LLLP. The  third try will
// use a DB to store the material information so it can be recreated.
// We check to see if the material file exists which it never will.
void OLMaterialTracker::FabricateMaterial(Ogre::String name, Ogre::MaterialPtr matPtr) {
	// Try to get the stream to load the material from.
	Ogre::DataStreamPtr stream;
	stream.setNull();
	if (m_shouldSerialize) {
		// if we aren't serializeing, don't expect to find the material either
		stream = Ogre::ResourceGroupManager::getSingleton().openResource(name, 
					OLResourceGroupName, true, matPtr.getPointer());
	}
	if (stream.isNull()) {
		// if the underlying material doesn't exist, return the default material
		// and request the real material be constructed
		MakeMaterialDefault(matPtr);
		Ogre::Material* mat = matPtr.getPointer();
		LookingGlassOgr::RequestResource(name.c_str(), name.c_str(), LookingGlassOgr::ResourceTypeMaterial);
	}
	else {
		// There is a material file under there somewhere, read the thing in
		try {
			Ogre::MaterialManager::getSingleton().parseScript(stream, Ogre::String(OLResourceGroupName));
			Ogre::MaterialPtr matPtr = Ogre::MaterialManager::getSingleton().getByName(name);
			if (!matPtr.isNull()) {
				// is this necessary to do here? Someday try it without
				matPtr->compile();
				matPtr->load();
				// this 'unload' seems to cause problems
				// matPtr->unload();
				// LookingGlassOgr::Log("ResourceListeners::processMaterialName: material loaded: %s", stream->getName().c_str());
			}
		}
		catch (Ogre::Exception& e) {
			LookingGlassOgr::Log("OLMeshSerializerListener::processMaterialName: error creating material %s: %s", 
				name.c_str(), e.getDescription().c_str());
		}
		stream->close();
		OGRE_DELETE stream.getPointer();
	}
	return;
}
// Clean out the passed material and build up a new one that is just the default
void OLMaterialTracker::MakeMaterialDefault(Ogre::MaterialPtr matPtr) {
	Ogre::Material* mat = matPtr.getPointer();
	mat->unload();
	mat->removeAllTechniques();
	Ogre::Technique* tech = mat->createTechnique();
	Ogre::Pass* pass = tech->createPass();
	pass->setShininess(0.0f);
	pass->setAmbient(0.1, 0.1, 0.1);
	// pass->setVertexColourTracking(Ogre::TVC_AMBIENT);
	pass->setDiffuse(0.582f, 0.5703f, 0.7578f, 0.7f); // blue gray from girl's shirt
	pass->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
	// pass->createTextureUnitState(m_defaultTextureName);	// we need a resolvable texture filename

	mat->load();
}

// Internal request to refresh a resource
void OLMaterialTracker::RefreshResource(const Ogre::String& resName, const int rType) {
	if (rType == LookingGlassOgr::ResourceTypeMesh) {
		Ogre::MeshPtr theMesh = (Ogre::MeshPtr)Ogre::MeshManager::getSingleton().getByName(resName);
		// unload it and let the renderer decide if it needs to be loaded again
		// NOTE: unload doesn't work here. We get exceptions if we unload while reload doesn't fail
		if (!theMesh.isNull()) theMesh->reload();
	}
	if (rType == LookingGlassOgr::ResourceTypeMaterial) {
		// mark it so the work happens later between frames (more queues to manage correctly someday)
		MarkMaterialModified(resName);
	}
	if (rType == LookingGlassOgr::ResourceTypeTexture) {
		Ogre::TextureManager::getSingleton().unload(resName);
		MarkTextureModified(resName, false);
	}
	if (rType == LookingGlassOgr::ResourceTypeTransparentTexture) {
		Ogre::TextureManager::getSingleton().unload(resName);
		MarkTextureModified(resName, true);
	}
}

// A material has been modified. Remember it's name and, between frames, reload the
// entities that contain the material.
void OLMaterialTracker::MarkMaterialModified(const Ogre::String materialName) {
	m_materialsModified.push(materialName);
}

// A texture has been modified. Remember it's name and, between frames, reload the
// entities that contain the material.
// The problem is that changing a
// material and reloading it does not cause Ogre to reload the entities that use the
// material. Therefore, it looks like you change the material but the thing on the
// screen does not change.
// This routine walks the entity list and reloads all the entities that use the
// named material
// Note that this does not reload the material itself. This presumes you already did
// that and you now just need Ogre to get with the program.
void OLMaterialTracker::MarkTextureModified(const Ogre::String materialName, bool hasTransparancy) {
	Ogre::String taggedName = (hasTransparancy ? "T" : " ") + materialName;
	m_texturesModified.push(taggedName);
}

// between frames, if there were material modified, refresh their containing entities
bool OLMaterialTracker::frameEnded(const Ogre::FrameEvent&) {
	Ogre::String matName;
	std::list<Ogre::MeshPtr> m_meshesToChange;
	int cnt = 10;
	while ((m_materialsModified.size() > 0) && (--cnt > 0)) {
		matName = m_materialsModified.front();
		m_materialsModified.pop();
		GetMeshesToRefreshForMaterials(&m_meshesToChange, matName);
	}
	cnt = 10;
	while ((m_texturesModified.size() > 0) && (--cnt > 0)) {
		matName = m_texturesModified.front();
		m_texturesModified.pop();
		char transparancyFlag = matName[0];
		GetMeshesToRefreshForTexture(&m_meshesToChange, matName.substr(1, matName.length()-1),
				(transparancyFlag == 'T' ? true : false));
	}
	if (m_meshesToChange.size() > 0) {
		ReloadMeshes(&m_meshesToChange);
	}
	m_meshesToChange.clear();
	return true;
}



// find all the meshes that use the material given and add it to a list of meshes
void OLMaterialTracker::GetMeshesToRefreshForMaterials(std::list<Ogre::MeshPtr>* meshes, const Ogre::String& matName) {
	// only check the Meshs for use of this material
	Ogre::ResourceManager::ResourceMapIterator rmi = Ogre::MeshManager::getSingleton().getResourceIterator();
	while (rmi.hasMoreElements()) {
		Ogre::MeshPtr oneMesh = rmi.getNext();
		Ogre::Mesh::SubMeshIterator smi = oneMesh->getSubMeshIterator();
		while (smi.hasMoreElements()) {
			Ogre::SubMesh* oneSubMesh = smi.getNext();
			if (oneSubMesh->getMaterialName() == matName) {
				// this mesh uses the material
				// we sometimes get multiple materials for one mesh -- just reload once
				std::list<Ogre::MeshPtr>::iterator ii = meshes->begin(); 
				while (ii != meshes->end()) {
					if (ii->getPointer()->getName() == oneMesh->getName()) {
						break;
					}
					ii++;
				}
				if (ii == meshes->end()) {
					meshes->push_front(oneMesh);
				}
				break;
			}
		}
	}
}

// find all the meshes that use the texture and add it to a list of meshes
// TODO: figure out of just reloading the resource group is faster
void OLMaterialTracker::GetMeshesToRefreshForTexture(std::list<Ogre::MeshPtr>* meshes, const Ogre::String& texName,
													 bool hasTransparancy) {
	// only check the Meshs for use of this material
	LookingGlassOgr::Log("GetMeshesToRefreshForTexture: refresh for %s", texName.c_str());
	Ogre::ResourceManager::ResourceMapIterator rmi = Ogre::MeshManager::getSingleton().getResourceIterator();
	while (rmi.hasMoreElements()) {
		Ogre::MeshPtr oneMesh = rmi.getNext();
		Ogre::Mesh::SubMeshIterator smi = oneMesh->getSubMeshIterator();
		while (smi.hasMoreElements()) {
			Ogre::SubMesh* oneSubMesh = smi.getNext();
			Ogre::String subMeshMaterialName = oneSubMesh->getMaterialName();
			Ogre::MaterialPtr subMeshMaterial = (Ogre::MaterialPtr)Ogre::MaterialManager::getSingleton().getByName(subMeshMaterialName);
			if (!subMeshMaterial.isNull()) {
				Ogre::Material::TechniqueIterator techIter = subMeshMaterial->getTechniqueIterator();
				while (techIter.hasMoreElements()) {
					Ogre::Technique* oneTech = techIter.getNext();
					Ogre::Technique::PassIterator passIter = oneTech->getPassIterator();
					while (passIter.hasMoreElements()) {
						Ogre::Pass* onePass = passIter.getNext();
						Ogre::Pass::TextureUnitStateIterator tusIter = onePass->getTextureUnitStateIterator();
						while (tusIter.hasMoreElements()) {
							Ogre::TextureUnitState* oneTus = tusIter.getNext();
							if (oneTus->getTextureName() == texName) {
								// we have the material pass with this texture. Update transparancy flag while here
								if (hasTransparancy) {
									onePass->setDepthWriteEnabled(false);
									onePass->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
								}
								else {
									onePass->setDepthWriteEnabled(true);
									onePass->setSceneBlending(Ogre::SBT_REPLACE);
								}
								// this mesh uses the material
								// we sometimes get multiple materials for one mesh -- just reload once
								std::list<Ogre::MeshPtr>::iterator ii = meshes->begin(); 
								while (ii != meshes->end()) {
									if (ii->getPointer()->getName() == oneMesh->getName()) {
										break;
									}
									ii++;
								}
								if (ii == meshes->end()) {
									meshes->push_front(oneMesh);
								}
								break;
							}
						}
					}
				}
			}
		}
	}
}

// given a list of meshes, reload them
// Does not modify the list passed
void OLMaterialTracker::ReloadMeshes(std::list<Ogre::MeshPtr>* meshes) {
	if (!meshes->empty()) {
		for (std::list<Ogre::MeshPtr>::iterator ii = meshes->begin(); ii != meshes->end(); ii++) {
			ii->getPointer()->reload();
		}
	}
}

// Passed the information to put in a material, find the material and make it
// follow this definition.
// OBSOLETE: DO NOT USE. PROBABLY DOESN"T WORK ANY MORE ANYWAY.
void OLMaterialTracker::CreateMaterialResource(const char* mName, const char* tName,
		const float colorR, const float colorG, const float colorB, const float colorA,
		const float glow, const bool fullBright, const int shiny, const int bump) {
/*
	Ogre::String materialName = mName;
	Ogre::String textureName = tName;
	// get rid of the old one
	Ogre::MaterialManager::getSingleton().remove(materialName);
	// create a new instance
	Ogre::MaterialManager::ResourceCreateOrRetrieveResult crResult =
			Ogre::MaterialManager::getSingleton().createOrRetrieve(materialName, OLResourceGroupName);
	Ogre::MaterialPtr matPtr = crResult.first;
	Ogre::Material* mat = matPtr.getPointer();

	mat->unload();
	mat->removeAllTechniques();
	Ogre::Technique* tech = mat->createTechnique();
	Ogre::Pass* pass = tech->createPass();
	pass->setLightingEnabled(true);
	pass->setShininess(((float)shiny)/256.0f);	// origionally a byte
	pass->setAmbient(0.05f, 0.05f, 0.05f);
	pass->setVertexColourTracking(Ogre::TVC_AMBIENT);
	if (textureName.length() > 0) {
		Ogre::TextureUnitState* tus = pass->createTextureUnitState(textureName);
		// TODO: somehow check to see if texture has transparency in it
		pass->setDepthWriteEnabled(false);
		pass->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
		tus->setColourOperationEx(Ogre::LBX_MODULATE, Ogre::LBS_TEXTURE, Ogre::LBS_MANUAL, 
			Ogre::ColourValue(colorR, colorG, colorB, colorA));
	}
	else {
		pass->setDiffuse(colorR, colorG, colorB, colorA);// this isn't right. color is a base color and not a lighting effect
		if (colorA == 1.0) {
			pass->setSceneBlending(Ogre::SBT_REPLACE);
		}
		else {
			pass->setDepthWriteEnabled(false);
			pass->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
		}
	}

	// see "Historical Note" on FabricateMaterial
	// because of the problems of thousands of material files, serialization is optional
	if (m_shouldSerialize) {
		Ogre::String filename = m_ro->EntityNameToFilename(materialName, "");
		m_ro->CreateParentDirectory(filename);
		m_serializer->exportMaterial(matPtr, filename);
	}
	// we're getting errors when this load happens if the textures don't already exist
	// mat->load();
*/
}

void OLMaterialTracker::CreateMaterialResource2(const char* mName, const char* tName, const float* parms) {
	Ogre::String materialName = mName;
	Ogre::String textureName = tName;
	// if (Ogre::MaterialManager::getSingleton().resourceExists(materialName))
	Ogre::MaterialManager::ResourceCreateOrRetrieveResult crResult =
			Ogre::MaterialManager::getSingleton().createOrRetrieve(materialName, OLResourceGroupName);
	Ogre::MaterialPtr matPtr = crResult.first;
	Ogre::Material* mat = matPtr.getPointer();

	mat->unload();
	mat->removeAllTechniques();
	mat->setReceiveShadows(true);
	Ogre::Technique* tech = mat->createTechnique();
	Ogre::Pass* pass = tech->createPass();
	pass->setLightingEnabled(true);
	pass->setShininess(parms[CreateMaterialShiny]/256.0);	// origionally a byte
	pass->setAmbient(m_ro->MaterialAmbientColor);
	pass->setVertexColourTracking(Ogre::TVC_AMBIENT);
	if (textureName.length() > 0) {
		Ogre::TextureUnitState* tus = pass->createTextureUnitState(textureName);

		// use SceneBlendType to add the alpha information
		if (parms[CreateMaterialTransparancy] == 1.0) {
			pass->setSceneBlending(Ogre::SBT_REPLACE);
		}
		else {
			// next 2 are what most of the forum entries suggest
			// pass->setDepthWriteEnabled(false);
			// pass->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
			// next 4 lines found in http://www.ogre3d.org/wiki/index.php/Creating_transparency_based_on_a_key_colour_in_code
			pass->setSceneBlending(Ogre::SBT_REPLACE);
			pass->setAlphaRejectSettings(Ogre::CMPF_GREATER_EQUAL, 160);
			pass->setCullingMode(Ogre::CULL_NONE);
			pass->setManualCullingMode(Ogre::MANUAL_CULL_NONE);
			mat->setTransparencyCastsShadows(true);

		}
		tus->setTextureUScroll(parms[CreateMaterialScrollU]);
		tus->setTextureVScroll(parms[CreateMaterialScrollV]);
		tus->setTextureUScale(parms[CreateMaterialScaleU]);
		tus->setTextureVScale(parms[CreateMaterialScaleV]);
		tus->setTextureRotate(Ogre::Radian(parms[CreateMaterialRotate]));
		//1 trying out different forms of adding the color to the texture
		//1 tus->setColourOperationEx(Ogre::LBX_MODULATE, Ogre::LBS_TEXTURE, Ogre::LBS_MANUAL, 
		//1 	Ogre::ColourValue( parms[CreateMaterialColorR], parms[CreateMaterialColorG], 
		//1 	parms[CreateMaterialColorB], parms[CreateMaterialColorA] ));
		pass->setDiffuse(parms[CreateMaterialColorR], 
				parms[CreateMaterialColorG], 
				parms[CreateMaterialColorB], 
				parms[CreateMaterialColorA]
		);
	}
	else {
		/*
		// this code makes the prim a solid color that does not respect light shading
		Ogre::TextureUnitState* tus = pass->createTextureUnitState(textureName);
		tus->setColourOperationEx(Ogre::LBX_SOURCE1, Ogre::LBS_MANUAL, Ogre::LBS_CURRENT,
				Ogre::ColourValue(
				parms[CreateMaterialColorR], parms[CreateMaterialColorG], parms[CreateMaterialColorB]));
		tus->setAlphaOperation(Ogre::LBX_SOURCE1, Ogre::LBS_MANUAL, Ogre::LBS_CURRENT,
				parms[CreateMaterialColorA]);
		*/

		// it's a solid color. Just use that.
		pass->setDiffuse(parms[CreateMaterialColorR], 
				parms[CreateMaterialColorG], 
				parms[CreateMaterialColorB], 
				parms[CreateMaterialColorA]
		);
		if (parms[CreateMaterialColorA] == 1.0) {
			pass->setSceneBlending(Ogre::SBT_REPLACE);
		}
		else {
			pass->setDepthWriteEnabled(false);
			pass->setSceneBlending(Ogre::SBT_TRANSPARENT_ALPHA);
			mat->setTransparencyCastsShadows(true);
		}
	}

	// see "Historical Note" on FabricateMaterial
	// because of the problems of thousands of material files, serialization is optional
	if (m_shouldSerialize) {
		Ogre::String filename = m_ro->EntityNameToFilename(materialName, "");
		m_ro->CreateParentDirectory(filename);
		m_serializer->exportMaterial(matPtr, filename);
	}
	// We're getting errors when this load happens if the textures don't already exist
	// and things seem to work without it
	// mat->load();
}

}