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
#include "LookingGlassOgre.h"
#include "OIS/OIS.h"

// forward definition
namespace RendererOgre { class RendererOgre; }

class UserIO :
	public OIS::MouseListener,
	public OIS::KeyListener,
	public Ogre::FrameListener
{
public:
	static const int IOTypeKeyPressed = 1;			// p1 = keycode
	static const int IOTypeKeyReleased = 2;		// p1 = keycode
	static const int IOTypeMouseMove = 3;			// p2 = x since last, p3 = y since last
	static const int IOTypeMouseButtonDown = 4;	// p1 = button number
	static const int IOTypeMouseButtonUp = 5;		// p1 = button number

	UserIO(RendererOgre::RendererOgre*);
	~UserIO(void);

	// MouseListener
	bool mouseMoved(const OIS::MouseEvent&);
	bool mousePressed(const OIS::MouseEvent&, OIS::MouseButtonID);
	bool mouseReleased(const OIS::MouseEvent&, OIS::MouseButtonID);

	// KeyListener
	bool keyPressed(const OIS::KeyEvent &e);
	bool keyReleased(const OIS::KeyEvent &e);

	// Ogre::FrameListener
	bool frameStarted(const Ogre::FrameEvent&);
	bool frameRenderingQueued(const Ogre::FrameEvent&);
	bool frameEnded(const Ogre::FrameEvent&);

protected:
	RendererOgre::RendererOgre* m_ro;

	OIS::InputManager* m_inputManager;
	OIS::Mouse* m_mouse;
	OIS::Keyboard* m_keyboard;
	
	void windowResized();
	
	void callUserIOCallback(int, int, float, float);

};
