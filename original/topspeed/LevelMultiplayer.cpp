/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "LevelMultiplayer.h"
#include "RaceServer.h"
#include "RaceClient.h"

LevelMultiplayer::LevelMultiplayer(Game* game, UInt nrOfLaps, Char* track, Track::TrackData trackData, Boolean automaticTransmission, UInt vehicle, Char* vehicleFile) :
    Level(game, track, trackData, automaticTransmission, nrOfLaps, vehicle, vehicleFile),
//    m_soundYouAre(0),
    m_soundPlayer(0),
    m_soundWaitingForPlayers(0),
    m_soundPressEnterToStart(0),
    m_position(1),
    m_lastComment(0.0f),
    m_updateClient(0.0f),
    m_soundRaceAborted(0)
{
    RACE("(+) LevelMultiplayer");
    m_game->raceClient()->playerState(notReady);
    m_game->raceClient()->playerUpdateState( );
//    UInt playerNr = m_game->raceClient()->playerNumber();
    Char filename[64];
    for (UInt i = 0; i < NMAXPLAYERS; ++i)
    {
        sprintf(filename, "race\\info\\youarepos%d", i+1);
        m_soundPosition[i] = m_game->loadLanguageSound(filename);
        sprintf(filename, "race\\info\\player%d", i+1);
        m_soundPlayerNr[i] = m_game->loadLanguageSound(filename);
        sprintf(filename, "race\\info\\finished%d", i+1);
        m_soundFinished[i] = m_game->loadLanguageSound(filename);
    }
    for (UInt i = 0; i < NVEHICLES; ++i)
    {
        sprintf(filename, "vehicles\\vehicle%d", i+1);
        m_soundVehicle[i] = m_game->loadLanguageSound(filename);
    }
    m_soundHasJoinedServer            = m_game->loadLanguageSound("race\\multiplayer\\hasjoinedtheserver");
    m_soundHasJoinedRace            = m_game->loadLanguageSound("race\\multiplayer\\hasjoinedthegame");
    m_soundHasLeftServer              = m_game->loadLanguageSound("race\\multiplayer\\haslefttheserver");
    m_soundHasLeftRace              = m_game->loadLanguageSound("race\\multiplayer\\haslefttherace");
    loadRandomSounds(front, "race\\info\\front");
    loadRandomSounds(tail, "race\\info\\tail");
//    if (playerNr < NMAXPLAYERS)
//    {
//        m_soundYouAre    = m_game->loadLanguageSound("race\\youare");
        m_soundPlayer    = m_game->loadLanguageSound("race\\player");
//        RACE("LevelMultiplayer : saying my name: Player %d", playerNr);
//        speak(m_soundYouAre);
//        speak(m_soundPlayer);
//        speak(m_game->m_soundNumbers[playerNr+1]);
//    }
    m_soundWaitingForPlayers    = m_game->loadLanguageSound("race\\multiplayer\\waitingforplayers");
    m_soundConnectedToServer = m_game->loadLanguageSound("menu\\connectedtoserver");
}


LevelMultiplayer::~LevelMultiplayer( )
{
    RACE("(-) LevelMultiplayer");
//    SAFE_DELETE(m_soundYouAre);
    SAFE_DELETE(m_soundPlayer);
    for (UInt i = 0; i < NMAXPLAYERS; ++i)
    {
        SAFE_DELETE(m_soundPosition[i]);
        SAFE_DELETE(m_soundPlayerNr[i]);
        SAFE_DELETE(m_soundFinished[i]);
    }
    for (UInt i = 0; i < NVEHICLES; ++i)
    {
        SAFE_DELETE(m_soundVehicle[i]);
    }
    SAFE_DELETE(m_soundWaitingForPlayers);
    if (m_soundPressEnterToStart)
    {
        SAFE_DELETE(m_soundPressEnterToStart);
    }
    if (m_soundRaceAborted)
        SAFE_DELETE(m_soundRaceAborted);
    SAFE_DELETE(m_soundConnectedToServer);
    SAFE_DELETE(m_soundHasJoinedServer);
    SAFE_DELETE(m_soundHasJoinedRace);
    SAFE_DELETE(m_soundHasLeftServer);
    SAFE_DELETE(m_soundHasLeftRace);
}


