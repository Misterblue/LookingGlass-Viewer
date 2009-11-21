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
#include "VisCalcFrustDist.h"
#include "LookingGlassOgre.h"
#include "RendererOgre.h"
#include "OLMeshTracker.h"

namespace LG { 
	
VisCalcFrustDist::VisCalcFrustDist() {
}

VisCalcFrustDist::~VisCalcFrustDist() {
}

void VisCalcFrustDist::Initialize() {
	// visibility culling parameters
	m_shouldCullMeshes = LG::GetParameterBool("Renderer.Ogre.Visibility.Cull.Meshes");
	m_shouldCullTextures = LG::GetParameterBool("Renderer.Ogre.Visibility.Cull.Textures");
	m_shouldCullByFrustrum = LG::GetParameterBool("Renderer.Ogre.Visibility.Cull.Frustrum");
	m_shouldCullByDistance = LG::GetParameterBool("Renderer.Ogre.Visibility.Cull.Distance");
	m_visibilityScaleMaxDistance = LG::GetParameterFloat("Renderer.Ogre.Visibility.MaxDistance");
	m_visibilityScaleMinDistance = LG::GetParameterFloat("Renderer.Ogre.Visibility.MinDistance");
	m_visibilityScaleOnlyLargeAfter = LG::GetParameterFloat("Renderer.Ogre.Visibility.OnlyLargeAfter");
	m_visibilityScaleLargeSize = LG::GetParameterFloat("Renderer.Ogre.Visibility.Large");
	LG::Log("initialize: visibility: min=%f, max=%f, large=%f, largeafter=%f",
			(double)m_visibilityScaleMinDistance, (double)m_visibilityScaleMaxDistance, 
			(double)m_visibilityScaleLargeSize, (double)m_visibilityScaleOnlyLargeAfter);
	LG::Log("VisCalcFrustDist::Initialize: visibility: cull meshes/textures/frustrum/distance = %s/%s/%s/%s",
					m_shouldCullMeshes ? "true" : "false",
					m_shouldCullTextures ? "true" : "false",
					m_shouldCullByFrustrum ? "true" : "false",
					m_shouldCullByDistance ? "true" : "false"
	);
	m_meshesReloadedPerFrame = LG::GetParameterInt("Renderer.Ogre.Visibility.MeshesReloadedPerFrame");
	return;
}

void VisCalcFrustDist::Start() {
	LG::GetOgreRoot()->addFrameListener(this);
	return;
}

void VisCalcFrustDist::Stop() {
	return;
}

void VisCalcFrustDist::RecalculateVisibility() {
	m_recalculateVisibility = true;
}

// we're between frames, on our own thread so we can do the work without locking
bool VisCalcFrustDist::frameEnded(const Ogre::FrameEvent& evt) {
	if (m_recalculateVisibility) {
		calculateEntityVisibility();
	}
	processEntityVisibility();
	return true;
}

// Once a frame, go though all the meshes and figure out which ones are visible.
// we unload the non-visible ones and make sure the visible ones are loaded.
// This keeps the number of in memory vertexes low
// BETWEEN FRAME OPERATION
int visSlowdown;
int visRegions;
int visChildren;
int visEntities;
int visNodes;
int visVisToInvis;
int visVisToVis;
int visInvisToVis;
int visInvisToInvis;
void VisCalcFrustDist::calculateEntityVisibility() {
	if ((!m_recalculateVisibility) || ((!m_shouldCullByDistance) && (!m_shouldCullByFrustrum))) return;
	m_recalculateVisibility = false;
	visRegions = visChildren = visEntities = visNodes = 0;
	visVisToVis = visVisToInvis = visInvisToVis = visInvisToInvis = 0;
	Ogre::SceneNode* nodeRoot = LG::RendererOgre::Instance()->m_sceneMgr->getRootSceneNode();
	if (nodeRoot == NULL) return;
	// Hanging off the root node will be a node for each 'region'. A region has
	// terrain and then content nodes
	Ogre::SceneNode::ChildNodeIterator rootChildIterator = nodeRoot->getChildIterator();
	while (rootChildIterator.hasMoreElements()) {
		visRegions++;
		Ogre::Node* nodeRegion = rootChildIterator.getNext();
		// a region node has the nodes of its contents.
		calculateEntityVisibility(nodeRegion);
	}
	if ((visSlowdown-- < 0) || (visVisToInvis != 0) || (visInvisToVis != 0)) {
		visSlowdown = 30;
		LG::Log("calcVisibility: regions=%d, nodes=%d, entities=%d, children=%d",
				visRegions, visNodes, visEntities, visChildren);
		LG::Log("calcVisibility: vv=%d, vi=%d, iv=%d, ii=%d",
				visVisToVis, visVisToInvis, visInvisToVis, visInvisToInvis);
	}
	LG::SetStat(LG::StatVisibleToVisible, visVisToVis);
	LG::SetStat(LG::StatVisibleToInvisible, visVisToInvis);
	LG::SetStat(LG::StatInvisibleToVisible, visInvisToVis);
	LG::SetStat(LG::StatInvisibleToInvisible, visInvisToInvis);
}

// BETWEEN FRAME OPERATION
void VisCalcFrustDist::calculateEntityVisibility(Ogre::Node* node) {
	if (node->numChildren() > 0) {
		// if node has more children nodes, visit them recursivily
		Ogre::SceneNode::ChildNodeIterator nodeChildIterator = node->getChildIterator();
		while (nodeChildIterator.hasMoreElements()) {
			Ogre::Node* nodeChild = nodeChildIterator.getNext();
			calculateEntityVisibility(nodeChild);
			visChildren++;
		}
	}
	visNodes++;
	// children taken care of... check fo attached objects to this node
	Ogre::SceneNode* snode = (Ogre::SceneNode*)node;
	float snodeDistance = LG::RendererOgre::Instance()->m_camera->getPosition().distance(snode->_getWorldAABB().getCenter());
	Ogre::SceneNode::ObjectIterator snodeObjectIterator = snode->getAttachedObjectIterator();
	while (snodeObjectIterator.hasMoreElements()) {
		Ogre::MovableObject* snodeObject = snodeObjectIterator.getNext();
		if (snodeObject->getMovableType() == "Entity") {
			visEntities++;
			Ogre::Entity* snodeEntity = (Ogre::Entity*)snodeObject;
			// check it's visibility if it's not world geometry (terrain and ocean)
			if ((snodeEntity->getQueryFlags() & Ogre::SceneManager::WORLD_GEOMETRY_TYPE_MASK) == 0) {
				// computation if it should be visible
				// Note: this call is overridden by derived classes that do fancier visibility rules
				bool viz = this->CalculateVisibilityImpl(LG::RendererOgre::Instance()->m_camera, snodeEntity, snodeDistance);
				if (snodeEntity->isVisible()) {
					// we currently think this object is visible. make sure it should stay that way
					if (viz) {
						// it should stay visible
						visVisToVis++;
					}
					else {
						// not visible any more... make invisible nad unload it
						snodeEntity->setVisible(false);
						visVisToInvis++;
						if (!snodeEntity->getMesh().isNull()) {
							queueMeshUnload(snodeEntity->getMesh());
						}
					}
				}
				else {
					// the entity currently thinks it's not visible.
					// check to see if it should be visible by checking a fake bounding box
					if (viz) {
						// it should become visible again
						if (!snodeEntity->getMesh().isNull()) {
							queueMeshLoad(snodeEntity, snodeEntity->getMesh());
						}
						// snodeEntity->setVisible(true);	// must happen after mesh loaded
						visInvisToVis++;
					}
					else {
						visInvisToInvis++;
					}
				}
			}
		}
	}
}

// Overloadable function that asks if, given this camera and entity, is the entity visible
// Allows this class to be subclassed and just extend this one visibility calcuation
bool VisCalcFrustDist::CalculateVisibilityImpl(Ogre::Camera* cam, Ogre::Entity* ent, float entDistance) {
	float snodeEntitySize = ent->getBoundingRadius() * 2;
	bool frust = m_shouldCullByFrustrum && cam->isVisible(ent->getWorldBoundingBox());
	bool dist = m_shouldCullByDistance && calculateScaleVisibility(entDistance, snodeEntitySize);
	bool viz = (!m_shouldCullByFrustrum && !m_shouldCullByDistance)	// don't cull
		|| (!m_shouldCullByDistance && frust)	// if frust cull only, it must be true
		|| (!m_shouldCullByFrustrum && dist)	// if dist cull only, it must be true
		|| (dist && frust);						// if cull both ways, both must be true
	return viz;
}

// Return TRUE if an object of this size should be seen at this distance
bool VisCalcFrustDist::calculateScaleVisibility(float dist, float siz) {
	// LG::Log("calculateScaleVisibility: dist=%f, siz=%f", dist, siz);
	// if it's farther than max, don't display
	if (dist >= m_visibilityScaleMaxDistance) return false;
	// if it's closer than min, display it
	if (dist <= m_visibilityScaleMinDistance) return true;
	// if it is large enough for within large thing bound, display it
	if (siz >= m_visibilityScaleLargeSize && dist > m_visibilityScaleOnlyLargeAfter) return true;
	// if it's size scales to big enough within scalemin and scalemax, display it
	if (siz > ((dist - m_visibilityScaleMinDistance)
				/(m_visibilityScaleMaxDistance - m_visibilityScaleMinDistance) 
				* m_visibilityScaleLargeSize)) return true;
	// not reason found to display it so don't
	return false;

}

// BETWEEN FRAME OPERATION
stdext::hash_map<Ogre::String, Ogre::Entity*> meshesToLoad;
stdext::hash_map<Ogre::String, Ogre::Entity*> meshesToUnload;
void VisCalcFrustDist::processEntityVisibility() {
	int cnt = m_meshesReloadedPerFrame;
	stdext::hash_map<Ogre::String, Ogre::Entity*>::iterator intr;
	while ((!meshesToLoad.empty()) && (cnt-- > 0)) {
		intr = meshesToLoad.begin();
		Ogre::String meshName = intr->first;
		Ogre::Entity* parentEntity = intr->second;
		meshesToLoad.erase(intr);
		Ogre::MeshPtr meshP = Ogre::MeshManager::getSingleton().getByName(meshName);
		if (!meshP.isNull()) {
			if (m_shouldCullMeshes) meshP->load();
			parentEntity->setVisible(true);
			LG::IncStat(LG::StatCullMeshesLoaded);
		}
	}
	LG::SetStat(LG::StatCullMeshesQueuedToLoad, meshesToLoad.size());
	return;
}

void VisCalcFrustDist::queueMeshLoad(Ogre::Entity* parentEntity, Ogre::MeshPtr meshP) {
	// remove from the unload list if scheduled to do that
	Ogre::String meshName = meshP->getName();
	// if it's in the list to unload and we're supposed to load it, remove from unload list
	if (meshesToUnload.find(meshName) != meshesToUnload.end()) {
		meshesToUnload.erase(meshName);
	}
	// add to the load list if not already there (that camera can move around)
	meshesToLoad.insert(std::pair<Ogre::String, Ogre::Entity*>(meshName, parentEntity));
	LG::IncStat(LG::StatCullMeshesQueuedToLoad);
}

// BETWEEN FRAME OPERATION
void VisCalcFrustDist::queueMeshUnload(Ogre::MeshPtr meshP) {
	Ogre::String meshName = meshP->getName();
	// if it's in the list to be loaded but we're unloading it, remove from load list
	if (meshesToLoad.find(meshName) != meshesToLoad.end()) {
		meshesToLoad.erase(meshName);
	}
	// for the moment, just unload it and don't queue
	unloadTheMesh(meshP);
}

// unload all about this mesh. The mesh itself and the textures.
// BETWEEN FRAME OPERATION
void VisCalcFrustDist::unloadTheMesh(Ogre::MeshPtr meshP) {
	if (m_shouldCullTextures) {
		Ogre::Mesh::SubMeshIterator smi = meshP->getSubMeshIterator();
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
							Ogre::String texName = oneTus->getTextureName();
							// TODO: the same texture gets unloaded multiple times. Is that a bad thing?
							Ogre::TextureManager::getSingleton().unload(texName);
							LG::IncStat(LG::StatCullTexturesUnloaded);
							// LG::Log("unloadTheMesh: unloading texture %s", texName.c_str());
						}
					}
				}
			}
		}
	}
	if (m_shouldCullMeshes) {
		LG::OLMeshTracker::Instance()->MakeUnLoaded(meshP->getName(), NULL, NULL);
		LG::IncStat(LG::StatCullMeshesUnloaded);
		// LG::Log("unloadTheMesh: unloading mesh %s", mshName.c_str());
	}
}

}
