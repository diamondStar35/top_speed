/**
* Common library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __COMMON_WINDOW_H__
#define __COMMON_WINDOW_H__

#include <windows.h>

class Window;
class MessageBox;


class Window
{
public:
    //typedef LRESULT (CALLBACK* WNDPROC)(HWND, UINT, WPARAM, LPARAM);
    typedef HWND        Handle;
    typedef HINSTANCE   Instance;
    typedef UInt      (*MessageHandler)(Handle, UInt, UInt, Int);

public:
    _common_ Window( );
    _common_ virtual ~Window( );

public:
    _common_ void create(Instance, WNDPROC msgHndlr, Int menu, Int smallIcon, Int largeIcon, const Char* title);

    _common_ Boolean handleMessages( );

public:
    _common_ void show( );
    _common_ void hide( );

public:
    Handle handle( ) const { return m_handle; }

private:
    Handle          m_handle;
    HINSTANCE       m_currentInstance;
    HACCEL          m_accelerator;
};

class Messagebox
{
public:
    _common_ Messagebox(Window::Handle, Char* title, Char* message);
    _common_ virtual ~Messagebox( );
};



#endif /* __COMMON_WINDOW_H__ */
