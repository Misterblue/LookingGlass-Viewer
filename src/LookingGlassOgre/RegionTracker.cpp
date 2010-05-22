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
#include "RendererOgre.h"
#include "Region.h"
#include "RegionTracker.h"

namespace LG {

RegionTracker* RegionTracker::m_instance = NULL;

RegionTracker::RegionTracker() {
	m_focusRegion = NULL;
}
RegionTracker::~RegionTracker() {
}

// Add a region to the regions being tracked.
// This creates the tracking Region instance, creates a scene node for the region
// and adds scene nodes to the region for the terrain and water.
// BETWEEN FRAME OPERATION
void RegionTracker::AddRegion(const char* regionSceneName,
							  double globalX, double globalY, double globalZ,
							  const float sizeX, const float sizeY, const float waterHeight) {
	Ogre::String regionSceneNodeName = Ogre::String(regionSceneName);
	// if already have defn for region, return
	LG::Log("RegionTracker::AddRegion: adding region %s", regionSceneName);
	Region* regn = FindRegion(regionSceneNodeName);
	if (regn != NULL) {
		return;
	}
	// create Region class
	regn = new Region();
	regn->Name = regionSceneNodeName;
	m_regions.insert(std::pair<Ogre::String, Region*>(regionSceneNodeName, regn));
	regn->Init(globalX, globalY, globalZ, sizeX, sizeY, waterHeight);
	RecalculateLocalCoords();
}

Region* RegionTracker::FindRegion(Ogre::String nam) {
	RegionHashMap::iterator intr = m_regions.find(nam);
	if (intr == m_regions.end()) {
		return NULL;
	}
	return intr->second;
}

std::list<Region*> RegionTracker::GetRegions() {
	std::list<Region*> regns;
	for (RegionHashMap::iterator intr = m_regions.begin(); intr != m_regions.end(); intr++) {
		Region* otherRegn = intr->second;
		regns.push_back(otherRegn);
	}
	return regns;
}

void RegionTracker::UpdateTerrain(const char* regnName, const int width, const int length, const float* hm) {
	Ogre::String regionName = Ogre::String(regnName);
	Region* regn = FindRegion(regionName);
	if (regn != NULL) {
		regn->UpdateTerrain(width, length, hm);
	}
	else {
		LG::Log("RegionTracker::UpdateTerrain: region not found so terrain not updated: %s", regnName);
	}
}

// Set a focus region and update all region's local coords relative to the focus region
// BETWEEN FRAME OPERATION
void RegionTracker::SetFocusRegion(Ogre::String regionName) {
	LG::Log("RegionTracker::SetFocusRegion: %s", regionName.c_str());
	for (RegionHashMap::iterator intr = m_regions.begin(); intr != m_regions.end(); intr++) {
		Region* otherRegn = intr->second;
		otherRegn->SetFocusRegion(false);
	}
	Region* regn = FindRegion(regionName);
	if (regn != NULL) {
		m_focusRegion = regn;
		regn->SetFocusRegion(true);
		RecalculateLocalCoords();
	}
}

// recalcualate the local coords based on the focus region
// BETWEEN FRAME OPERATION
void RegionTracker::RecalculateLocalCoords() {
	Region* regn = GetFocusRegion();
	if (regn != NULL) {
		for (RegionHashMap::iterator intr = m_regions.begin(); intr != m_regions.end(); intr++) {
			Region* otherRegn = intr->second;
			otherRegn->CalculateLocal(regn->GlobalX, regn->GlobalY, regn->GlobalZ);
		}
	}
	// Do I have to update all the scene nodes to tell them that stuff has changed?
	LG::RendererOgre::Instance()->m_sceneMgr->getRootSceneNode()->needUpdate();
}

// Focusing a region causes the coordinate system to be zeroed to that point. Since the camera
// operates in global coordinates, it must be offset for the focus region.
Ogre::Vector3 RegionTracker::PositionForFocusRegion(Ogre::Vector3 pos) {
	return PositionCameraForFocusRegion(pos.x, pos.y, pos.z);
}

// Focusing a region causes the coordinate system to be zeroed to that point. Since the camera
// operates in global coordinates, it must be offset for the focus region.
Ogre::Vector3 RegionTracker::PositionCameraForFocusRegion(double px, double py, double pz) {
	Region* fRegion = GetFocusRegion();
	Ogre::Vector3 newPos;
	if (fRegion != NULL) {
		newPos.x = (Ogre::Real)(px - fRegion->GlobalX);
		newPos.y = (Ogre::Real)(py - fRegion->GlobalY);
		newPos.z = (Ogre::Real)(pz - fRegion->GlobalZ);
	}
	else {
		// if no focus region, just return what they passed
		newPos.x = (Ogre::Real)px;
		newPos.y = (Ogre::Real)py;
		newPos.z = (Ogre::Real)pz;
	}
	// cam->setPosition(px - fRegion->GlobalX, py - fRegion->GlobalY, pz - fRegion->GlobalZ);
	return newPos;
}

// return focus region or NULL if none
Region* RegionTracker::GetFocusRegion() {
	return m_focusRegion;
}

void RegionTracker::SetRegionDetail(Ogre::String regionName, const RegionRezCode LODLevel) {
	Region* regn = FindRegion(regionName);
	if (regn != NULL) {
		regn->ChangeRez(LODLevel);
	}
}

}


