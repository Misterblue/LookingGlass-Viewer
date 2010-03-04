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
#include "OgreArchive.h"
#include "OgreArchiveFactory.h"

namespace LG {

class OLPreloadArchive : public Ogre::Archive
{
protected:
	Ogre::String m_name; 
    Ogre::String m_type;

	Ogre::Archive* m_FSArchive;

	Ogre::String m_defaultMeshFilename;
	Ogre::String m_defaultTextureFilename;
	
	Ogre::String ExtractEntityFromFilename(Ogre::String) const;

public:
	OLPreloadArchive( const Ogre::String& name, const Ogre::String& archType );

	~OLPreloadArchive(void);

	/// Get the name of this archive
	const Ogre::String& getName(void) const { return m_name; }

    /// Returns whether this archive is case sensitive in the way it matches files
    bool isCaseSensitive(void) const;

    // Loads the archive.
    void load();

    // Unloads the archive.
    void unload();

    // Open a stream on a given file. 
	Ogre::DataStreamPtr open(const Ogre::String& filename) const;
	Ogre::DataStreamPtr open(const Ogre::String& filename, bool readonly = true) const;

    // List all file names in the archive.
    Ogre::StringVectorPtr list(bool recursive = true, bool dirs = false);
    
    // List all files in the archive with accompanying information.
    Ogre::FileInfoListPtr listFileInfo(bool recursive = true, bool dirs = false);

    // Find all file or directory names matching a given pattern
    Ogre::StringVectorPtr find(const Ogre::String& pattern, bool recursive = true,
        bool dirs = false);

    // Find out if the named file exists (note: fully qualified filename required) */
    bool exists(const Ogre::String& filename); 

	// Retrieve the modification time of a given file */
	time_t getModifiedTime(const Ogre::String& filename); 


    // Find all files or directories matching a given pattern in this
    Ogre::FileInfoListPtr findFileInfo(const Ogre::String& pattern, 
        bool recursive = true, bool dirs = false);

    /// Return the type code of this Archive
    const Ogre::String& getType(void) const { return m_type; }
	// END OF Ogre::Archive

};

// our ArchiveFactory
class _OgrePrivate OLPreloadArchiveFactory : public Ogre::ArchiveFactory
{
public:
  virtual ~OLPreloadArchiveFactory() {}

  const Ogre::String& getType(void) const;

  Ogre::Archive* createInstance( const Ogre::String&);

  void destroyInstance( Ogre::Archive* arch) { delete arch; }
};

}