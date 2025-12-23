/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "Game.h"
#include "RaceClient.h"
#include "Packets.h"

RaceClient::RaceClient(Game* game) :
    m_game(game),
    m_client(0),
    m_trackSelected(false),
    m_nrOfLaps(3),
    m_playerNumber(0),
    m_playerId(0),
    m_connected(false),
    m_raceAborted(false),
    m_startRace(false),
    m_playerBumped(false),
    m_prevPlayerState(undefined),
    m_playerState(undefined)
{
    Mutex::Guard guard(m_mutex);
    RACE("(+) RaceClient");
    m_trackData.definition = NULL;
}


RaceClient::~RaceClient( )
{
    Mutex::Guard guard(m_mutex);
    RACE("(-) RaceClient");
    finalize( );
}

void
RaceClient::initialize( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceClient::initialize");
    if (m_client == 0)
    {
        m_client = new DirectX::Client( );
        m_client->setGUID(m_game->gameGuid());
        m_client->setIClient(this);
        m_client->initialize( );
    }
    m_trackSelected = false;
    // state flags
    m_connected = false;
    m_startRace = false;
    m_playerBumped = false;
    m_sessionLost = false;
    m_forceDisconnected = false;
    m_raceAborted = false;
    // reset player data
    for (UInt player = 0; player < NMAXPLAYERS; ++player)
    {
        m_playerData[player].id    = 0;
        m_playerData[player].posX  = 0;
        m_playerData[player].posY  = 0;
        m_playerData[player].speed = 0;
        m_playerData[player].frequency = 22050;
        m_playerData[player].state = undefined;
        m_playerData[player].engineRunning = false;
        m_playerData[player].braking = false;
        m_playerData[player].horning = false;
        m_playerData[player].backfiring = false;
        m_playerFinished[player] = false;
        m_playerFinalize[player] = false;
        m_playerStarted[player] = false;
        m_playerCrashed[player] = false;
    }
    m_playerState = undefined;
    resetResults( );
}

void
RaceClient::finalize( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceClient::finalize");
    if (m_client)
    {
        m_client->finalize( );
        SAFE_DELETE(m_client);
    }
    m_trackSelected = false;
	SAFE_DELETE_ARRAY(m_trackData.definition);
    // state flags
    m_connected = false;
    m_startRace = false;
    m_playerBumped = false;
    m_sessionLost = false;
    m_forceDisconnected = false;
    m_raceAborted = false;
}


void
RaceClient::startEnumSessions(char* ipAddress)
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceClient::startEnumSessions");
    if (m_client)
    {
        m_client->startSessionEnum(m_game->gamePort(), ipAddress);
    }
}


void
RaceClient::stopEnumSessions( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceClient::stopEnumSessions");
    if (m_client)
    {
        m_client->stopSessionEnum( );
    }
}

UInt
RaceClient::nSessions( )
{
    Mutex::Guard guard(m_mutex);
    UInt nSessions = m_client->nSessions( );
    RACE("RaceClient::nSessions : found %d sessions.", nSessions);
    return nSessions;
}

UInt 
RaceClient::session(UInt i, DirectX::Client::SessionInfo& info)
{
    Mutex::Guard guard(m_mutex);
    return m_client->session(i, info);
}

UInt 
RaceClient::joinSession(UInt session)
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceClient::joinSession : joining session %i", session);
    resetResults( );
    return m_client->joinSession(session);
}

UInt 
RaceClient::joinSessionAt(Char* ipAddress)
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceClient::joinSessionAt : joining session at %s...", ipAddress);
    return m_client->joinSessionAt(m_game->gamePort(), ipAddress);
}

Boolean
RaceClient::playerFinished(UInt player)
{
    Mutex::Guard guard(m_mutex);
    if (m_playerFinished[player])
    {
        RACE("RaceClient::playerFinished : player %d finished!", player);
        m_playerFinished[player] = false;
        return true;
    }
    else
        return false;
}

