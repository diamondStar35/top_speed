/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include <DxCommon/If/Common.h>
#include <dxerr8.h>


namespace DirectX
{

Timer::Timer( )
{
    DXCOMMON("(+) Timer");
    // See how many ticks per second our PerformanceTimer does
    LARGE_INTEGER ticksPerSec;
    m_usingQPT = (QueryPerformanceFrequency(&ticksPerSec) != 0);
    // Do we have a PerformanceTimer?
    if (m_usingQPT)
    {
        DXCOMMON("Timer : using performance timer");
        // Initialize m_ticksPerSec
        m_ticksPerSec = ticksPerSec.QuadPart;
        // Initialize m_lastTimed
        LARGE_INTEGER queryTime;
        QueryPerformanceCounter(&queryTime);
        m_lastTimed = queryTime.QuadPart;
    }
    else
    {
        DXCOMMON("Timer : using standard timer");
        // Initialize m_lastTimed
        m_lastTimed = timeGetTime( );
    }
}


Timer::~Timer( )
{
    
}


void Timer::start( )
{
    DXCOMMON("Timer::start : not implemented");
}


void Timer::stop( )
{
    DXCOMMON("Timer::stop : not implemented");
}


void Timer::reset( )
{
    DXCOMMON("Timer::reset : not implemented");
}


UInt Timer::elapsed(Boolean reset)
{
    if (m_usingQPT)
    {
        LARGE_INTEGER queryTime;
        QueryPerformanceCounter( &queryTime );
        UInt elapsed = (UInt) ((queryTime.QuadPart - m_lastTimed)*1000.0 / ((double) m_ticksPerSec));
        if (reset)
            m_lastTimed = queryTime.QuadPart;
        return elapsed;
    }
    else
    {
        Huge time = timeGetTime( );
        UInt elapsed = (UInt) (time - m_lastTimed);
        if(reset)
            m_lastTimed = time;
        return elapsed;
    }
}


Huge Timer::microElapsed( )
{
    if (m_usingQPT)
    {
        LARGE_INTEGER queryTime;
        QueryPerformanceCounter( &queryTime );
        Huge elapsed = (Huge)((queryTime.QuadPart - m_lastTimed)*1000000.0 / ((double) m_ticksPerSec));
        m_lastTimed = queryTime.QuadPart;
        return elapsed;
    }
    else
    {
        Huge time = timeGetTime( );
        Huge elapsed = (Huge) ((time - m_lastTimed) * 1000);
        m_lastTimed = time;
        return elapsed;
    }
}

} // namespace DirectX
