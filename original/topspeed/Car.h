/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_CAR_H__
#define __RACING_CAR_H__

#include "Game.h"
#include "Track.h"
#include "Packets.h"

class Track;
// class Car;


class CarListener
{
public:
    virtual void onStart( ) = 0;
    virtual void onCrash( ) = 0;
};



class Car
{
public:
    Car(Game* game, Track* track, UInt vehicle, Char* vehicleFile = NULL);
    virtual ~Car( );

public:
    enum State
    {
        stopped,
        starting,
        running,
        slipping,
        crashing,
        stopping
    };

public:
    struct Parameters
    {
        // parameters 
        Int                     engineSound;
        Int                     startSound;
        Int                     hornSound;
        Int                     throttleSound;
        Int                     crashSound;
        Int                     monoCrashSound;
        Int                     brakeSound;
        Int                     backfireSound;
        Int                     hasWipers;
        Int                     acceleration;
        Int                     deceleration;
        Int                     topspeed;
        Int                     idlefreq;
        Int                     topfreq;
        Int                     shiftfreq;
        Int                     gears;
        Int                     steering;
        Int                     steeringFactor;
    };

public:
    void initialize(Int positionX = 0, Int positionY = 0);
    void finalize( );

public:
    void start( );
    void stop( );
    void crash( );
    void miniCrash(Int newPosition);
    void bump(Int bumpX, Int bumpY, int bumpSpeed);
    void quiet( );

    void run(Float elapsed);
    void evaluate(Track::Road road);

public:
    const State     state( ) const                  { return m_state;           }
    void            state(State state)              { m_state = state;          }
    Int             positionX( )                    { return m_positionX;       }
    Int             positionY( )                    { return m_positionY;       }
    void            position(Int X, Int Y)          { m_positionX = X; m_positionY = Y;   }
    Int             speed( )                        { return m_speed;           }
    Int             frequency( )                        { return m_frequency;           }
    Int             gear( )                             { return m_gear;                }
    void            manualTransmission(Boolean b)     { m_manualTransmission = b;   }
    CarType         carType( )                      { return m_carType;         }
    void            listener(CarListener* l)        { m_listener = l;           }
    Boolean         engineRunning( )                { return m_soundEngine->playing( );   }
    Boolean         braking( )                      { return m_soundBrake->playing( );    }
    Boolean         horning( )                      { return m_soundHorn->playing( );     }
    Boolean         backfiring( );
    Boolean         userDefined( )                      { return m_userDefined;     }
    Char*           customFile( )                      { return m_customFile;     }
    void            pause( );
    void            unpause( );


private:
    Int calculateAcceleration( );

    void updateEngineFreq( );
    void updateEngineFreqManual( );
    void updateSoundRoad( );

	void brakeSound( );
	void brakeCurveSound( );

private:
    void pushEvent(Event::Type type, Float time);
    
private:
    State                   m_state;
    Game*                   m_game;
    Track*                  m_track;
    DirectX::SoundManager*  m_soundManager;
    DirectX::Sound*         m_soundEngine;
    DirectX::Sound*         m_soundThrottle;
    DirectX::Sound*         m_soundHorn;
    DirectX::Sound*         m_soundStart;
    DirectX::Sound*         m_soundBrake;
    DirectX::Sound*         m_soundCrash;
    DirectX::Sound*         m_soundMiniCrash;
    DirectX::Sound*         m_soundAsphalt;
    DirectX::Sound*         m_soundGravel;
    DirectX::Sound*         m_soundWater;
    DirectX::Sound*         m_soundSand;
    DirectX::Sound*         m_soundSnow;
    DirectX::Sound*         m_soundWipers;
    DirectX::Sound*         m_soundBump1;
    DirectX::Sound*         m_soundBadSwitch;
    DirectX::Sound*	    m_soundBackfire;
    EventList               m_eventList;
    Int                     m_frame;

    Float                   m_prevThrottleVolume;
    Float                   m_throttleVolume;

    Int                     m_surface;

    Int                     m_speed;
    Int                     m_gear;
    Int                     m_positionX;
    Int                     m_positionY;
    Boolean                 m_manualTransmission;
    Boolean                 m_backfirePlayed;
    Boolean                 m_backfirePlayedAuto;
    Int                     m_hasWipers;
    Int                     m_switchingGear;
    CarType                 m_carType;
    CarListener*            m_listener;

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
    Int                     m_thrust;
    Int                     m_prevFrequency;
    Int                     m_frequency;
    Int                     m_prevBrakeFrequency;
    Int                     m_brakeFrequency;
    Int                     m_prevSurfaceFrequency;
    Int                     m_surfaceFrequency;
    UInt                    m_laneWidth;
    Float                   m_relPos;
    Int                     m_panPos;
    Int                     m_currentSteering;
    Int                     m_currentThrottle;
    Int                     m_currentBrake;
    Int                     m_currentAcceleration;
    Int                     m_currentDeceleration;
    Int                     m_speedDiff;
    Int                     m_factor1;
    Double                  m_factor2;
    Char                    m_customFile[64];
    Boolean                 m_userDefined;
    // forcefeedback
    DirectX::ForceFeedbackEffect*   m_effectStart;
    DirectX::ForceFeedbackEffect*   m_effectCrash;
    DirectX::ForceFeedbackEffect*   m_effectSpring;
    DirectX::ForceFeedbackEffect*   m_effectGravel;
    DirectX::ForceFeedbackEffect*   m_effectEngine;
    DirectX::ForceFeedbackEffect*   m_effectCurbLeft;
    DirectX::ForceFeedbackEffect*   m_effectCurbRight;
    DirectX::ForceFeedbackEffect*   m_effectBumpLeft;
    DirectX::ForceFeedbackEffect*   m_effectBumpRight;
};



#endif // __RACING_CAR_H__
