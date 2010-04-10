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
#include "LGCamera.h"
#include "Region.h"

namespace LG {
class RegionTracker : public SingletonInstance {
public:
	RegionTracker();
	~RegionTracker();
		
	// SingletonInstance.Instance();
	static RegionTracker* Instance() { 
		if (LG::RegionTracker::m_instance == NULL) {
			LG::RegionTracker::m_instance = new RegionTracker();
		}
		return LG::RegionTracker::m_instance; 
	}

	void SetFocusRegion(Ogre::String rName);
	Region* GetFocusRegion();
	void SetRegionDetail(Ogre::String rName, const RegionRezCode LODLevel);
	void AddRegion(const char* rName, double globalX, double globalY, double globalZ, 
		const float sizeX, const float sizeY, const float oceanHeight);
	Region* FindRegion(Ogre::String);
	void UpdateTerrain(const char*, const int, const int, const float*);

	std::list<Region*> GetRegions();

	Ogre::Vector3 PositionCameraForFocusRegion(double px, double py, double pz);

private:
	static RegionTracker* m_instance;

	typedef stdext::hash_map<Ogre::String, Region*> RegionHashMap;
	RegionHashMap m_regions;
	Region* m_focusRegion;
	void RecalculateLocalCoords();

};
}