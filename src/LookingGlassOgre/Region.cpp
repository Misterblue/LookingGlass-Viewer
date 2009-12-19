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
#include "Region.h"
#include "RendererOgre.h"

namespace LG {

Region::Region() {
		this->CurrentBaseSceneNode = 0;
		this->TerrainSceneNode = 0;
		this->OceanSceneNode = 0;
		this->m_highRezSceneNode = 0;
		this->m_medRezSceneNode = 0;
		this->m_lowRezSceneNode = 0;
		this->m_veryLowRezSceneNode = 0;
		this->m_focusRegion = false;
		this->OceanHeight = 0.0;
}

Region::~Region() {
}

void Region::ReleaseRegion() {
}
void Region::ChangeRez(RegionRezCode newRez) {
	if (newRez != this->CurrentRez) {
		switch (newRez) {
			case RegionRezCodeHigh: {
				DisconnectOldRezAndConnectNew(newRez, m_highRezSceneNode);
			}
			case RegionRezCodeMed: {
				if (this->m_medRezSceneNode == NULL) {
					// create medium scene node
				}
				DisconnectOldRezAndConnectNew(newRez, m_medRezSceneNode);
			}
			case RegionRezCodeLow: {
				if (this->m_lowRezSceneNode == NULL) {
					// create low rez scene node
				}
				DisconnectOldRezAndConnectNew(newRez, m_lowRezSceneNode);
		   }
			case RegionRezCodeVeryLow: {
				if (this->m_veryLowRezSceneNode == NULL) {
					// create very low rez scene node
				}
				DisconnectOldRezAndConnectNew(newRez, m_veryLowRezSceneNode);
			}
		}
	}
}

void Region::DisconnectOldRezAndConnectNew(RegionRezCode newRez, Ogre::SceneNode* newBase) {
	// disconnect the old base node from the scene graph
	// connect new base node
	// point to our new state
	this->CurrentBaseSceneNode = newBase;
	this->CurrentRez = newRez;
	return;
}

void Region::AddRegionSceneNode(Ogre::SceneNode* nod, RegionRezCode rez) {
	switch(rez) {
		case RegionRezCodeHigh: {
			this->m_highRezSceneNode = nod;
		}
		case RegionRezCodeMed: {
			this->m_medRezSceneNode = nod;
		}
		case RegionRezCodeLow: {
			this->m_lowRezSceneNode = nod;
	   }
		case RegionRezCodeVeryLow: {
			this->m_veryLowRezSceneNode = nod;
		}
	}
}

void Region::SetFocusRegion() {
}

bool Region::IsFocusRegion() {
	return this->m_focusRegion;
}

void Region::CalculateLocal(double X, double Y, double Z) {
	this->LocalX = (float)(this->GlobalX - X);
	this->LocalY = (float)(this->GlobalY - Y);
	this->LocalZ = (float)(this->GlobalZ - Z);
	return;
}

void Region::Init( double globalX, double globalY, double globalZ, float sizeX, float sizeY, float waterHeight) {
	LG::Log("Region::Init: r=%s, <%lf,%lf,%lf>", this->Name.c_str(), globalX, globalY, globalZ);
	this->GlobalX = globalX;
	this->GlobalY = globalY;
	this->GlobalZ = globalZ;
	this->LocalX = (float)globalX;
	this->LocalY = (float)globalY;
	this->LocalZ = (float)globalZ;
	// create scene Node
	Ogre::Quaternion orient = Ogre::Quaternion(Ogre::Radian(-3.14159265/2.0), Ogre::Vector3(1.0, 0.0, 0.0));
	Ogre::SceneNode* regionNode = LG::RendererOgre::Instance()->CreateSceneNode(this->Name.c_str(), 
			(Ogre::SceneNode*)NULL, false, true, this->LocalX, this->LocalY, this->LocalZ, 
			1.0, 1.0, 1.0, orient.w, orient.x, orient.y, orient.z);
	// add ocean to the region

	Ogre::SceneNode* oceanSceneNode = CreateOcean(regionNode, sizeX, sizeY, waterHeight, "Water/" + this->Name);
	this->OceanSceneNode = oceanSceneNode;
	this->OceanHeight = waterHeight;
	
	Ogre::SceneNode* terrainSceneNode = CreateTerrain(regionNode, sizeX, sizeY, "Terrain/" + this->Name);
	this->TerrainSceneNode = terrainSceneNode;

	this->AddRegionSceneNode(regionNode, LG::RegionRezCodeHigh);
	// m_visCalc->RecalculateVisibility();
	return;
}

// Create the scene node for the ocean. We create a plane, add the ocean material, create a scene node
// and add the plane to the scene node and return that scene node.
// BETWEEN FRAME OPERATION
Ogre::SceneNode* Region::CreateOcean(Ogre::SceneNode* regionNode,
			 const float width, const float length, const float waterHeight, Ogre::String waterName) {
	Ogre::Plane* oceanPlane = new Ogre::Plane(0.0, 0.0, 1.0, 0);
	Ogre::MeshPtr oceanMesh = Ogre::MeshManager::getSingleton().createPlane(waterName, OLResourceGroupName, 
					*oceanPlane, width, length,
					2, 2, true,
					2, 2.0, 2.0, Ogre::Vector3::UNIT_Y);
	Ogre::String oceanMaterialName = LG::GetParameter("Renderer.Ogre.OceanMaterialName");
	LG::Log("Region::CreateOcean: r=%s, h=%f, n=%s, m=%s", 
		regionNode->getName().c_str(), waterHeight, waterName.c_str(), oceanMaterialName.c_str());
	oceanMesh->getSubMesh(0)->setMaterialName(oceanMaterialName);
	Ogre::Entity* oceanEntity = LG::RendererOgre::Instance()->m_sceneMgr->createEntity("WaterEntity/" + waterName, oceanMesh->getName());
	oceanEntity->addQueryFlags(Ogre::SceneManager::WORLD_GEOMETRY_TYPE_MASK);
	oceanEntity->setCastShadows(false);
	Ogre::SceneNode* oceanNode = regionNode->createChildSceneNode("WaterSceneNode/" + waterName);
	oceanNode->setInheritOrientation(true);
	oceanNode->setInheritScale(false);
	oceanNode->translate(width/2.0, length/2.0, waterHeight);
	oceanNode->attachObject(oceanEntity);
	return oceanNode;
}

// Create the scene node for the terrain. We don't need to make the mesh as the later calls
// will do that.
Ogre::SceneNode* Region::CreateTerrain(Ogre::SceneNode* regionNode,
			 const float width, const float length, Ogre::String terrainName) {
	LG::Log("Region::CreateTerrain: r=%s, w=%f, l=%f, n=%s", this->Name.c_str(), width, length, terrainName.c_str());
	Ogre::SceneNode* terrainNode = regionNode->createChildSceneNode("TerrainSceneNode/" + terrainName);
	terrainNode->setInheritOrientation(true);
	terrainNode->setInheritScale(false);
	terrainNode->translate(0.0, 0.0, 0.0);
	return terrainNode;
}

// Given a scene node for a terrain, find the manual object on that scene node and
// update the manual object with the heightmap passed. If  there is no manual object on
// the scene node, remove all it's attachments and add the manual object.
// The heightmap is passed in a 1D array ordered by width rows (for(width) {for(length) {hm[w,l]}})
// This must be called between frames since it touches the scene graph
// BETWEEN FRAME OPERATION
void Region::UpdateTerrain(const int hmWidth, const int hmLength, const float* hm) {
	Ogre::SceneNode* node = this->TerrainSceneNode;
	LG::Log("Region::UpdateTerrain: updating terrain for region %s", this->Name.c_str());

	if (node == NULL) {
		LG::Log("Region::UpdateTerrain: terrain scene node doesn't exist. Not updating terrain.");
		return;
	}

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
		LG::Log("Region::UpdateTerrain: creating terrain ManualObject for region %s", this->Name.c_str());
        // if no attached objects, we add our dynamic ManualObject
		Ogre::ManualObject* mob = LG::RendererOgre::Instance()->m_sceneMgr->createManualObject("ManualObject/" + node->getName());
		mob->addQueryFlags(Ogre::SceneManager::WORLD_GEOMETRY_TYPE_MASK);
		mob->setDynamic(true);
		mob->setCastShadows(true);
		mob->setVisible(true);
		mob->setQueryFlags(Ogre::SceneManager::WORLD_GEOMETRY_TYPE_MASK);
		node->attachObject(mob);
		// m_visCalc->RecalculateVisibility();
	}

	Ogre::ManualObject* mo = (Ogre::ManualObject*)node->getAttachedObject(0);

	// stuff our heightmap information into the dynamic manual object
	mo->estimateVertexCount(hmWidth * hmLength);
	mo->estimateIndexCount(hmWidth * hmLength * 6);

	if (mo->getNumSections() == 0) {
		// if first time
		mo->begin(LG::GetParameter("Renderer.Ogre.DefaultTerrainMaterial"));
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

}
