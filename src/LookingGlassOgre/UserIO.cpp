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
#include "UserIO.h"
#include "LookingGlassOgre.h"
#include "RendererOgre.h"

namespace LG {
UserIO::UserIO() {
	size_t windowHnd = 0;
	Ogre::RenderWindow* theWindow = LG::RendererOgre::Instance()->m_window;
	theWindow->getCustomAttribute("WINDOW", &windowHnd);
	m_inputManager = OIS::InputManager::createInputSystem(windowHnd);

	LG::Log("UserIO: setting up mouse and keyboard");

	m_mouse = static_cast<OIS::Mouse*>(m_inputManager->createInputObject(OIS::OISMouse, true));
	m_mouse->setEventCallback(this);

	m_keyboard = static_cast<OIS::Keyboard*>(m_inputManager->createInputObject(OIS::OISKeyboard, true));
	m_keyboard->setEventCallback(this);

	// set initial mouse boundries
	windowResized();

	LG::RendererOgre::Instance()->m_root->addFrameListener(this);
}

UserIO::~UserIO(void) {
	if (m_mouse) {
		m_inputManager->destroyInputObject(m_mouse);
		m_mouse = NULL;
	}
	if (m_keyboard) {
		m_inputManager->destroyInputObject(m_keyboard);
		m_keyboard = NULL;
	}
	m_inputManager->destroyInputSystem(m_inputManager);
	m_inputManager = NULL;
}

// MouseListener
bool UserIO::mouseMoved(const OIS::MouseEvent &e) {
	// LG::Log("UserIO: Mouse moved");
	callUserIOCallback(IOTypeMouseMove, 0, (float)e.state.X.rel, (float)e.state.Y.rel);
	return true;
}

bool UserIO::mousePressed(const OIS::MouseEvent &e, OIS::MouseButtonID id) {
	// LG::Log("UserIO: Mouse pressed");
	callUserIOCallback(IOTypeMouseButtonDown, id, 0.0, 0.0);
	return true;
}

bool UserIO::mouseReleased(const OIS::MouseEvent &e, OIS::MouseButtonID id) {
	// LG::Log("UserIO: Mouse released");
	callUserIOCallback(IOTypeMouseButtonUp, id, 0.0, 0.0);
	return true;
}

// KeyListener
bool UserIO::keyPressed(const OIS::KeyEvent &e) {
	// LG::RendererOgre::Instance()->Log("UserIO: key pressed: %d", (int)e.key);
	callUserIOCallback(IOTypeKeyPressed, e.key, 0.0, 0.0);
	return true;
}

bool UserIO::keyReleased(const OIS::KeyEvent &e) {
	// LG::Log("UserIO: key released: %d", (int)e.key);
	callUserIOCallback(IOTypeKeyReleased, e.key, 0.0, 0.0);
	return true;
}

void UserIO::callUserIOCallback(int type, int param1, float param2, float param3) {
	if (LG::userIOCallback != NULL) {
		(*LG::userIOCallback)(type, param1, param2, param3);
	}
}

void UserIO::windowResized() {
	unsigned int wid, hit, dep;
	int top, left;
	LG::RendererOgre::Instance()->m_window->getMetrics(wid, hit, dep, left, top);
	const OIS::MouseState& ms = m_mouse->getMouseState();
	ms.width = wid;
	ms.height = hit;
}

// ========== Ogre::FrameListener
bool UserIO::frameStarted(const Ogre::FrameEvent& evt) {
	return true;
}

bool UserIO::frameRenderingQueued(const Ogre::FrameEvent& evt) {
	if (m_mouse) m_mouse->capture();
	if (m_keyboard) m_keyboard->capture();
	return true;
}

bool UserIO::frameEnded(const Ogre::FrameEvent& evt) {
	return true;
}
// ========== end of Ogre::FrameListener

}