Boolean
RaceClient::playerFinalize(UInt player)
{
    Mutex::Guard guard(m_mutex);
    if (m_playerFinalize[player])
    {
//        RACE("RaceClient::playerFinalize : player %d", player);
        m_playerFinalize[player] = false;
        return true;
    }
    else
        return false;
}


Boolean
RaceClient::playerStarted(UInt player)
{
    Mutex::Guard guard(m_mutex);
    if (m_playerStarted[player])
    {
        RACE("RaceClient::playerStarted : player %d started!", player);
        m_playerStarted[player] = false;
        return true;
    }
    else
        return false;
}


Boolean
RaceClient::playerCrashed(UInt player)
{
    Mutex::Guard guard(m_mutex);
    if (m_playerCrashed[player])
    {
//        RACE("RaceClient::playerCrashed : player %d crashed!", player);
        m_playerCrashed[player] = false;
        return true;
    }
    else
        return false;
}

Boolean     
RaceClient::playerBumped(Int& bumpX, Int& bumpY, Int& bumpSpeed)
{
    Mutex::Guard guard(m_mutex);
    if (m_playerBumped)
    {
        bumpX = m_playerBumpX;
        bumpY = m_playerBumpY;
        bumpSpeed = m_playerBumpSpeed;
        m_playerBumped = false;
        return true;
    }
    else
        return false;
}


void
RaceClient::resetTrack( )
{
    Mutex::Guard guard(m_mutex);
RACE("RaceClient::resetTrack");
    m_trackSelected = false;

	SAFE_DELETE_ARRAY(m_trackData.definition);
}

/*
void 
RaceClient::playerReadyToStart(CarType car)
{
    Mutex::Guard guard(m_mutex);
//    RACE("RaceClient::playerReadyToStart");
    PacketPlayerReady packet;
    packet.command = cmdPlayerReadyToStart;
    packet.playerId = m_playerId;
    packet.playerNumber = m_playerNumber;
    packet.racedata.positionX = 0;
    packet.racedata.positionY = 0;
    packet.racedata.speed     = 0;
    packet.racedata.frequency     = 22050;
    packet.car                = car;    
    sendPacket(&packet, sizeof(PacketPlayerReady), true);
}
*/

void 
RaceClient::playerFinished( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceClient::playerFinished");
    PacketPlayer packet;
    packet.command = cmdPlayerFinished;
    packet.playerId = m_playerId;
    packet.playerNumber = (UByte)m_playerNumber;
    sendPacket(&packet, sizeof(PacketPlayer), true);
}

void 
RaceClient::playerFinalize( )
{
    Mutex::Guard guard(m_mutex);
//    RACE("RaceClient::playerFinalize");
    PacketPlayerState packet;
    packet.command = cmdPlayerFinalize;
    packet.playerId = m_playerId;
    packet.playerNumber = (UByte)m_playerNumber;
    packet.state = notReady;
    sendPacket(&packet, sizeof(PacketPlayerState), true);
}

void 
RaceClient::playerStarted( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceClient::playerStarted");
    PacketPlayer packet;
    packet.command = cmdPlayerStarted;
    packet.playerId = m_playerId;
    packet.playerNumber = (UByte)m_playerNumber;
    sendPacket(&packet, sizeof(PacketPlayer), true);
    m_playerStarted[m_playerNumber] = true;
}


void 
RaceClient::playerCrashed( )
{
    Mutex::Guard guard(m_mutex);
//    RACE("RaceClient::playerCrashed");
    PacketPlayer packet;
    packet.command = cmdPlayerCrashed;
    packet.playerId = m_playerId;
    packet.playerNumber = (UByte)m_playerNumber;
    sendPacket(&packet, sizeof(PacketPlayer), true);
}



