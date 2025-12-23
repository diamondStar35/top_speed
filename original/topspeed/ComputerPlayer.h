/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_COMPUTERPLAYER_H__
#define __RACING_COMPUTERPLAYER_H__

#define NVEHICLES 12

#include "Game.h"
#include "Track.h"
#include "Packets.h"

class ComputerPlayer
{
public:
    ComputerPlayer(Game* game, UInt vehicle, Track* track, Int playerNumber);
    virtual ~ComputerPlayer( );

public:
    enum State
    {
        stopped,
        starting,
        running,
        // slipping,
        crashing,
        stopping
    };

public:
    void initialize(Int positionX, Int positionY, Int trackLength);
    void finalize( );

public:
    void start( );
    void pendingStart( );
    void stop( );
    void crash(Int newPosition);
    void miniCrash(Int newPosition);
    void bump(Int bumpX, Int bumpY, int bumpSpeed);
    void quiet( );

    void run(Float elapsed, Int playerX, Int playerY);
    void evaluate(Track::Road road);

public:
    const State     state( ) const                  { return m_state;           }
    void            state(State state)              { m_state = state;          }
    Int             positionX( )                    { return m_positionX;       }
    Int             positionY( )                    { return m_positionY;       }
    void            position(Int X, Int Y)          { m_positionX = X; m_positionY = Y;   }
    Int             speed( )                        { return m_speed;           }
    CarType         carType( )                      { return m_carType;         }
    Boolean         engineRunning( )                { return m_soundEngine->playing( );   }
    Boolean         braking( )                      { return m_soundBrake->playing( );    }
    Boolean         horning( )                      { return m_soundHorn->playing( );     }
    // void            position(Int pos)               { m_position = pos;                   }
    // Int             position()               { return m_position;                   }
    Int             playerNumber( )                 { return m_playerNumber;              }
    Boolean         finished( )                     { return m_finished;                  }
    void            finished(Boolean b)             { m_finished = b;                     }
    void pause( );
    void unpause( );

//    DirectX::Sound* inFront( )                      { return m_soundInFront;    }
//    DirectX::Sound* onTail( )                       { return m_soundOnTail;     }

private:
    void AI(/* Int playerPosY */);
    Int calculateAcceleration( );

    void updateEngineFreq( );
    void setSoundPosition(DirectX::Sound* sound, DirectX::Vector3 relPos);
    void horn( );

private:
    void pushEvent(Event::Type type, Float time);
    
private:
    State                   m_state;
    Game*                   m_game;
    Track*                  m_track;
    DirectX::SoundManager*  m_soundManager;
    DirectX::Sound*         m_soundEngine;
    DirectX::Sound*         m_soundHorn;
    DirectX::Sound*         m_soundStart;
    DirectX::Sound*         m_soundBrake;
    DirectX::Sound*         m_soundCrash;
    DirectX::Sound*         m_soundMiniCrash;
    DirectX::Sound*         m_soundBump1;
    DirectX::Sound*	      m_soundBackfire;

//    DirectX::Sound*         m_soundInFront;
//    DirectX::Sound*         m_soundOnTail;
    EventList               m_eventList;
    Int                     m_frame;

    Int                     m_surface;

    Int                     m_speed;
    Int                     m_gear;
    Int                     m_positionX;
    Int                     m_positionY;
    Int                     m_switchingGear;
    CarType                 m_carType;
    Int                     m_trackLength;

    // parameters   
    Int                     m_acceleration;
    Int                     m_deceleration;
    Int                     m_topspeed;
    Int                     m_idlefreq;
    Int                     m_topfreq;
    Int                     m_shiftfreq;
    Int                     m_gears;
    Int                     m_steering;
    Int                     m_steeringFactor;

    Int                     m_playerNumber;
    // Int                     m_position;
    Int                     m_random;
    UInt                    m_prevFrequency;
    UInt                    m_frequency;
    UInt                    m_prevBrakeFrequency;
    UInt                    m_brakeFrequency;
    UInt                    m_laneWidth;
    Float                   m_relPos;
    Float                   m_nextRelPos;
    // Int                     m_panPos;
    Int                     m_diffX;
    Int                     m_diffY;
    Int                     m_currentSteering;
    Int                     m_currentThrottle;
    Int                     m_currentBrake;
    Int                     m_currentAcceleration;
    Int                     m_currentDeceleration;
    Int                     m_speedDiff;
    Int                     m_thrust;
    Int                     m_difficulty;
    Boolean                 m_finished;
    Boolean                 m_horning;
    Boolean                 m_backfirePlayedAuto;
};



#endif /* __RACING_COMPUTERPLAYER_H__ */
