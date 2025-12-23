/**
* Common library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include <Common/If/Mutex.h>


Mutex::Mutex( )
{
    ::InitializeCriticalSection(&m_criticalSection);
}


Mutex::~Mutex( )
{
    ::DeleteCriticalSection(&m_criticalSection);
}


void
Mutex::lock( )
{
    ::EnterCriticalSection(&m_criticalSection); 
}


void
Mutex::unlock( )
{
    ::LeaveCriticalSection(&m_criticalSection);
}