void 
RaceClient::sendData(PlayerData data, Boolean secure)
{
    Mutex::Guard guard(m_mutex);
    if ((secure) || (m_playerData[m_playerNumber].car != data.car) || (m_playerData[m_playerNumber].posX != data.posX) || (m_playerData[m_playerNumber].posY != data.posY) || (m_playerData[m_playerNumber].speed != data.speed) || (m_playerData[m_playerNumber].frequency != data.frequency) || (m_playerData[m_playerNumber].engineRunning != data.engineRunning) || (m_playerData[m_playerNumber].braking != data.braking) || (m_playerData[m_playerNumber].horning != data.horning) || (m_playerData[m_playerNumber].backfiring != data.backfiring))
    {
        PacketPlayerData packet;
        packet.command              = cmdPlayerDataToServer;
        packet.playerId             = data.id;
        packet.playerNumber         = data.playerNumber;
        packet.car                  = data.car;
        packet.racedata.positionX   = data.posX;
        packet.racedata.positionY   = data.posY;
        packet.racedata.speed       = data.speed;
        packet.racedata.frequency       = data.frequency;
        // RACE("RaceClient::sendData : Sending packet with speed=%d and frequency=%d", packet.racedata.speed, packet.racedata.frequency);
        packet.state                = (UByte)m_playerState;
        packet.engineRunning        = data.engineRunning;
        packet.braking              = data.braking;
        packet.horning              = data.horning;
        packet.backfiring           = data.backfiring;
        sendPacket(&packet, sizeof(PacketPlayerData), secure);
        m_playerData[m_playerNumber].id = data.id;
        m_playerData[m_playerNumber].playerNumber = data.playerNumber;
        m_playerData[m_playerNumber].car = data.car;
        m_playerData[m_playerNumber].posX = data.posX;
        m_playerData[m_playerNumber].posY = data.posY;
        m_playerData[m_playerNumber].speed = data.speed;
        m_playerData[m_playerNumber].frequency = data.frequency;
        m_playerData[m_playerNumber].engineRunning = data.engineRunning;
        m_playerData[m_playerNumber].braking = data.braking;
        m_playerData[m_playerNumber].horning = data.horning;
        m_playerData[m_playerNumber].backfiring = data.backfiring;
    }
}


void
RaceClient::playerUpdateState( )
{
    Mutex::Guard guard(m_mutex);
    if (m_playerState == m_prevPlayerState)
        return;
    PacketPlayerState packet;
    packet.command          = cmdPlayerState;
    packet.playerId         = m_playerId;
    packet.playerNumber     = (UByte)m_playerNumber;
    packet.state            = (UByte)m_playerState;
    sendPacket(&packet, sizeof(PacketPlayerState), true);
}


UInt 
RaceClient::raceResults(UInt* &results)
{
    Mutex::Guard guard(m_mutex);
    results = m_results;
    return m_nResults;
}

