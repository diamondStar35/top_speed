/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __DXCOMMON_TIMER_H__
#define __DXCOMMON_TIMER_H__

#include <DxCommon/If/Internal.h>


namespace DirectX
{

class Timer;

/*************************************************************************************
 *@class Timer
 *@description
 *    The Timer class offers an interface to measure how much time (in milliseconds)
 *    has elapsed since the last call. 
 *************************************************************************************/
class Timer
{
public:
    _dxcommon_ Timer( );
    _dxcommon_ virtual ~Timer( );

public:
    _dxcommon_ void  start( );
    _dxcommon_ void  stop( );
    _dxcommon_ void  reset( );
    _dxcommon_ UInt  elapsed(Boolean reset = true);
    _dxcommon_ Huge  microElapsed( );


private:
    Boolean   m_usingQPT; // QueryPerformanceTimer supported?
    Huge      m_ticksPerSec; 
    Huge      m_lastTimed;
};

} // namespace DirectX

#endif /* __DXCOMMON_TIMER_H__ */
