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
#include "LookingGlassOgre.h"
#include "RendererOgre.h"
#include "ResourceListeners.h"

namespace LG {
OLResourceLoadingListener::OLResourceLoadingListener() {
	return;
}
OLResourceLoadingListener::~OLResourceLoadingListener(){
	return;
}

Ogre::DataStreamPtr OLResourceLoadingListener::resourceLoading(const Ogre::String& rname, 
	const Ogre::String& rgroup, Ogre::Resource* resource) {

	LG::Log("ResourceListeners::resourceLoading: %s", rname.c_str());
	return Ogre::DataStreamPtr();
}

void OLResourceLoadingListener::resourceStreamOpened(const Ogre::String& rname, const Ogre::String& rgroup, 
	Ogre::Resource* resource, Ogre::DataStreamPtr& rstream) {

	LG::Log("ResourceListeners::resourceStreamOpened: %s", rname.c_str());
}

bool OLResourceLoadingListener::resourceCollision(Ogre::Resource* resource, Ogre::ResourceManager* rManager) {
	LG::Log("ResourceListeners::resourceCollision:");
	return false;
}

// ==========================================================================
OLMeshSerializerListener::OLMeshSerializerListener() {
	LG::GetOgreRoot()->addFrameListener(this);
	return;
}
OLMeshSerializerListener::~OLMeshSerializerListener() {
	return;
}
// called when mesh is being parsed and it comes across a material name
// check that it is defined and, if not, create it and read it in
void OLMeshSerializerListener::processMaterialName(Ogre::Mesh* mesh, Ogre::String* name) {
	// LG::Log("ResourceListeners::processMaterialName: %s -> %s", mesh->getName().c_str(), name->c_str());

	// get the resource to see if it exists
	Ogre::ResourceManager::ResourceCreateOrRetrieveResult result = 
			Ogre::MaterialManager::getSingleton().createOrRetrieve(*name, OLResourceGroupName);
	if ((bool)result.second) {
		// the material was created by createOrRetreive() -- try to read it in
		// The created material is an empty, blank material so we must fill it
		Ogre::MaterialPtr theMaterial = result.first;
		// Do the magic to make this material happen
		LG::OLMaterialTracker::Instance()->FabricateMaterial(*name, theMaterial);
	}
}
void OLMeshSerializerListener::processSkeletonName(Ogre::Mesh *mesh, Ogre::String *name) {
	LG::Log("ResourceListeners::processSkeletonName: %s -> %s", mesh->getName().c_str(), name->c_str());
}

// ==========================================================================
OLScriptCompilerListener::OLScriptCompilerListener() {
	return;
}

OLScriptCompilerListener::~OLScriptCompilerListener() {
	return;
}

bool OLScriptCompilerListener::handleEvent(Ogre::ScriptCompiler* compiler, const Ogre::String& oper,
	const std::vector<Ogre::Any>& args, Ogre::Any* retval) {
	// LG::Log("ResourceListeners::handlEvent: %s", oper.c_str());
	return false;
}

// Called when the script compiler is about to create and object
// Pass back the object we want the script to be read into
Ogre::Any OLScriptCompilerListener::createObject(Ogre::ScriptCompiler* compiler, const Ogre::String& type,
				const std::vector<Ogre::Any>& args) {
	// The name of the file the object is being read from
	Ogre::String filename = Ogre::any_cast<Ogre::String>(args[0]);
	// The name of the object being defined
	Ogre::String objectName = Ogre::any_cast<Ogre::String>(args[1]);
	Ogre::String groupName = Ogre::any_cast<Ogre::String>(args[2]);

	LG::Log("ResourceListeners::createObject: %s, %s, %s, %s", type.c_str(),
		filename.c_str(), objectName.c_str(), groupName.c_str());

	/*
	// check to see if it's one of our materials

	// short term kludge -- for me the filename is the same as the material name
	Ogre::ResourceManager::ResourceCreateOrRetrieveResult result = 
			Ogre::MaterialManager::getSingleton().createOrRetrieve(filename, groupName);
	if ((bool)result.second)
		LG::Log("ResourceListeners::createObject: object created again");
	Ogre::MaterialPtr theMaterial = result.first;

	return Ogre::Any(theMaterial.getPointer());
	*/
	return Ogre::Any();	// returning an null Any causes the script compiler to create it's own object

}
}