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
#include "OLArchive.h"
#include "OgreFileSystem.h"
#include "LookingGlassOgre.h"
#include "RendererOgre.h"

namespace LG {

OLArchive::OLArchive( const Ogre::String& name, const Ogre::String& archType )
			: Ogre::Archive(name, archType) {
	LG::Log("OLArchive creation: n=%s, t=%s", name.c_str(), archType.c_str());
	m_FSArchive = NULL;
	return;
}

OLArchive::~OLArchive(void) {
	unload();
}

/// Get the name of this archive
// const String& OLArchive::getName(void) const { return mName; }

/// Returns whether this archive is case sensitive in the way it matches files
bool OLArchive::isCaseSensitive(void) const {
	return m_FSArchive->isCaseSensitive();
}

// Loads the archive.
void OLArchive::load() {
	// this is really a wrapper for a filesystem archive
	LG::Log("OLArchive::load(): mName=%s", mName.c_str());
	m_defaultMeshFilename = LG::GetParameter("Renderer.Ogre.DefaultMeshFilename");
	LG::Log("OLArchive::load(): DefaultMeshFile=%s", m_defaultMeshFilename.c_str());
	m_defaultTextureFilename = LG::GetParameter("Renderer.Ogre.DefaultTextureFilename");
	LG::Log("OLArchive::load(): DefaultTextureFile=%s", m_defaultTextureFilename.c_str());
	m_FSArchive = OGRE_NEW Ogre::FileSystemArchive(mName, "XXOLFilesystem");
	LG::Log("OLArchive::load(): loading FSArchive");
	m_FSArchive->load();
	LG::Log("OLArchive::load(): completed loading FSArchive");
}

// Unloads the archive.
void OLArchive::unload() {
	Ogre::ArchiveManager::getSingleton().unload(m_FSArchive);
}

Ogre::DataStreamPtr OLArchive::open(const Ogre::String& filename) const {
	return this->open(filename, true);
}

// Open a stream on a given file. 
Ogre::DataStreamPtr OLArchive::open(const Ogre::String& filename, bool readonly) const {
	// LG::Log("OLArchive::open(%s)", filename.c_str());
	if (m_FSArchive->exists(filename)) {
		return m_FSArchive->open(filename);
	}
	// if the file doesn't exist, just return a default type
	try {
		Ogre::MemoryDataStream* renamed = 0;
		switch (ExtractResourceTypeFromName(filename)) {
			case LG::ResourceTypeMaterial:
				// we don't do materials, these are handled at a higher level if they don't exist.
				// Return an empty stream
				LG::Log("OLArchive::open(): returning empty stream for material %s", filename.c_str());
				return Ogre::DataStreamPtr();
			case LG::ResourceTypeMesh:
				LG::OLMeshTracker::Instance()->RequestMesh(filename, filename);
				// LG::RequestResource(filename.c_str(), filename.c_str(), LG::ResourceTypeMesh);
				return m_FSArchive->open(m_defaultMeshFilename);
			case LG::ResourceTypeTexture:
				LG::RequestResource(filename.c_str(), filename.c_str(), LG::ResourceTypeTexture);
				return m_FSArchive->open(m_defaultTextureFilename);
		}
	}
	catch (char* e) {
		LG::Log("OLArchive::open(): default shape not found: %s", e);
	}
	return Ogre::DataStreamPtr(new Ogre::MemoryDataStream(10));
}

// List all file names in the archive.
Ogre::StringVectorPtr OLArchive::list(bool recursive, bool dirs) {
	LG::Log("OLArchive::list()");
	return m_FSArchive->list(recursive, dirs);
	// return Ogre::StringVectorPtr(new Ogre::StringVector());
}

// List all files in the archive with accompanying information.
Ogre::FileInfoListPtr OLArchive::listFileInfo(bool recursive, bool dirs) {
	LG::Log("OLArchive::listFileInfo()");
	return m_FSArchive->listFileInfo(recursive, dirs);
	// return Ogre::FileInfoListPtr(new Ogre::FileInfoList());
}

Ogre::// Find all file or directory names matching a given pattern
StringVectorPtr OLArchive::find(const Ogre::String& pattern, bool recursive, bool dirs) {
	LG::Log("OLArchive::find(%s)", pattern.c_str());
	return m_FSArchive->find(pattern, recursive, dirs);
	// return Ogre::StringVectorPtr(new Ogre::StringVector());
}

// Find out if the named file exists (note: fully qualified filename required) */
bool OLArchive::exists(const Ogre::String& filename) {
	return true;
	/*
	// LG::Log("OLArchive::exists(%s)", filename.c_str());
	if (m_FSArchive->exists(filename)) {
		return true;
	}
	// it isn't in the cache so we have to request it. Figure out what to request
	int rType = ExtractResourceTypeFromName(filename);
	if (rType != LG::ResourceTypeUnknown) {
		// we don't have an asset context, fake things by just passing the name twice
		LG::RequestResource(filename.c_str(), filename.c_str(), rType);
		return true;
	}
	LG::Log("OLArchive::exists. It does not exist");
	return false;
	*/
}

// Retrieve the modification time of a given file */
time_t OLArchive::getModifiedTime(const Ogre::String& filename) {
	return m_FSArchive->getModifiedTime(filename);
}

// Find all files or directories matching a given pattern in this
Ogre::FileInfoListPtr OLArchive::findFileInfo(const Ogre::String& pattern, 
			  bool recursive, bool dirs) {
	return m_FSArchive->findFileInfo(pattern, recursive, dirs);
}

const Ogre::String& OLArchiveFactory::getType(void) const {
	static Ogre::String name = OLArchiveTypeName;
	return name;
}

Ogre::Archive* OLArchiveFactory::createInstance( const Ogre::String& name ) {
	LG::Log("OLArchiveFactory::createInstance(%s)", name.c_str());
    return OGRE_NEW OLArchive(name, OLArchiveTypeName);
}

//From the filename, figure out what type of resource it is. We use the file extension
// but we also always return 'texture' for unknown types because they will be .png, .jpg, etc
int OLArchive::ExtractResourceTypeFromName(Ogre::String resourceName) const {
	int ret = LG::ResourceTypeUnknown;
	if (resourceName.size() > 5
			&& resourceName.substr(resourceName.size()-5, 5) == ".mesh") {
		ret = LG::ResourceTypeMesh;
	}
	else {
		if (resourceName.size() > 9
				&& resourceName.substr(resourceName.size()-9, 9) == ".material") {
			ret = LG::ResourceTypeMaterial;
		}
		else {
			ret = LG::ResourceTypeTexture;
		}
	}
	return ret;
}
}