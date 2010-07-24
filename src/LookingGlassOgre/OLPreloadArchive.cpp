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
#include "OLPreloadArchive.h"
#include "OgreFileSystem.h"
#include "LookingGlassOgre.h"
#include "RendererOgre.h"

namespace LG {
OLPreloadArchive::OLPreloadArchive( const Ogre::String& name, const Ogre::String& archType )
			: Ogre::Archive(name, archType) {
	LG::Log("OLPreloadArchive creation: n=%s, t=%s", name.c_str(), archType.c_str());
	m_FSArchive = NULL;
	return;
}

OLPreloadArchive::~OLPreloadArchive(void) {
	unload();
}

/// Get the name of this archive
// const String& OLPreloadArchive::getName(void) const { return mName; }

/// Returns whether this archive is case sensitive in the way it matches files
bool OLPreloadArchive::isCaseSensitive(void) const {
	return m_FSArchive->isCaseSensitive();
}

// Loads the archive.
void OLPreloadArchive::load() {
	// this is really a wrapper for a filesystem archive
	LG::Log("OLPreloadArchive::load(): mName=%s", mName.c_str());
	m_defaultMeshFilename = LG::GetParameter("Renderer.Ogre.DefaultMeshFilename");
	LG::Log("OLPreloadArchive::load(): DefaultMeshFile=%s", m_defaultMeshFilename.c_str());
	m_defaultTextureFilename = LG::GetParameter("Renderer.Ogre.DefaultTextureFilename");
	LG::Log("OLPreloadArchive::load(): DefaultTextureFile=%s", m_defaultTextureFilename.c_str());
	m_FSArchive = OGRE_NEW Ogre::FileSystemArchive(mName, "XXOLFilesystem");
	m_FSArchive->load();
}

// Unloads the archive.
void OLPreloadArchive::unload() {
	Ogre::ArchiveManager::getSingleton().unload(m_FSArchive);
}

// The preloaded entities are in a filesystem without the grid name at the beginning.
// This routine strips the grid name of the beginning and looks for the file.
Ogre::DataStreamPtr OLPreloadArchive::open(const Ogre::String& filename, bool readonly) const {
	LG::Log("OLPreloadArchive::open(%s)", filename.c_str());
	Ogre::String entityName = ExtractEntityFromFilename(filename);
	if (m_FSArchive->exists(entityName)) {
		return m_FSArchive->open(entityName);
	}
	// if the file doesn't exist, we shouldn't have been asked for it
	LG::Log("OLPreloadArchive::open(): the entity didn't exist!!!!. '%s'", entityName.c_str());
	return Ogre::DataStreamPtr(new Ogre::MemoryDataStream(10));
}

Ogre::DataStreamPtr OLPreloadArchive::open(const Ogre::String& filename) const {
	return this->open(filename, true);
}

// List all file names in the archive.
Ogre::StringVectorPtr OLPreloadArchive::list(bool recursive, bool dirs) {
	LG::Log("OLPreloadArchive::list()");
	return m_FSArchive->list(recursive, dirs);
}

// List all files in the archive with accompanying information.
Ogre::FileInfoListPtr OLPreloadArchive::listFileInfo(bool recursive, bool dirs) {
	LG::Log("OLPreloadArchive::listFileInfo()");
	return m_FSArchive->listFileInfo(recursive, dirs);
}

Ogre::// Find all file or directory names matching a given pattern
StringVectorPtr OLPreloadArchive::find(const Ogre::String& pattern, bool recursive, bool dirs) {
	LG::Log("OLPreloadArchive::find(%s)", pattern.c_str());
	return m_FSArchive->find(pattern, recursive, dirs);
}

// Find out if the named file exists (note: fully qualified filename required) */
bool OLPreloadArchive::exists(const Ogre::String& filename) {
	// Ogre::String entityName = ExtractEntityFromFilename(filename);
	// bool answer = m_FSArchive->exists(ExtractEntityFromFilename(filename));
	// LG::Log("OLPreloadArchive::exists(%s)(%s)(%s)", filename.c_str(), 
	// 	entityName.c_str(), answer ? "exists" : "does not exist");
	// return answer;
	return m_FSArchive->exists(ExtractEntityFromFilename(filename));
}

// Retrieve the modification time of a given file */
time_t OLPreloadArchive::getModifiedTime(const Ogre::String& filename) {
	return m_FSArchive->getModifiedTime(ExtractEntityFromFilename(filename));
}

// Find all files or directories matching a given pattern in this
Ogre::FileInfoListPtr OLPreloadArchive::findFileInfo(const Ogre::String& pattern, 
			  bool recursive, bool dirs) {
	return m_FSArchive->findFileInfo(pattern, recursive, dirs);
}

const Ogre::String& OLPreloadArchiveFactory::getType(void) const {
	static Ogre::String name = OLPreloadTypeName;
	return name;
}

Ogre::Archive* OLPreloadArchiveFactory::createInstance( const Ogre::String& name ) {
	LG::Log("OLPreloadArchiveFactory::createInstance(%s)", name.c_str());
    return OGRE_NEW OLPreloadArchive(name, OLPreloadTypeName);
}

// The filename passed to us has the grid name at the beginning. Remove that and
// return the result. We presume it's everything up the slash.
Ogre::String OLPreloadArchive::ExtractEntityFromFilename(Ogre::String filename) const {
	int pos = filename.find("/");
	if (pos > 0) {
		return filename.substr(pos + 1);
	}
	return Ogre::String("");
}
}