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
// #include "StdAfx.h"
#include "Region.h"
#include "RendererOgre.h"

namespace LG {

Region::Region() {
		this->TerrainSceneNode = 0;
		this->OceanSceneNode = 0;
		this->Resolutions[RegionRezCodeVeryLow] = 0;
		this->Resolutions[RegionRezCodeLow] = 0;
		this->Resolutions[RegionRezCodeMed] = 0;
		this->Resolutions[RegionRezCodeHigh] = 0;
		this->CurrentRez = RegionRezCodeHigh;
		this->m_focusRegion = false;
		this->OceanHeight = 0.0;
}

Region::~Region() {
}

void Region::ReleaseRegion() {
}
void Region::ChangeRez(RegionRezCode newRez) {
	if (newRez != this->CurrentRez) {
		if (this->Resolutions[newRez] != 0) {
			// do anything to release the region
		}
		this->CurrentRez = newRez;
		// set proper local coordinates for this new scene node
		this->Resolutions[newRez]->setPosition(this->LocalX, this->LocalY, this->LocalZ);
	}
}

void Region::AddRegionSceneNode(Ogre::SceneNode* nod, RegionRezCode rez) {
	if (this->Resolutions[rez] != 0) {
		// TODO: release  the old scen node and its tree of nodes
	}
	this->Resolutions[rez] = nod;
}

void Region::SetFocusRegion(bool flag) {
	this->m_focusRegion = flag;
}

bool Region::IsFocusRegion() {
	return this->m_focusRegion;
}

// there is a new global address  that is the center of teh universe. Recalculate our position
// against this new center.
void Region::CalculateLocal(double X, double Y, double Z) {
	this->LocalX = (float)(this->GlobalX - X);
	this->LocalY = (float)(this->GlobalY - Y);
	this->LocalZ = (float)(this->GlobalZ - Z);
	LG::Log("Region::CalculateLocal: %s to %f, %f, %f", this->Name.c_str(), this->LocalX, this->LocalY, this->LocalZ);
	// set the new position in the scene node
	this->CurrentSceneNode()->setPosition(this->LocalX, this->LocalY, this->LocalZ);
	return;
}

// Initialize the region. This takes a global description and creates the scene node that will be
// the base for the region an, if needed, creates the water level.
void Region::Init( double globalX, double globalY, double globalZ, float sizeX, float sizeY, float waterHeight) {
	LG::Log("Region::Init: r=%s, <%lf,%lf,%lf>", this->Name.c_str(), globalX, globalY, globalZ);
	this->GlobalX = globalX;
	this->GlobalY = globalY;
	this->GlobalZ = globalZ;
	this->LocalX = (float)globalX;
	this->LocalY = (float)globalY;
	this->LocalZ = (float)globalZ;
	// create scene Node
	Ogre::Quaternion orient = Ogre::Quaternion(Ogre::Radian(-3.14159265f/2.0f), Ogre::Vector3(1.0f, 0.0f, 0.0f));
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
	oceanNode->translate(width/2.0f, length/2.0f, waterHeight);
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
			mo->position((Ogre::Real)xx, (Ogre::Real)yy, hm[loc++]);
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
