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
#include "OgreImageCodec.h"

// Based on an idea discussed at https://www.ogre3d.org/forums/viewtopic.php?f=2&t=48191
// we create a codec that will be asked for last when loading an image file.
// The problem is that the image file exists but we can't read it. Usually means
// a corrupted file.

// forward definition
namespace RendererOgre { class RendererOgre; }

namespace BadImageCodec {

#define BIC_EXTENSION "zzz"

class BadImageCodec : public Ogre::ImageCodec
{
public:
	BadImageCodec(RendererOgre::RendererOgre*);
	~BadImageCodec(void) { };

	// called to create and register this codec
	void startup();

	// functions we need to subclass Ogre::ImageCodec
	Ogre::String getType() const;
	Ogre::DataStreamPtr code(Ogre::MemoryDataStreamPtr& , Ogre::Codec::CodecDataPtr&) const;
	void codeToFile(Ogre::MemoryDataStreamPtr&, const Ogre::String&, Ogre::Codec::CodecDataPtr&) const;
	Ogre::String magicNumberToFileExt(const char*, size_t) const;

	// routine to actually decode the data
	Ogre::Codec::DecodeResult decode(Ogre::DataStreamPtr&) const;

private:
	RendererOgre::RendererOgre* m_ro;
};

}