void
LevelMultiplayer::initialize(Boolean isServer)
{
    RACE("LevelMultiplayer::initialize");
    initializeLevel( );
    UInt playerNr = m_game->raceClient()->playerNumber(); 
    Int positionX = 0;
    if (playerNr % 2)
        positionX = 3000;
    else
        positionX = -3000;
    Int positionY = 14000 - (playerNr * 2000);
    m_car->position(positionX, positionY);
    m_car->listener(this);
    // m_lastLoadTrack = 0.0f;
    m_position = 1;
    m_isServer = isServer;
    speak(m_soundWaitingForPlayers);
    if (m_isServer)
    {
        // m_game->raceServer()->loadCustomTrack(m_track->trackName());
        m_soundPressEnterToStart    = m_game->loadLanguageSound("race\\multiplayer\\pressentertostart");
        RACE("LevelMultiplayer : waiting for players");
        speak(m_soundPressEnterToStart);
    }
    else
    {
        m_soundRaceAborted = m_game->loadLanguageSound("race\\multiplayer\\raceaborted");
    }
    PlayerData data;
    data.car = (UByte)m_car->carType( );
    data.id  = m_game->raceClient()->playerId( );
    data.playerNumber = (UByte)m_game->raceClient()->playerNumber( );
    data.posX = m_car->positionX( );
    data.posY = m_car->positionY( );
    data.speed = 0;
    data.frequency = m_car->frequency( );
    data.engineRunning = false;
    data.braking = false;
    data.horning = false;
    data.backfiring = false;
    data.state = awaitingStart;
    RACE("LevelMultiplayer::initialize : sending first time data, cartype = %d", m_car->carType( ));
    m_game->raceClient()->sendData(data, true);
    m_game->raceClient()->playerState(awaitingStart);
    m_game->raceClient()->playerUpdateState( );
    m_infoKeyReleased = false;
    m_startPressed = false;
}


void
LevelMultiplayer::finalize( )
{
    RACE("LevelMultiplayer::finalize");
    m_car->listener(0);
    if (m_isServer)
        m_game->raceServer()->resetTrack( );
    if (m_game->raceClient()->trackSelected( ))
    m_game->raceClient()->resetTrack( );
    // ugly, needs a check for current state
    /*
    PlayerData data;
    data.car = (UByte)m_car->carType( );
    data.id  = m_game->raceClient()->playerId( );
    data.playerNumber = (UByte)m_game->raceClient()->playerNumber( );
    data.posX = m_car->positionX( );
    data.posY = m_car->positionY( );
    data.speed = (UShort)m_car->speed( );
    data.frequency = m_car->frequency( );
    data.engineRunning = m_car->engineRunning( );
    data.braking = m_car->braking( );
    data.horning = m_car->horning( );
    data.backfiring = m_car->backfiring( );
    data.state = notReady;
    m_game->raceClient()->sendData(data, true);
    */
    m_game->raceClient()->playerState(notReady);
    m_game->raceClient()->playerUpdateState( );
    m_game->raceClient()->playerFinalize( );
    finalizeLevel( );
}


