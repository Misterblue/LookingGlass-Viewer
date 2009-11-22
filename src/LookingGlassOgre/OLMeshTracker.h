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

namespace LG {
/*
NOTE TO THE NEXT PERSON: CODE NOT COMPLETE OR HOOKED INTO MAIN CODE
This code is started but not complete. The idea is to create a routine that
tracks meshes and their state (loaded, unloaded, ...) with the goal of allowing
the actual file access part of a mesh load (the call to mesh->prepare()) be
done outside the frame rendering thread.
*/
	class OLMeshTracker {
	public:
		OLMeshTracker();
		~OLMeshTracker();

		static OLMeshTracker* Instance() { 
			if (LG::OLMeshTracker::m_instance == NULL) {
				LG::OLMeshTracker::m_instance = new OLMeshTracker();
			}
			return LG::OLMeshTracker::m_instance; 
		}

		void TrackMesh(Ogre::String meshName, Ogre::String meshGroupName, Ogre::String contextEntName, Ogre::String fingerprint);
		void UnTrackMesh(Ogre::String meshName);
		void MakeLoaded(Ogre::String meshName, void(*callback)(void*), void* callbackParam);
		void MakeUnLoaded(Ogre::String meshName, void(*callback)(void*), void* callbackParam);
		void MakePersistant(Ogre::String meshName, Ogre::String entName);
		void MakePersistant(Ogre::MeshPtr mesh, Ogre::String entName);
		Ogre::String GetMeshContext(Ogre::String meshName);
		Ogre::String GetSimilarMesh(Ogre::String fingerprint);


	private:
		static OLMeshTracker* m_instance;

		Ogre::MeshSerializer* m_meshSerializer;
		Ogre::String m_cacheDir; 
	};
}