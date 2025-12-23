/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_GAME_H__
#define __RACING_GAME_H__

#include <DxCommon\If\Common.h>
#include <Common\If\TList.h>

#include "RaceSettings.h"
#include "Track.h"

extern Tracer  _raceTracer;
#define  RACE _raceTracer.trace

struct Event
{
    enum Type
    {
        carStart,
        carComputerStart,
        carRestart,
        raceStart,
        raceFinish,
        playSound,
//        playSoundAndDelete,
//        deleteSound,
        acceptInput,
        playRadioSound,
        inGear,
        stopSessionEnum,
        stopSessionEnumAndJoin,
        playCurrentItem,
        serverRaceStart,
        startHorn,
        stopHorn,
        raceTimeFinalize,
        acceptPlayerInfo,
        acceptCurrentRaceInfo
    };
    Float           time;
    Type            type;
    DirectX::Sound* sound;
//    UInt* number;
    Event*          next;
    Event*          previous;
};


typedef TList<Event>  EventList;

class Menu;
class LevelTimeTrial;
class LevelSingleRace;
class LevelMultiplayer;
class RaceServer;
class RaceClient;
class RaceInput;


class Game
{
public:
    Game( );
    virtual ~Game( );

public:
    void initialize(::Window::Handle handle);
    void run( );

public:
    enum State
    {
        menu,
        starting,
        quickStart,
        timeTrial,
        singleRace,
        multiplayer,
        awaitingGame,
        paused
    };

public:
    void                   state(State state);
    State                  state( ) { return m_state; }
    void                   nextTrack(Char* track) { strcpy(m_nextTrack, track); }
	void				   nextTrackData(Track::TrackData trackData) { m_nextTrackData = trackData; }
    void                   nextVehicle(UInt vehicle, Char* vehicleFile = NULL);
    void                   nextAutomaticTransmission(Boolean b) { m_nextAutomaticTransmission = b; }
    DirectX::SoundManager* soundManager( )   { return m_soundManager; }
    DirectX::InputManager* inputManager( )   { return m_inputManager; }
    RaceInput*             raceInput( )      { return m_raceInput;    }
    RaceServer*            raceServer( )     { return m_raceServer;   }
    RaceClient*            raceClient( )     { return m_raceClient;   }
    RaceSettings&          raceSettings( )   { return m_raceSettings; }
    Float                  currentTime( )    { return m_currentTime;  }
    DirectX::Input::State& input( )          { return m_inputState;   }
    Boolean                started( );
    DirectX::Sound*        loadLanguageSound(Char* file, Boolean threeD = false, Boolean ignoreNonexistence = false);

public:
    void hardwareAcceleration(Boolean b)     { m_hardwareAcceleration = b; }    
    void reverseStereo(Boolean b)            { m_reverseStereo = b;        }
    Boolean threeD()                         { return m_threeD;        }
    void threeD(Boolean val)                 { m_threeD = val;        }
    void language(Char* l);


public:
    Boolean startClient( );
    void    stopClient( );
//    void    playerConnected(UByte playerNr);
    void    playerDisconnected(UByte playerNr);
    void    startEnumSessions(char* ipAddress = 0);
    void    stopEnumSessions( );
    UInt    joinSession(UInt session);
    UInt    joinSessionAt(char* ipAddress);
    
    Boolean startServer( );
    void    stopServer( );



public:
    GUID     gameGuid( )     { GUID gameGuid = {0xede9493e, 0x6ac8, 0x4f15, {0x8d, 0x1, 0x8b, 0x16, 0x32, 0x0, 0xb9, 0x66}}; return gameGuid; }
    UInt     gamePort( )     { return 25255; }

public:
    void    resetTimer( ) { m_timer.microElapsed( ); }
    void    pauseKeyReleased(Boolean b) { m_pauseKeyReleased = b; }
    Boolean pauseKeyReleased( ) { return m_pauseKeyReleased; }
    Boolean serverStarted( ) { return m_serverStarted; }

private:
    Boolean                         m_initialized;
    State                           m_state;
    DirectX::Timer                  m_timer;
    DirectX::SoundManager*          m_soundManager;
    DirectX::InputManager*          m_inputManager;
    RaceInput*                      m_raceInput;
    DirectX::Input::State           m_inputState;
    Float                           m_currentTime;
    Char                            m_nextTrack[256];
    Track::TrackData				m_nextTrackData;
    UInt                            m_nextVehicle;
    Char*                            m_nextVehicleFile;
    Boolean                         m_nextAutomaticTransmission;

    RaceSettings                    m_raceSettings;

    // levels, menu's
    Menu*                           m_menu;
    LevelTimeTrial*                 m_levelTimeTrial;
    LevelSingleRace*                m_levelSingleRace;
    LevelMultiplayer*               m_levelMultiplayer;

    // settings from configfile
    Boolean                         m_hardwareAcceleration;
    Boolean                         m_reverseStereo;
    Char                            m_language[64];

    // multiplayer
    RaceServer*                     m_raceServer;    
    RaceClient*                     m_raceClient;
    Boolean                         m_serverStarted;
    Boolean                         m_threeD;
    State                           m_pausedState;
    Boolean                         m_pauseKeyReleased;

    // numbers
public:
    DirectX::Sound*                 m_soundNumbers[101];
};



#endif // __RACING_GAME_H__