void
LevelMultiplayer::run(Float elapsed)
{
    if (m_isServer)
    {
        /*
        if ((!m_started) && (m_elapsedTotal - m_lastLoadTrack > 1.0f))
        {
            m_game->raceServer()->loadCustomTrack(m_track->trackName());
            m_lastLoadTrack = m_elapsedTotal;
        }
        */
        m_game->raceServer()->run(elapsed);

        if ((!m_started) && (m_elapsedTotal > (m_soundWaitingForPlayers->length( ) + m_soundPressEnterToStart->length( ))))
        {
            if ((m_game->input( ).b1) && (!m_startPressed))
            {
                m_game->raceServer()->startRace( );
                m_startPressed = true;
            }
        }
    }
    if (m_game->raceClient()->sessionLost())
    {
        flushPendingSounds( );
        RACE("Multiplayer::run : Server lost! In fact, this should not have happened...");
    }
    if ((m_game->raceClient()->raceAborted( )) && (!m_isServer))
    {
        flushPendingSounds( );
        RACE("Multiplayer::run : Race aborted!");
        m_soundRaceAborted = m_game->loadLanguageSound("race\\multiplayer\\raceaborted");
        m_soundRaceAborted->play( );
        ::Sleep(DWORD(m_soundRaceAborted->length( ) * 1000.0f));
        m_game->resetTimer( );
    }
    if (m_game->raceClient()->connected( ))
    {
        if (m_car->state( ) == Car::stopped)
        {
            // not yet started, start now
            m_car->start( );
        }

        // send our data to the server
        m_updateClient += elapsed;
        if (m_updateClient > SERVER_UPDATE_TIME)
        {
            m_updateClient = 0.0f;
            // did we bump other cars?
            Int bumpX, bumpY, bumpSpeed;
            if (m_game->raceClient()->playerBumped(bumpX, bumpY, bumpSpeed))
            {
                m_car->bump(bumpX, bumpY, bumpSpeed);
            }
            PlayerData data;
            data.car = (UByte)m_car->carType( );
            data.id  = m_game->raceClient()->playerId( );
            data.playerNumber = (UByte)m_game->raceClient()->playerNumber( );
            data.posX = m_car->positionX( );
            data.posY = m_car->positionY( );
            data.speed = (UShort)m_car->speed( );
            data.frequency = m_car->frequency( );
            data.engineRunning = m_car->engineRunning( );
            data.braking = m_car->braking( );
            data.horning = m_car->horning( );
            data.backfiring = m_car->backfiring( );
            m_game->raceClient()->sendData(data);
            // check whether we're finished
            updateResults( );
        }
        // update playerData 
        for (UInt player = 0; player < NMAXPLAYERS; ++player)
        {
            PlayerData playerData = m_game->raceClient()->playerData(player);
            if ((playerData.playerNumber != m_game->raceClient()->playerNumber()) && (playerData.state != undefined) && (playerData.state != notReady))
            {
                if (m_players[player].initialized())
                {
                    m_players[player].position(playerData.posX, playerData.posY);
                    if (playerData.state != finished)
                    {
                        m_players[player].speed(playerData.speed);
                        m_players[player].frequency(playerData.frequency);
                        m_players[player].engineRunning(playerData.engineRunning);
                        m_players[player].braking(playerData.braking);
                    }
                    m_players[player].horning(playerData.horning);
                    m_players[player].backfiring(playerData.backfiring);
                }
                else
                {
                    m_players[player].initialize(m_game, player, playerData.car, m_track->length( ), m_track->laneWidth());
                    m_players[player].position(playerData.posX, playerData.posY);
                    if (playerData.state != finished)
                    {
                        m_players[player].speed(playerData.speed);
                        m_players[player].frequency(playerData.frequency);
                        m_players[player].engineRunning(playerData.engineRunning);
                        m_players[player].braking(playerData.braking);
                    }
                    m_players[player].horning(playerData.horning);
                    m_players[player].backfiring(playerData.backfiring);
                    RACE("LevelMultiplayer : player %d has joined the game!", player+1);
speak(m_soundPlayer);
                    speak(m_game->m_soundNumbers[player+1]);
                    speak(m_soundHasJoinedRace);
                }
                // started?
                if (m_game->raceClient()->playerStarted(player))
                    m_players[player].start( );
                // crashed?
                if (m_game->raceClient()->playerCrashed(player))
                    m_players[player].crash( );
                m_players[player].run(elapsed, m_car->positionX(), m_car->positionY());
            }
//            if ((m_players[player].initialized()) && (playerData.state == undefined))
            if ((m_players[player].initialized()) && (playerData.state == notReady) && (m_lap <= m_nrOfLaps))
            {
                RACE("LevelMultiplayer : Player %d has left the game!", player);
                m_players[player].finalize( );
                speak(m_soundPlayer);
                speak(m_game->m_soundNumbers[player+1]);
                speak(m_soundHasLeftRace);
            }
            if ((m_players[player].initialized( )) && (playerData.state == finished) && (!m_players[player].finished( )))
            {
                if (!m_game->threeD( ))
                    m_players[player].quiet( );
                m_players[player].stop( );
                m_players[player].finished(true);
            }
        }
        if ((m_game->raceInput()->getCurrentGear( )) && (m_started) && (m_acceptCurrentRaceInfo) && (m_lap <= m_nrOfLaps))
        {
            m_acceptCurrentRaceInfo = false;
            Int gear = m_car->gear( );
            m_game->m_soundNumbers[gear]->play( );
            pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[gear]->length( ));
        }
        if ((m_game->raceInput()->getCurrentLapNr( )) && (m_started) && (m_acceptCurrentRaceInfo) && (m_lap <= m_nrOfLaps))
        {
            m_acceptCurrentRaceInfo = false;
            m_game->m_soundNumbers[m_lap]->play();
            pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[m_lap]->length( ));
        }
        if ((m_game->raceInput()->getCurrentRacePerc( )) && (m_started) && (m_acceptCurrentRaceInfo) && (m_lap <= m_nrOfLaps))
        {
            m_acceptCurrentRaceInfo = false;
            Float perc = ((Float)m_car->positionY( ) / ((Float)m_track->length( ) * m_nrOfLaps)) * 100.0f;
            UInt units = (UInt)perc;
            UInt decs = UInt((perc - (Float)units) * 100.0f);
            m_game->m_soundNumbers[units]->play( );
            if (decs > 0)
            {
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ), m_soundPoint);
                if (decs < 10)
                {
                    pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ), m_game->m_soundNumbers[0]);
                    pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ), m_game->m_soundNumbers[decs]);
                    pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ) + m_game->m_soundNumbers[decs]->length( ), m_soundPercent);
                    pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ) + m_game->m_soundNumbers[decs]->length( ) + m_soundPercent->length( ));
                }
                else
                {
                    if (decs % 10 == 0)
                        decs = decs/10;
                    pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ), m_game->m_soundNumbers[decs]);
                    pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[decs]->length( ), m_soundPercent);
                    pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[decs]->length( ) + m_soundPercent->length( ));
                }
            }
            else
            {
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ), m_soundPercent);
                pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPercent->length( ));
            }
        }
        if ((m_game->raceInput()->getCurrentLapPerc( )) && (m_started) && (m_acceptCurrentRaceInfo) && (m_lap <= m_nrOfLaps))
        {
            m_acceptCurrentRaceInfo = false;
            Float perc = (((Float)m_car->positionY( ) - (m_track->length( ) * (m_lap - 1))) / (Float)m_track->length( )) * 100.0f;
            UInt units = (UInt)perc;
            // UInt decs = UInt((perc - (Float)units) * 100.0f);
            m_game->m_soundNumbers[units]->play( );
            /* if (decs > 0)
            {
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ), m_soundPoint);
                if (decs < 10)
                {
                    pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ), m_game->m_soundNumbers[0]);
                    pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ), m_game->m_soundNumbers[decs]);
                    pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ) + m_game->m_soundNumbers[decs]->length( ), m_soundPercent);
                    pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ) + m_game->m_soundNumbers[decs]->length( ) + m_soundPercent->length( ));
                }
                else
                {
                    if (decs % 10 == 0)
                        decs = decs / 10;
                    pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ), m_game->m_soundNumbers[decs]);
                    pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[decs]->length( ), m_soundPercent);
                    pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[decs]->length( ) + m_soundPercent->length( ));
                }
            }
            else
            { */
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ), m_soundPercent);
                pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPercent->length( ));
            // }
        }
        if ((m_game->raceInput()->getCurrentRaceTime( )) && (m_started) && (m_acceptCurrentRaceInfo))
        {
            m_acceptCurrentRaceInfo = false;
            m_sayTimeLength = 0.0f;
            if (m_lap <= m_nrOfLaps)
                sayTime(m_stopwatch.elapsed(false), false);
            else
                sayTime(m_raceTime, false);
            pushEvent(Event::acceptCurrentRaceInfo, m_sayTimeLength);
        }
        // update comments
        m_lastComment += elapsed;
        if ((m_game->raceSettings().automaticInfo > 1) && (m_lastComment > 6.0f))
        {
            comment(/* elapsed */);
            m_lastComment = 0.0f;
        }
        if ((m_game->raceInput()->getRequestInfo( )) && (m_infoKeyReleased))
        {
            if (m_lastComment > 2.0f)
            {
                m_infoKeyReleased = false;
                comment(/* elapsed, */ false);
                m_lastComment = 0.0f;
            }  
        }
        else if ((!m_game->raceInput()->getRequestInfo( )) && (!m_infoKeyReleased))
        {
            m_infoKeyReleased = true;
        }
        // check for player info requests
        UInt player;
        if ((m_game->raceInput()->getPlayerInfo(player)) && (m_acceptPlayerInfo))
        {
            if (player != m_game->raceClient()->playerNumber( ))
            {
                if (m_players[player].initialized( ))
                {
                    m_acceptPlayerInfo = false;
                    UInt carType = m_players[player].carType( );
                    m_soundVehicle[carType]->play( );
                    pushEvent(Event::acceptPlayerInfo, m_soundVehicle[carType]->length( ));
                }
                else if (m_game->raceClient()->playerData(player).state == notReady)
                {
                    m_acceptPlayerInfo = false;
                    m_soundConnectedToServer->play( );
                    pushEvent(Event::acceptPlayerInfo, m_soundConnectedToServer->length( ));
                }
            }
            else
            {
                m_acceptPlayerInfo = false;
                UInt carType = m_car->carType( );
                m_soundVehicle[carType]->play( );
                pushEvent(Event::acceptPlayerInfo, m_soundVehicle[carType]->length( ));
            }
        }
        if ((m_game->raceInput()->getPlayerPosition(player)) && (m_acceptPlayerInfo) && (m_started) && ((m_players[player].initialized( )) || (player == m_game->raceClient()->playerNumber( ))))
        {
            m_acceptPlayerInfo = false;
            UInt perc = calculatePlayerPerc(player);
            m_game->m_soundNumbers[perc]->play( );
            pushEvent(Event::playSound, m_game->m_soundNumbers[perc]->length( ), m_soundPercent);
            pushEvent(Event::acceptPlayerInfo, m_game->m_soundNumbers[perc]->length( ) + m_soundPercent->length( ));
        }
        if ((m_game->raceInput()->getTrackName( )) && (m_acceptCurrentRaceInfo))
        {
            m_acceptCurrentRaceInfo = false;
            m_soundTrackName->play( );
            pushEvent(Event::acceptCurrentRaceInfo, m_soundTrackName->length( ));
        }
        if ((m_game->raceInput()->getPlayerNumber( )) && (m_acceptCurrentRaceInfo))
        {
            m_acceptCurrentRaceInfo = false;
            m_game->m_soundNumbers[m_game->raceClient()->playerNumber()+1]->play( );
            pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[m_game->raceClient()->playerNumber()+1]->length( ));
        }
        if (m_game->raceClient()->startRace( ))
        {
            m_game->raceClient()->startRace(false);
            m_game->raceClient()->playerState(racing);
            m_game->raceClient()->playerUpdateState( );
            pushEvent(Event::raceStart, 5.0f);
            flushPendingSounds( );
            m_soundStart->play( );
        }
    }

    // Handle events
    Event* e = 0;
    while (e = m_eventList.next(e))
    {
        if (e->time <= m_elapsedTotal)
        {
            switch (e->type)
            {
            case Event::carStart:
                m_car->start( );
                break;
            case Event::raceStart:
                m_raceTime = 0;
                // start stopwatch
                m_stopwatch.elapsed( );
                m_lap = 0;
                m_started = true;
                break;
            case Event::raceFinish:
                m_acceptCurrentRaceInfo = false;
                flushPendingSounds( );
                m_sayTimeLength = 0.0f;
                pushEvent(Event::playSound, m_sayTimeLength, m_soundYourTime);
                m_sayTimeLength += m_soundYourTime->length() + 0.5f;
                sayTime(m_raceTime);
                pushEvent(Event::raceTimeFinalize, m_sayTimeLength);
                break;
            case Event::playSound:
                if (e->sound)
                {
                    e->sound->reset( );
                    e->sound->play( );
                }
                break;
            /* case Event::playSoundAndDelete:
                if (e->sound)
                {
                    e->sound->play( );
                    pushEvent(Event::deleteSound, e->sound->length( ), e->sound);
                }
                break;
            case Event::deleteSound:
                if (e->sound)
                {
                    SAFE_DELETE(e->sound);
                }
                break; */
            case Event::serverRaceStart:
                if (m_isServer)
                    m_game->raceServer()->startRace( );
                break;
            case Event::raceTimeFinalize:
                m_sayTimeLength = 0.0f;
                delete e;
                e = 0;
                m_game->state(Game::menu);
                return;
             case Event::playRadioSound:
                --m_unkeyQueue;
                if (m_unkeyQueue == 0)
                    speak(m_soundUnkey[random(NUNKEYS)]);
                break;
            case Event::acceptPlayerInfo:
                m_acceptPlayerInfo = true;
                break;
            case Event::acceptCurrentRaceInfo:
                m_acceptCurrentRaceInfo = true;
                break;
            default:
                break;
            }
            m_eventList.purge(e);
            delete e;
            e = 0;
        }
    }

    m_car->run(elapsed);
    m_track->run(/* elapsed, */ m_car->positionY( ));
    Track::Road road = m_track->road(m_car->positionY( ));
    m_car->evaluate(road);
    Track::Road nextRoad;
    if (m_track->nextRoad(nextRoad, m_car->positionY( ), m_car->speed()))
    {
        callNextRoad(nextRoad);
    }
    if (m_track->lap(m_car->positionY( )) > m_lap)
    {
        m_lap = m_track->lap(m_car->positionY( ));
        if (m_lap > m_nrOfLaps)
        {
            speak(m_randomSounds[finish][random(m_totalRandomSounds[finish])], true);
            m_car->manualTransmission(false);
            m_raceTime = m_stopwatch.elapsed( );
            if (m_car->state() !=  Car::stopping)
            {
                m_game->raceClient()->playerState(finished);
                m_game->raceClient()->playerUpdateState( );
                m_game->raceClient()->playerFinished( );
            }
            m_car->quiet( );
            m_car->stop( );
            // handleFinish( );
        }
        else if ((m_game->raceSettings( ).automaticInfo) && (m_lap > 1) && (m_lap <= m_nrOfLaps))
            speak(m_soundLaps[m_nrOfLaps - m_lap], true);
    }
    // if (m_game->raceInput()->getFlush( )) 
        // flushPendingSounds( );
    // update elapsed time
    m_elapsedTotal += elapsed;
}

