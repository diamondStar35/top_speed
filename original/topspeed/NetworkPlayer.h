/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_NETWORKPLAYER_H__
#define __RACING_NETWORKPLAYER_H__

#define NVEHICLES 12


#include "Packets.h"
#include "Game.h"

class NetworkPlayer
{
public:
    NetworkPlayer( );
    virtual ~NetworkPlayer( );

    enum State
    {
        running,
        stopping
    };

public:
    void    initialize(Game* game, UInt number, UInt vehicle, Int trackLength, UInt laneWidth);
    void    finalize( );
    void    run(Float elapsed, Int playerX, Int playerY);
    void    position(Int x, Int y)  { m_positionX = x; m_positionY = y; }
    void    speed(Int speed)        { m_speed = speed;                  }
    void    frequency(Int frequency)        { m_frequency = frequency;                  }
    CarType         carType( )                      { return m_carType;         }
    void    engineRunning(Boolean b);
    Boolean engineRunning( )        { return m_engineRunning;           }
    void    braking(Boolean b)      { m_braking = b;                    }
    Boolean braking( )              { return m_braking;                 }
    void    horning(Boolean b)      { m_horning = b;                    }
    void    backfiring(Boolean b)   { m_backfiring = b;                    }
    Boolean horning( )              { return m_horning;                 }
    Boolean backfiring( )           { return m_backfiring;                 }
    Boolean initialized( )          { return m_initialized;             }
    Boolean         finished( )                     { return m_finished;                  }
    void            finished(Boolean b)             { m_finished = b;                     }
    const State     state( ) const                  { return m_state;           }
    void            state(State state)              { m_state = state;          }
    Int     positionX( )            { return m_positionX;               }
    Int     positionY( )            { return m_positionY;               }
    Int     speed( )                { return m_speed;                   }
    Int     frequency( )                { return m_frequency;                   }
    void    start( )                { m_soundStart->play( );            }
    void stop( );
    void    crash( )                { m_soundCrash->play( );            }
//    void    sayInFront( );
//    void    sayOnTail( );
    void    quiet( );
//    DirectX::Sound* inFront( )                      { return m_soundInFront;    }
//    DirectX::Sound* onTail( )                       { return m_soundOnTail;     }

private:
    void    updateEngineFreq( );
    void    setSoundPosition(DirectX::Sound* sound, DirectX::Vector3 relPos);

private:
    UInt                    m_number;
    State                   m_state;
    Game*                   m_game;
    Int                     m_speed;
    Int                     m_prevFrequency;
    Int                     m_frequency;
    Int                     m_prevBrakeFrequency;
    Int                     m_brakeFrequency;
    Int                     m_positionX;
    Int                     m_positionY;
    Boolean                 m_engineRunning;
    Boolean                 m_braking;
    Boolean                 m_horning;
    Boolean                 m_backfiring;
    DirectX::Sound*         m_soundEngine;
    DirectX::Sound*         m_soundStart;
    DirectX::Sound*         m_soundHorn;
    DirectX::Sound*         m_soundBackfire;
    DirectX::Sound*         m_soundBrake;
//    DirectX::Sound*         m_soundInFront;
//    DirectX::Sound*         m_soundOnTail;
    DirectX::Sound*         m_soundCrash;
    Boolean                 m_initialized;
    Boolean                 m_finished;
    Boolean                 m_backfirePlayed;
    CarType                 m_carType;
    Int                     m_trackLength;
    Int                     m_topspeed;
    Int                     m_deceleration;
    Int                     m_idlefreq;
    Int                     m_topfreq;
    Int                     m_shiftfreq;
    Int                     m_gears;
    Int                     m_frame;
    UInt                    m_laneWidth;
    Int                     m_diffX;
    Int                     m_diffY;
};





#endif /* __RACING_NETWORKPLAYER_H__ */
