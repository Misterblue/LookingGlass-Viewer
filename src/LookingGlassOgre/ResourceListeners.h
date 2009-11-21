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
#include "OgreScriptCompiler.h"
#include "RendererOgre.h"

namespace LG {
class OLResourceLoadingListener : public Ogre::ResourceLoadingListener {
public:
	OLResourceLoadingListener();
	~OLResourceLoadingListener(void);

	Ogre::DataStreamPtr resourceLoading(const Ogre::String&, const Ogre::String&, Ogre::Resource*);
	void resourceStreamOpened(const Ogre::String&, const Ogre::String&, Ogre::Resource*, Ogre::DataStreamPtr&);
	bool resourceCollision(Ogre::Resource*, Ogre::ResourceManager*);

private:
};

class OLMeshSerializerListener : public Ogre::MeshSerializerListener, public Ogre::FrameListener {
public:
	OLMeshSerializerListener();
	~OLMeshSerializerListener(void);
	void processMaterialName(Ogre::Mesh *mesh, Ogre::String *name);
	void processSkeletonName(Ogre::Mesh *mesh, Ogre::String *name);

private:
};

class OLScriptCompilerListener : public Ogre::ScriptCompilerListener {
public:
	OLScriptCompilerListener();
	~OLScriptCompilerListener(void);

	bool handleEvent(Ogre::ScriptCompiler*, const Ogre::String&, const std::vector<Ogre::Any>&, Ogre::Any*);
	Ogre::Any createObject(Ogre::ScriptCompiler*, const Ogre::String&, const std::vector<Ogre::Any>&);

private:
};
}