/*
void
LevelMultiplayer::handleFinish( )
{
    RACE("LevelMultiplayer::handleFinish");
    m_car->quiet( );
    m_game->raceClient()->playerState(finished);
    m_game->raceClient()->playerUpdateState( );
    m_game->raceClient()->playerFinished( );
}
*/


void
LevelMultiplayer::comment(/* Float elapsed, */ Boolean automatic)
{
    if ((!m_started) || (m_lap > m_nrOfLaps))
        return;
    UInt position = 1;
    Int inFront        = -1;
    Int inFrontDist    = 50000;
    Int onTail         = -1;
    Int onTailDist     = 50000;
    UInt nPlayers      = 1;
    for (UInt player = 0; player < NMAXPLAYERS; ++player)
    {
        if (m_players[player].initialized( ))
        {
            ++nPlayers;
            if (m_players[player].positionY( ) > m_car->positionY( ))
            {
                ++position;
                Int dist = m_players[player].positionY( ) - m_car->positionY( );
                if (dist < inFrontDist)
                {
                    inFront     = player;
                    inFrontDist = dist;
                }
            }
            else if (m_players[player].positionY( ) < m_car->positionY( ))
            {
                Int dist = m_car->positionY( ) - m_players[player].positionY( );
                if (dist < onTailDist)
                {
                    onTail      = player;
                    onTailDist  = dist;
                }
            }
        }
    }
    if ((automatic) && (position != m_position))
    // && (m_game->raceSettings().automaticInfo > 1))
    {
        // if (position != m_position)
        // {
            if (position == nPlayers)
            {
//                RACE("LevelMultiplayer : 'you're in last position'");
                speak(m_soundPosition[NMAXPLAYERS-1], true);
            }
            else
            {
//                RACE("LevelMultiplayer : 'you're in %d position'", position);
                speak(m_soundPosition[position-1], true);
            }
            m_position = position;
            return;
        // }
    }
    if (inFrontDist < onTailDist)
    {
        if (inFront != -1)
        {
            // if (m_players[inFront].initialized())
            // {
            RACE("Comment : player %d is in front of you", inFront+1);
//            speak(m_players[inFront].inFront( ));
//            m_players[inFront].sayInFront( );
            speak(m_soundPlayerNr[inFront], true);
            speak(m_randomSounds[front][random(m_totalRandomSounds[front])], true);
            return;
            // }
        }
    }
    else
    {
        if (onTail != -1)
        {
            // if (m_players[onTail].initialized())
            // {
            RACE("Comment : player %d is on your tail", onTail+1);
//            speak(m_players[onTail].onTail( ));
//            m_players[onTail].sayOnTail( );
            speak(m_soundPlayerNr[onTail], true);
            speak(m_randomSounds[tail][random(m_totalRandomSounds[tail])], true);
            return;
            // }
        }
    }
    if ((inFront == -1) && (onTail == -1) && (!automatic))
    {
        if (position == nPlayers)
        {
            RACE("LevelMultiplayer : 'you're in last position'");
            speak(m_soundPosition[NMAXPLAYERS-1], true);
        }
        else
        {
            RACE("LevelMultiplayer : 'you're in %d position'", position);
            speak(m_soundPosition[position-1], true);
        }
        m_position = position;
        return;
    }
}