void
RaceClient::onPacket(UInt from, void* buffer, UInt size)
{
    Mutex::Guard guard(m_mutex);
    //RACE("RaceClient::onPacket");
    if (size >= 0)
    {
        PacketBase* packet = reinterpret_cast<PacketBase*>(buffer);
        if (packet->version == _TopSpeedVersion)
        {
RACE("***client received packet %d, command %d with a size of %d", packet, packet->command, size);
            switch (packet->command)
            {
                /*
                case cmdLoadTrack :
                {
                    PacketLoadTrack* loadTrack = reinterpret_cast<PacketLoadTrack*>(packet);
                    // RACE("RaceClient::onPacket : received 'LoadTrack(%s)', nrOfLaps = %d", loadTrack->trackname, loadTrack->nrOfLaps);
                    m_nrOfLaps = loadTrack->nrOfLaps;
                    strcpy(m_track, loadTrack->trackname);
                    m_trackSelected = true;
                    break;
                }
                */
                case cmdDisconnect :
                    // We were disconnected
                    m_connected = false;
                    m_forceDisconnected = true;
                    break;
                case cmdRaceAborted :
                    // The race has been aborted
                    RACE("RaceClient::onPacket : The race has been aborted by the server host");
                    if (m_startRace)
                    {
                        m_startRace = false;
                    }
                    resetTrack( );
                    m_raceAborted = true;
                    break;
                case cmdPlayerNumber :
                {
                    // We received a player number! We're officially connected now
                    RACE("RaceClient::onPacket : received a playerNumber");
                    m_connected = true;
                    PacketPlayer* player = reinterpret_cast<PacketPlayer*>(packet);
                    RACE("RaceClient::onPacket : received playernumber %d and id %d", player->playerNumber, player->playerId);
                    m_playerId = player->playerId;
                    m_playerNumber = player->playerNumber;
                    break;
                }
                case cmdPlayerData :
                {
                    // Update the data for this client
                    PacketPlayerData* playerData = reinterpret_cast<PacketPlayerData*>(packet);
                    UInt player = playerData->playerNumber;
                    if (player < NMAXPLAYERS)
                    {
                        // RACE("RaceClient::onPacket : received playerdata for player %d, posX=%d, posY=%d, speed=%d, frequency=%d, state=%d)", playerData->playerNumber, playerData->racedata.positionX, playerData->racedata.positionY, playerData->racedata.speed, playerData->racedata.frequency, playerData->state);
                        m_playerData[player].car            = playerData->car;
                        m_playerData[player].playerNumber   = playerData->playerNumber;
                        m_playerData[player].id             = playerData->playerId;
                        m_playerData[player].posX           = playerData->racedata.positionX;
                        m_playerData[player].posY           = playerData->racedata.positionY;
                        m_playerData[player].speed          = playerData->racedata.speed;
                        m_playerData[player].frequency          = playerData->racedata.frequency;
                        m_playerData[player].state          = playerData->state;
                        m_playerData[player].engineRunning  = playerData->engineRunning;
                        m_playerData[player].braking        = playerData->braking;
                        m_playerData[player].horning        = playerData->horning;
                        m_playerData[player].backfiring     = playerData->backfiring;
                    }
                    break;
                }
                case cmdPlayerState :
                {
                    // Update the playerstate for this client
                    PacketPlayerState* playerState = reinterpret_cast<PacketPlayerState*>(packet);
                    UInt player = playerState->playerNumber;
                    if (player < NMAXPLAYERS)
                    {
                        RACE("RaceClient::onPacket : updating the state for client %d from %d to %d",
                            player, m_playerData[player].state, playerState->state);
                        m_playerData[player].state          = playerState->state;
                    }
                    break;
                }
                case cmdStartRace :
                    // The race has started!
                    RACE("RaceClient::onPacket : received a 'startRace' command");
                    // reset the results
                    resetResults( );
                    m_startRace = true;
                    break;
                case cmdStopRace :
                {
                    // The race has finished, update results
                    RACE("RaceClient::onPacket : received a 'stopRace' command");
                    if (m_startRace)
                    {
                        resetTrack( );
                        m_startRace = false;
                    }
else
{
                    PacketRaceResults* results = reinterpret_cast<PacketRaceResults*>(packet);
                    m_nResults = results->nPlayers;
                    for (UInt i = 0; i < m_nResults; ++i)
                        m_results[i] = results->result[i];
}
                    break;
                }
                case cmdPlayerFinished :
                {
                    PacketPlayer* playerFinished = static_cast<PacketPlayer*>(buffer);
                    RACE("RaceClient::onPacket : received 'PlayerFinished' from player %d", playerFinished->playerNumber);
                    m_playerFinished[playerFinished->playerNumber] = true;
                    break;
                }
                case cmdPlayerFinalize :
                {
                    PacketPlayerState* playerFinalize = static_cast<PacketPlayerState*>(buffer);
                    // RACE("RaceClient::onPacket : received 'PlayerFinalize' from player %d", playerFinalize->playerNumber);
                    m_playerData[playerFinalize->playerNumber].state = playerFinalize->state;
                    m_playerFinalize[playerFinalize->playerNumber] = true;
                    break;
                }
                case cmdPlayerStarted :
                {
                    PacketPlayer* playerStarted = static_cast<PacketPlayer*>(buffer);
                    RACE("RaceClient::onPacket : received 'PlayerStarted' from player %d", playerStarted->playerNumber);
                    m_playerStarted[playerStarted->playerNumber] = true;
                    break;
                }
                case cmdPlayerCrashed :
                {
                    PacketPlayer* playerCrashed = static_cast<PacketPlayer*>(buffer);
                    // RACE("RaceClient::onPacket : received 'PlayerCrashed' from player %d", playerCrashed->playerNumber);
                    m_playerCrashed[playerCrashed->playerNumber] = true;
                    break;
                }
                case cmdPlayerBumped :
                {
                    PacketPlayerBumped* playerBumped = static_cast<PacketPlayerBumped*>(buffer);
                    // RACE("RaceClient::onPacket : received 'PlayerBumped', bumpX = %d, bumpY = %d", playerBumped->bumpX, playerBumped->bumpY);
                    m_playerBumped = true;
                    m_playerBumpX = playerBumped->bumpX;
                    m_playerBumpY = playerBumped->bumpY;
                    m_playerBumpSpeed = playerBumped->bumpSpeed;
                    break;
                }
                case cmdPlayerDisconnected :
                {
                    PacketPlayer* player = static_cast<PacketPlayer*>(buffer);
                    RACE("RaceClient::onPacket : received 'PlayerDisconnected' for player %d", player->playerNumber);
                    m_playerData[player->playerNumber].state = undefined;
                    m_game->playerDisconnected((UInt)player->playerNumber);
                    break;
                }
                case cmdLoadCustomTrack :
                    if (!m_trackSelected)
                    {
                        PacketLoadCustomTrack* loadTrack = reinterpret_cast<PacketLoadCustomTrack*>(packet);
                        RACE("RaceClient::onPacket : received 'LoadCustomTrack(%s)', nrOfLaps = %d, trackLength = %d", loadTrack->trackname, loadTrack->nrOfLaps, loadTrack->trackLength);
                        m_nrOfLaps = loadTrack->nrOfLaps;
                        strcpy(m_track, loadTrack->trackname);
                        m_trackData.weather = (Track::Weather)loadTrack->trackWeather;
                        m_trackData.ambience = (Track::Ambience)loadTrack->trackAmbience;
                        m_trackData.length = loadTrack->trackLength;
                        SAFE_DELETE_ARRAY(m_trackData.definition);
                        m_trackData.definition = new Track::Definition[loadTrack->trackLength];
        				for (UInt i = 0; i < loadTrack->trackLength; ++i)
                        {
                            m_trackData.definition[i].type = (Track::Type)loadTrack->trackDefinition[i].type;
                            m_trackData.definition[i].surface = (Track::Surface)loadTrack->trackDefinition[i].surface;
                            m_trackData.definition[i].noise = (Track::Noise)loadTrack->trackDefinition[i].noise;
                            m_trackData.definition[i].length = loadTrack->trackDefinition[i].length;
                        }
                        m_trackSelected = true;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}


void
RaceClient::onSessionLost( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceClient::onSessionLost");
    m_connected = false;
    m_sessionLost = true;

//        m_sessionLost = false;
//        m_game->state(Game::menu);
//return;
}

void 
RaceClient::sendPacket(PacketBase* packet, UInt size, Boolean secure)
{
    packet->version = _TopSpeedVersion;
    m_client->sendPacket((void*) packet, size, secure);
}

void
RaceClient::resetResults( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceClient::resetResults");
    m_nResults = 0;
    for (UInt i = 0; i < NMAXPLAYERS; ++i)
        m_results[i] = 0;
}
