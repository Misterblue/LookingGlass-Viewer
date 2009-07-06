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
#include "BadImageCodec.h"
#include "LookingGlassOgre.h"

namespace BadImageCodec {

	// code used to start this up.
void BadImageCodec::startup() {
	Ogre::ImageCodec* codec = OGRE_NEW BadImageCodec();
	Ogre::Codec::registerCodec(codec);
}

Ogre::String BadImageCodec::getType() const {
	return BIC_EXTENSION;
}

Ogre::String BadImageCodec::magicNumberToFileExt(const char*, size_t) const {
	return BIC_EXTENSION;
}

// functions we need to subclass Ogre::ImageCodec
Ogre::DataStreamPtr BadImageCodec::code(Ogre::MemoryDataStreamPtr& , Ogre::Codec::CodecDataPtr&) const {
	LookingGlassOgr::Log("BadImageCodec::code: Attempt to code in the bad codec");
	return Ogre::DataStreamPtr();
}

// functions we need to subclass Ogre::ImageCodec
void BadImageCodec::codeToFile(Ogre::MemoryDataStreamPtr&, const Ogre::String&, Ogre::Codec::CodecDataPtr&) const {
	LookingGlassOgr::Log("BadImageCodec::codeToFile: Attempt to code in the bad codec");
	return;
}

// routine to actually decode the data
// We just get a datastream here. 
// TODO: figure out how to get back to the corrupt file and blow it away and get a new request sent.
// TODO: This is code the could be replaced with fetching a "Corrupt texture" file
Ogre::Codec::DecodeResult BadImageCodec::decode(Ogre::DataStreamPtr& dstrm) const {
	LookingGlassOgr::Log("BadImageCodec::decode: CORRUPT FILE!!! %s", dstrm->getName().c_str());

	// dstrm->getName() gets the entity name. Request a reload of the texture.
	LookingGlassOgr::RequestResource(dstrm->getName().c_str(), dstrm->getName().c_str(), 
		LookingGlassOgr::ResourceTypeTexture);

	// for the moment, put something in it's place
	ImageCodec::ImageData * imgData = OGRE_NEW ImageCodec::ImageData();
	imgData->width = 2;
	imgData->height = 2;
	imgData->depth = 1;
	imgData->format = Ogre::PF_BYTE_RGBA;
	int colors[4] = { 0xFFFF0080, 0xFF80FFFF, 0xFFFF80FF, 0xFFFFFF80}; // make it really ugly!
	int * pData = (int *)OGRE_MALLOC(sizeof(colors), Ogre::MEMCATEGORY_GENERAL);
	memcpy(pData, colors, sizeof(colors));
	return std::make_pair(Ogre::MemoryDataStreamPtr(new Ogre::MemoryDataStream(pData, 1, true)), Ogre::Codec::CodecDataPtr(imgData));
}

}