void
LevelMultiplayer::updateResults( )
{
    UInt* results;
    UInt nResults = m_game->raceClient()->raceResults(results);
    if (nResults)
    {
        RACE("LevelMultiplayer::updateResults : race is finished, reading results...");
        Float totalTime = 4.0f;
        for (UInt i = 0; i < nResults; ++i)
        {
            pushEvent(Event::playSound, totalTime, m_soundPlayerNr[results[i]]);
            totalTime += m_soundPlayerNr[results[i]]->length( );
            if (i == nResults-1)
            {
                pushEvent(Event::playSound, totalTime, m_soundFinished[NMAXPLAYERS-1]);
                totalTime += m_soundFinished[NMAXPLAYERS-1]->length( );
                UInt randomNr = random(NUNKEYS);
                pushEvent(Event::playSound, totalTime, m_soundUnkey[randomNr]);
                totalTime += m_soundUnkey[randomNr]->length( );
            }
            else
            {
                pushEvent(Event::playSound, totalTime, m_soundFinished[i]);
                totalTime += m_soundFinished[i]->length( );
            }
        }
        m_game->raceClient()->resetResults( );
        pushEvent(Event::raceFinish, totalTime + 1.0f, 0);
    }
}


void
LevelMultiplayer::onStart( )
{
    RACE("LevelMultiplayer::onStart");
    m_game->raceClient( )->playerStarted( );
}


void
LevelMultiplayer::onCrash( )
{
    if (m_car->state() == Car::crashing)
    {
        RACE("LevelMultiplayer::onCrash");
        m_game->raceClient( )->playerCrashed( );
    }
}

UInt
LevelMultiplayer::calculatePlayerPerc(UInt player)
{
    UInt perc;
    if (player == m_game->raceClient()->playerNumber( ))
        perc = UInt(((Float)m_car->positionY( ) / ((Float)m_track->length( ) * m_nrOfLaps)) * 100.0f);    
    else
        perc = UInt(((Float)m_players[player].positionY( ) / ((Float)m_track->length( ) * m_nrOfLaps)) * 100.0f);    
    if (perc > 100)
        perc = 100;
    return perc;
}

void
LevelMultiplayer::playerDisconnected(UInt player)
{
    if (m_players[player].initialized())
        m_players[player].finalize( );
    speak(m_soundPlayer);
    speak(m_game->m_soundNumbers[player+1]);
    speak(m_soundHasLeftServer);
}
