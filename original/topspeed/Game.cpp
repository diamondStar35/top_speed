/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#define _WIN32_WINNT 0x0501
#define _WIN32_DCOM
#include <objbase.h>


#include "Game.h"
#include "Menu.h"
#include "LevelTimeTrial.h"
#include "LevelSingleRace.h"
#include "LevelMultiplayer.h"
#include "RaceServer.h"
#include "RaceClient.h"
#include "RaceInput.h"
#include "resource.h"
#include "Common\If\Algorithm.h"


Tracer  _raceTracer("race");


Game::Game( ) :
    m_initialized(false),
    m_soundManager(0),
    m_raceInput(0),
    m_menu(0),
    m_levelTimeTrial(0),
    m_levelSingleRace(0),
    m_levelMultiplayer(0),
    m_state(menu),
    m_nextAutomaticTransmission(true),
    m_nextVehicle(0),
    m_nextVehicleFile(NULL),
    m_raceServer(0),
    m_raceClient(0),
    m_serverStarted(false),
    m_threeD(m_raceSettings.threeD),
    m_pauseKeyReleased(true)
{
    RACE("(+) Game");
    RACE("Game : initializing COM");
    HRESULT hres = CoInitializeEx(NULL, COINIT_MULTITHREADED);
    if (FAILED(hres))
    {
        RACE("Game : initializing COM failed with errorcode 0x%x", hres);
    }
}



Game::~Game( ) 
{
    RACE("(-) Game");
    SAFE_DELETE(m_nextVehicleFile);
    m_raceInput->finalize( );
    SAFE_DELETE(m_raceInput);
    SAFE_DELETE(m_raceServer);
    SAFE_DELETE(m_raceClient);
    SAFE_DELETE(m_menu);
    SAFE_DELETE(m_levelTimeTrial);
    SAFE_DELETE(m_levelSingleRace);
    SAFE_DELETE(m_levelMultiplayer);
    SAFE_DELETE(m_soundManager);
    SAFE_DELETE(m_inputManager);
    RACE("~Game : uninitializing COM");
    CoUninitialize();
    RACE("~Game : uninitialized COM");
}


void 
Game::initialize(::Window::Handle handle)
{
    RACE("Game::initialize : handle = %d", (UInt) handle);
    m_soundManager = new DirectX::SoundManager(handle, 2, 44100, 16);
    if (!m_raceSettings.hardwareAcceleration)
        m_soundManager->playInSoftware(true);
    m_soundManager->reverseStereo(m_raceSettings.reverseStereo);
    strcpy(m_language, m_raceSettings.language);
    m_inputManager = new DirectX::InputManager;
    m_inputManager->initialize(handle);
    m_raceInput = new RaceInput(this);
    m_raceInput->initialize( );
    m_raceServer = new RaceServer(this);
    m_raceClient = new RaceClient(this);

    Char numberSound[12];
    for (UInt i = 0; i <= 100; ++i)
    {
        sprintf(numberSound, "numbers\\%d", i);
        m_soundNumbers[i] = loadLanguageSound(numberSound);
    }

    m_timer.microElapsed( );
    m_currentTime = 0.0f;
    state(menu);        
    m_initialized = true;
}

