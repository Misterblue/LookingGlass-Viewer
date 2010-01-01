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
#include "ShadowBase.h"

namespace LG {

// Simple shadow  that usees Ogre's built in shadow implementation
	class Shadow03 : public ShadowBase/*, public Ogre::ShadowListener*/ {
public:
	Shadow03(const char* shadowType);
	~Shadow03();

	void AddTerrainShadow(Ogre::Material* mat);
	void AddLightShadow(Ogre::Light* lit);
	void AddReceiverShadow(Ogre::Material* mat);
	void AddCasterShadow(Ogre::MovableObject* mat);

	// Ogre::ShadowListener
    void shadowTextureCasterPreViewProj(Ogre::Light *light, Ogre::Camera *cam);
    void shadowTexturesUpdated(size_t) {}
    void shadowTextureReceiverPreViewProj(Ogre::Light*, Ogre::Frustum*) {}


private:
};

}