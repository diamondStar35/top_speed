/**
* Common library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __COMMON_MUTEX_H__
#define __COMMON_MUTEX_H__

#include <Common/If/Common.h>

class CMutex;
class CSingleLock;

class Mutex
{
public:
    ///@name Constructor and destructor
    //@{
    _common_ Mutex( );
    _common_ virtual ~Mutex( );
    //@}
public:
    _common_ void       lock( );
    _common_ void       unlock( );

public:
    class Guard;
private:
    CRITICAL_SECTION    m_criticalSection;
};


class Mutex::Guard
{
public:
    Guard(Mutex& mutex) : m_mutex(mutex)    { m_mutex.lock( );      }
    ~Guard( )                               { m_mutex.unlock( );    }  
    Mutex::Guard& operator= (const Mutex::Guard&) { return *this; }
private:
    Mutex&  m_mutex;
};

#endif /* __COMMON_MUTEX_H__ */