void
Game::run( )
{
    if (m_initialized)
    {
        Huge helapsed = m_timer.microElapsed( );
        Float elapsed = helapsed / 1000000.0f;
        m_currentTime += elapsed;
        m_inputManager->update( );
        m_inputState = m_inputManager->state( );
        m_raceInput->run(m_inputState);
        switch (m_state)
        {
        case menu:
            if (m_menu)
                m_menu->run(elapsed);
                    if (m_raceClient->raceAborted( ))
                        m_raceClient->raceAborted(false);
            break;
        case quickStart:
        case singleRace:
            if (m_levelSingleRace)
            {
                m_levelSingleRace->run(elapsed);
                if (m_inputState.keys[DIK_ESCAPE])
                    state(menu);
            }
            break;
        case timeTrial:
            if (m_levelTimeTrial)
            {
                m_levelTimeTrial->run(elapsed);
                if (m_inputState.keys[DIK_ESCAPE])
                    state(menu);
            }
            break;
        case multiplayer:
            if (m_levelMultiplayer)
            {
                m_levelMultiplayer->run(elapsed);
                if (((m_inputState.keys[DIK_ESCAPE]) && (!m_serverStarted)) || (m_raceClient->sessionLost( )) || (m_raceClient->forceDisconnected( )) || (m_raceClient->raceAborted( )))
                {
                    if (m_raceClient->raceAborted( ))
                        m_raceClient->raceAborted(false);
                    state(menu);
                    break;
                }
                if ((m_inputState.keys[DIK_ESCAPE]) && (m_serverStarted))
                {
                    m_raceServer->abortRace( );
                    break;
                }
            }
            break;
        case awaitingGame:
                if ((m_inputState.keys[DIK_ESCAPE]) || (m_raceClient->sessionLost( )) || (m_raceClient->forceDisconnected( )))
            {
                state(menu);
                break;
            }
            if (m_raceClient->raceAborted( ))
                m_raceClient->raceAborted(false);
            if ((m_serverStarted) && (!m_raceServer->trackSelected( )))
            {
                m_raceServer->loadCustomTrack(m_nextTrack);
                // RACE("*** Server selected track! Track name = '%s'", m_raceServer->track( ));
                nextTrack(m_raceServer->track( ));
                nextTrackData(m_raceServer->trackData( ));
                if ((m_raceClient) && (m_raceClient->connected( )))
                {
                    state(multiplayer);
                    break;
                }
            }
            if (m_raceClient)
            {
                if ((!m_serverStarted) && (m_raceClient->connected( )) && (m_raceClient->trackSelected( )))
                {
                    // RACE("*** Client selected track! Track name = '%s'", m_raceClient->track( ));
                    nextTrack(m_raceClient->track( ));
                    nextTrackData(m_raceClient->trackData( ));
                    state(multiplayer);
                    break;
                }
                else if ((m_serverStarted) && (m_raceClient->connected( )) && (m_raceServer->trackSelected( )))
                {
                    state(multiplayer);
                    break;
                }
            }
            else
            {
                state(menu);
                break;            
            }
            break;            
        case paused:
            if ((!m_pauseKeyReleased) && (!m_raceInput->getPause( )))
            {
                m_pauseKeyReleased = true;
            }
            else if ((m_pauseKeyReleased) && (m_raceInput->getPause( )))
            {
                m_pauseKeyReleased = false;
                switch (m_pausedState)
                {
                    case timeTrial:
                        m_levelTimeTrial->unpause( );
                        m_levelTimeTrial->stopStopwatchDiff( );
                        break;
                    case quickStart:
                    case singleRace:
                        m_levelSingleRace->unpause( );
                        m_levelSingleRace->stopStopwatchDiff( );
                        break;
                }
                m_state = m_pausedState;
            }
            break;
        default:
            break;            
        }
        if (elapsed < 10.0f)
            ::Sleep(10);
        else if (elapsed < 20.0f)
            ::Sleep(10 - floatToDWORD(elapsed - 10.0f));
    }
}

void
Game::state(State state)
{
    switch (state)
    {
        case menu:
            if ((m_state == quickStart) || (m_state == timeTrial) || (m_state == singleRace) || (m_state == multiplayer))
            {
                if (m_levelTimeTrial)
                {
                    m_levelTimeTrial->finalize( );
                    SAFE_DELETE(m_levelTimeTrial);
                }
                else if (m_levelSingleRace)
                {
                    m_levelSingleRace->finalize( );
                    SAFE_DELETE(m_levelSingleRace);
                }
                else if (m_levelMultiplayer)
                {
                    m_levelMultiplayer->finalize( );
                    SAFE_DELETE(m_levelMultiplayer);
                }
            }
            m_menu = new Menu(this);
            switch (m_state)
            {
            case quickStart:
                m_menu->initialize(Menu::quickStart);
                break;
            case timeTrial:
                m_menu->initialize(Menu::timeTrial);
                break;
            case singleRace:
                m_menu->initialize(Menu::singleRace);
                break;
            case multiplayer:
                if (m_serverStarted)
                {
                    m_menu->initialize(Menu::multiHost);
                    break;
                }
                else if (!m_raceClient->connected( ))
                {
                    m_menu->initialize(Menu::multiplayer);
                    break;
                }
                else
                {
                    m_menu->initialize(Menu::multiJoin);
                    break;
                }
                break;
            case awaitingGame:
                if (m_serverStarted)
                {
                    m_menu->initialize(Menu::multiHost);
                    break;
                }
                else if (!m_raceClient->connected( ))
                {
                    m_menu->initialize(Menu::multiplayer);
                    break;
                }
                else
                {
                    m_menu->initialize(Menu::multiJoin);
                    break;
                }
            default:
                m_menu->initialize(Menu::none);
                m_menu->start( );
                break;
            }
            // reset timer
            m_timer.microElapsed( );
            m_currentTime = 0.0f;
            m_state = menu;
            break;
        case timeTrial:
            if (m_menu)
            {
                m_menu->finalize( );
                SAFE_DELETE(m_menu);
            }
            m_levelTimeTrial = new LevelTimeTrial(this, m_raceSettings.nrOfLaps, m_nextTrack, m_nextAutomaticTransmission, m_nextVehicle, m_nextVehicleFile);
            m_levelTimeTrial->initialize( );
            m_timer.microElapsed( );
            m_state = state;
            break;
        case quickStart:
        case singleRace:
            if (m_menu)
            {
                m_menu->finalize( );
                SAFE_DELETE(m_menu);
            }
            m_levelSingleRace = new LevelSingleRace(this, m_raceSettings.nrOfLaps, m_nextTrack, m_nextAutomaticTransmission, m_nextVehicle, m_nextVehicleFile);
            m_levelSingleRace->initialize(random(m_raceSettings.nrOfComputers+1));
            m_timer.microElapsed( );
            m_state = state;
            break;
        case multiplayer:
            if (m_menu)
            {
                m_menu->finalize( );
                SAFE_DELETE(m_menu);
            }
            UInt nrOfLaps;
            if (m_serverStarted)
                nrOfLaps = m_raceSettings.nrOfLaps;
            else
                nrOfLaps = m_raceClient->nrOfLaps( );
            m_levelMultiplayer = new LevelMultiplayer(this, nrOfLaps, m_nextTrack, m_nextTrackData, m_nextAutomaticTransmission, m_nextVehicle, m_nextVehicleFile);
            m_levelMultiplayer->initialize(m_serverStarted);
            m_timer.microElapsed( );
            m_state = state;
            break;
        case awaitingGame:
            // RACE("Game::awaitingGame");
            if (m_menu)
            {
                m_menu->finalize( );
                SAFE_DELETE(m_menu);
            }
            m_timer.microElapsed( );
            m_state = state;
            break;
        case paused:
            m_pausedState = m_state;
            switch (m_pausedState)
            {
                case timeTrial:
                    m_levelTimeTrial->startStopwatchDiff( );
                    m_levelTimeTrial->pause( );
                    break;
                case quickStart:
                case singleRace:
                    m_levelSingleRace->startStopwatchDiff( );
                    m_levelSingleRace->pause( );
                    break;
            }
            m_state = state;
            break;
        default:
            break;
    }
    // reset clock to cancel out load times
    m_timer.microElapsed( );
}


/*
void
Game::nextTrack(Char* track)
{
    strcpy(m_nextTrack, track);
}

void
Game::nextTrackData(Track::TrackData trackData)
{
    m_nextTrackData = trackData;
}
*/

void
Game::nextVehicle(UInt vehicle, Char* vehicleFile)
{
    m_nextVehicle = vehicle;
    if (vehicleFile != NULL)
    {
        m_nextVehicleFile = new Char[128];
        strcpy(m_nextVehicleFile, vehicleFile);
    }
    else
    {
        m_nextVehicleFile = NULL;
    }
}


/*
void
Game::nextAutomaticTransmission(Boolean b)
{
    m_nextAutomaticTransmission = b;
}
*/


Boolean
Game::startClient( )
{
    m_raceClient->initialize( );
    return true;
}

void
Game::stopClient( )
{
    m_raceClient->finalize( );
}

/*
void
Game::playerConnected(playerNr)
{
//    if(m_state == multiPlayer)
//        m_levelMultiplayer->playerConnected(playerNr);
}
*/

void
Game::playerDisconnected(UByte playerNr)
{
	if(m_state == multiplayer)
        m_levelMultiplayer->playerDisconnected(playerNr);
}

void
Game::startEnumSessions(char* ipAddress)
{
    m_raceClient->startEnumSessions(ipAddress);
}


void
Game::stopEnumSessions( )
{
    m_raceClient->stopEnumSessions( );
}


UInt
Game::joinSession(UInt session)
{
    RACE("Game::joinSession %d", session);
    return m_raceClient->joinSession(session);
}

UInt
Game::joinSessionAt(char* ipAddress)
{
    RACE("Game::joinSessionAt %s", ipAddress);
    return m_raceClient->joinSessionAt(ipAddress);
}

    
Boolean
Game::startServer( )
{
    m_raceServer->initialize( );
    m_serverStarted = true;
    return true;
}


void
Game::stopServer( )
{
    m_raceServer->finalize( );
    m_serverStarted = false;
}


Boolean 
Game::started( )
{
    switch (m_state)
    {
    case timeTrial:
        return m_levelTimeTrial->started( );
        break;
    case quickStart:
    case singleRace:
        return m_levelSingleRace->started( );
        break;
    case multiplayer:
        return m_levelMultiplayer->started( );
        break;
    default:
        return false;
        break;
    }
}

void 
Game::language(Char* l)
{
    if (strlen(l) < 64)
        strcpy(m_language, l);
}

DirectX::Sound* 
Game::loadLanguageSound(Char* file, Boolean threeD, Boolean ignoreNonexistence)
{
    Char filename[128];
    #ifdef _USE_WAV_
        sprintf(filename, "Sounds\\%s\\%s.wav", m_language, file);
        DirectX::Sound* result = m_soundManager->create(filename, threeD);
    #else
        sprintf(filename, "Sounds\\%s\\%s.ogg", m_language, file);
        DirectX::Sound* result = m_soundManager->createVorbis(filename, threeD);
    #endif
    if (!ignoreNonexistence)
    {
        if (result == 0)
        {
            sprintf(filename, "Sounds\\en\\%s.ogg", file);
            result = m_soundManager->createVorbis(filename, threeD);
        }
        if (result == 0)
        {
            RACE("(!) Game::loadLanguageSound : failed to load %s.ogg", file);
            result = m_soundManager->create(IDR_ERROR, threeD);
        }
    }
    return result;
}


/*
void
Game::resetTimer( )
{
    // reset timer
    m_timer.microElapsed( );
}
*/
