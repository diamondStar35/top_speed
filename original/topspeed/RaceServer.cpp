/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "Game.h"
#include "Raceserver.h"
#include "RaceClient.h"
#include <Common/If/Algorithm.h>

RaceServer::RaceServer(Game* game) :
    m_game(game),
    m_server(0),
    m_raceStarted(false),
    m_finalizing(false),
    m_lastUpdateTime(0.0f),
    m_trackSelected(false)
{
    Mutex::Guard guard(m_mutex);
    RACE("(+) RaceServer");
    m_trackData.definition = NULL;
}


RaceServer::~RaceServer( )
{
    Mutex::Guard guard(m_mutex);
    RACE("(-) RaceServer");
    finalize( );
}

void
RaceServer::initialize( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceServer::initialize");
    if (m_server == 0)
    {
        m_server = new DirectX::Server( );
        m_server->setGUID(m_game->gameGuid());
        m_server->setIServer(this);
        char sessionName[64];
        sprintf(sessionName, "%d", m_game->raceSettings().serverNumber);
        m_server->startSession(sessionName, m_game->gamePort());
        m_playerMap.clear( );
    }
}

void
RaceServer::finalize( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceServer::finalize");
    m_finalizing = true;
    if (m_server)
    {
        m_server->stopSession( );
        SAFE_DELETE(m_server);
        m_playerMap.clear( );
    }
    resetTrack( );
    m_finalizing = false;
}


void
RaceServer::run(Float elapsed)
{
    Mutex::Guard guard(m_mutex);
    m_lastUpdateTime += elapsed;
    if (m_lastUpdateTime > SERVER_UPDATE_TIME)
    {
        TPlayerDataMap::iterator it;
        for (it = m_playerMap.begin( ); it != m_playerMap.end( ); ++it)
        {
            PlayerData player = (*it).second;
            if ((player.state != undefined) && (player.state != notReady))
            {
                PacketPlayerData packet;
                packet.command              = cmdPlayerData;
                packet.playerId             = player.id;
                packet.playerNumber         = player.playerNumber;
                packet.car                  = (CarType)player.car;
                packet.racedata.positionX   = player.posX;
                packet.racedata.positionY   = player.posY;
                packet.racedata.speed       = player.speed;
                packet.racedata.frequency       = player.frequency;
                packet.state                = (PlayerState)player.state;
                packet.engineRunning        = player.engineRunning;
                packet.braking              = player.braking;
                packet.horning              = player.horning;
                packet.backfiring           = player.backfiring;
                sendPacketToRacersExceptTo(player.id, &packet, sizeof(PacketPlayerData), false);
            }
            if (player.state == racing)
            {
                // check for bumps
                for (TPlayerDataMap::iterator it2 = m_playerMap.begin( ); it2 != m_playerMap.end( ); ++it2)
                {
                    if (it2 != it)
                    {
                        PlayerData player2 = (*it2).second;
                        if (player2.state == racing)
                        {
                            if ((absval<Int>(player.posX - player2.posX) < 1000) && (absval<Int>(player.posY - player2.posY) < 500))
                            {
                                PacketPlayerBumped packetBumped;
                                packetBumped.command        = cmdPlayerBumped;
                                packetBumped.playerId       = player.id;
                                packetBumped.playerNumber   = player.playerNumber;
                                packetBumped.bumpX = player.posX - player2.posX;
                                packetBumped.bumpY = player.posY - player2.posY;
                                packetBumped.bumpSpeed      = player.speed - player2.speed;
                                sendPacketTo(player.id, &packetBumped, sizeof(PacketPlayerBumped), true);
                            }
                        }
                    }
                }
            }
        }        
        m_lastUpdateTime = 0.0f;
    }
}

/*
void 
RaceServer::loadTrack(Char* trackname, UInt nrOfLaps)
{
    Mutex::Guard guard(m_mutex);
    PacketLoadTrack packet;
    packet.command = cmdLoadTrack;
    packet.nrOfLaps = nrOfLaps;
    strcpy(packet.trackname, trackname);
    sendPacket(&packet, sizeof(PacketLoadTrack), true);
}
*/

void
RaceServer::loadCustomTrack(Char* trackname)
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceServer::loadCustomTrack : sending track to all pending players, trackname = %s", trackname);
    PacketLoadCustomTrack packet;
    packet.command = cmdLoadCustomTrack;
    strcpy(m_track, trackname);
    if (strstr(_strlwr(m_track), "adv") == NULL)
        packet.nrOfLaps = (UByte)m_game->raceSettings().nrOfLaps;
    else
        packet.nrOfLaps = 1;
    Track* track = Track::readTrack(trackname);
    m_trackData.userDefined = track->userDefined( );
    if (!m_trackData.userDefined)
        strcpy(packet.trackname, trackname);
    else
        sprintf(packet.trackname, "custom");
    m_trackData.weather = track->weather( );
    packet.trackWeather = (UByte)track->weather( );
    m_trackData.ambience = track->ambience( );
    packet.trackAmbience = (UByte)track->ambience( );
    m_trackData.length = track->trackLength( );
    packet.trackLength = (UShort)track->trackLength( );
    SAFE_DELETE_ARRAY(m_trackData.definition);
    m_trackData.definition = new Track::Definition[track->trackLength()];
    Track::Definition* trackDefinition = track->definition();
    for (UInt i = 0; i < track->trackLength(); ++i)
    {
        m_trackData.definition[i] = trackDefinition[i];
        packet.trackDefinition[i].type = (UByte)trackDefinition[i].type;
        packet.trackDefinition[i].surface = (UByte)trackDefinition[i].surface;
        packet.trackDefinition[i].noise = (UByte)trackDefinition[i].noise;
        packet.trackDefinition[i].length = trackDefinition[i].length;
        if (i == MAXMULTITRACKLENGTH-1)
        {
           m_trackData.length = MAXMULTITRACKLENGTH;
           packet.trackLength = MAXMULTITRACKLENGTH;
           break;
        }
    }
    sendPacketToNotReady(&packet, sizeof(PacketLoadTrack) + (sizeof(MultiplayerDefinition) * track->trackLength()), true);
    m_trackSelected = true;
}

void 
RaceServer::sendDisconnect(Int id)
{
    Mutex::Guard guard(m_mutex);
    PacketGeneral packet;
    packet.command = cmdDisconnect;
    sendPacketTo(id, &packet, sizeof(PacketGeneral), true);
}


void
RaceServer::startRace( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceServer::startRace");
    PacketGeneral packet;
    packet.command = cmdStartRace;
    sendPacket(&packet, sizeof(PacketGeneral), true);
    m_nRaceResults = 0;
    for (UInt i = 0; i < NMAXPLAYERS; ++i)
        m_raceResults[i] = 0;
    m_raceStarted = true;
}

void
RaceServer::stopRace( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceServer::stopRace");
    m_raceStarted = false;
    resetTrack( );
    PacketRaceResults packet;
    packet.command = cmdStopRace;
    packet.nPlayers = (UByte)m_nRaceResults;
    for (UInt i = 0; i < m_nRaceResults; ++i)
        packet.result[i] = (UByte)m_raceResults[i];
    sendPacket(&packet, sizeof(PacketNPlayers) + (sizeof(UByte) * m_nRaceResults), true);
}

void
RaceServer::abortRace( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceServer::abortRace");
    PacketGeneral packet;
    packet.command = cmdRaceAborted;
    sendPacket(&packet, sizeof(PacketGeneral), true);
    m_raceStarted = false;

}

void 
RaceServer::sendPacket(PacketBase* packet, UInt size, Boolean secure)
{
    Mutex::Guard guard(m_mutex);
    packet->version = _TopSpeedVersion;
    TPlayerDataMap::iterator it;
    for (it = m_playerMap.begin( ); it != m_playerMap.end( ); ++it)
    {
        PlayerData player = (*it).second;
RACE("***** sending packet %d with command %d with a size of %d to player %d", packet, packet->command, size, player);
        m_server->sendPacket(player.id, (void*)packet, size, secure);
    }
}

void
RaceServer::sendPacketTo(UInt to, PacketBase* packet, UInt size, Boolean secure)
{
    Mutex::Guard guard(m_mutex);
    packet->version = _TopSpeedVersion;
RACE("***** sending packet command %d to: %d with a size of %d to player %d", packet->command, packet, size, to);
    m_server->sendPacket(to, (void*)packet, size, secure);
}

void
RaceServer::sendPacketExceptTo(UInt to, PacketBase* packet, UInt size, Boolean secure)
{
    Mutex::Guard guard(m_mutex);
    packet->version = _TopSpeedVersion;
    TPlayerDataMap::iterator it;
    for (it = m_playerMap.begin( ); it != m_playerMap.end( ); ++it)
    {
        PlayerData player = (*it).second;
        if (player.id != to)
{
RACE("***** sending packet with command %d except to %d, %d with a size of %d to player %d", packet->command, to, packet, size, player);
            m_server->sendPacket(player.id, (void*)packet, size, secure);
}
    }
}

void 
RaceServer::sendPacketToNotReady(PacketBase* packet, UInt size, Boolean secure)
{
    Mutex::Guard guard(m_mutex);
    packet->version = _TopSpeedVersion;
    TPlayerDataMap::iterator it;
    for (it = m_playerMap.begin( ); it != m_playerMap.end( ); ++it)
    {
        PlayerData player = (*it).second;
        if (player.state == notReady)
{
RACE("***** sending packet to notready %d, command %d with a size of %d to player %d", packet, packet->command, size, player);
            m_server->sendPacket(player.id, (void*)packet, size, secure);
}
    }
}

void 
RaceServer::sendPacketToRacers(PacketBase* packet, UInt size, Boolean secure)
{
    Mutex::Guard guard(m_mutex);
    packet->version = _TopSpeedVersion;
    TPlayerDataMap::iterator it;
    for (it = m_playerMap.begin( ); it != m_playerMap.end( ); ++it)
    {
        PlayerData player = (*it).second;
        if ((player.state == awaitingStart) || (player.state == racing) || (player.state == finished))
{
RACE("***** sending packet racers %d, command %d with a size of %d to player %d", packet, packet->command, size, player);
            m_server->sendPacket(player.id, (void*)packet, size, secure);
}
    }
}

void 
RaceServer::sendPacketToRacersExceptTo(UInt to, PacketBase* packet, UInt size, Boolean secure)
{
    Mutex::Guard guard(m_mutex);
    packet->version = _TopSpeedVersion;
    TPlayerDataMap::iterator it;
    for (it = m_playerMap.begin( ); it != m_playerMap.end( ); ++it)
    {
        PlayerData player = (*it).second;
        if ((player.id != to) && ((player.state == awaitingStart) || (player.state == racing) || (player.state == finished)))
{
RACE("***** sending packet with command %d to racers except to %d, %d with a size of %d to player %d", packet->command, to, packet, size, player);
            m_server->sendPacket(player.id, (void*)packet, size, secure);
}
    }
}

void
RaceServer::sendPlayerDisconnected(UInt player)
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceServer : sending a 'player %d disconnected' notification", player);
    PacketPlayer packet;
    packet.command = cmdPlayerDisconnected;
    PlayerData playerData = m_playerMap[player];
    packet.playerId = playerData.id;
    packet.playerNumber = playerData.playerNumber;
    sendPacketExceptTo(playerData.id, &packet, sizeof(PacketPlayer), true);
}

void
RaceServer::onPacket(UInt from, void* buffer, UInt size)
{
    Mutex::Guard guard(m_mutex);
    PacketBase* packet = static_cast<PacketBase*>(buffer);
RACE("***server received packet %d, command %d with a size of %d", packet, packet->command, size);
    switch (packet->command)
    {
    /*
    case cmdPlayerReadyToStart:
        {
            PacketPlayerReady* playerReady = static_cast<PacketPlayerReady*>(buffer);
            RACE("RaceServer::onPacket : received 'PlayerReadyToStart' from player %d", playerReady->playerNumber);
            PlayerData player = m_playerMap[from];
            player.state = readyToStart;
            player.car   = playerReady->car;
            player.posX  = playerReady->racedata.positionX;
            player.posY  = playerReady->racedata.positionY;
            player.speed = playerReady->racedata.speed;
            player.frequency = playerReady->racedata.frequency;
        }
        break;
        */
    case cmdPlayerDataToServer:
        {
            PacketPlayerData* playerData = static_cast<PacketPlayerData*>(buffer);
//            RACE("RaceServer::onPacket : PlayerDataToServer for player %d, posX=%d, posY=%d, speed=%d, frequency=%d)", m_playerMap[from].playerNumber, m_playerMap[from].posX, m_playerMap[from].posY, m_playerMap[from].speed, m_playerMap[from].frequency);
            m_playerMap[from].car           = playerData->car;
            m_playerMap[from].playerNumber  = playerData->playerNumber;
            m_playerMap[from].posX          = playerData->racedata.positionX;
            m_playerMap[from].posY          = playerData->racedata.positionY;
            m_playerMap[from].speed         = playerData->racedata.speed;
            m_playerMap[from].frequency         = playerData->racedata.frequency;
            m_playerMap[from].engineRunning = playerData->engineRunning;
            m_playerMap[from].braking       = playerData->braking;
            m_playerMap[from].horning       = playerData->horning;
            m_playerMap[from].backfiring    = playerData->backfiring;
        }
        break;
    case cmdPlayerState:
        {
            // Update the playerstate for this client
            PacketPlayerState* playerState = reinterpret_cast<PacketPlayerState*>(packet);
            if ((playerState->state == notReady) && (m_playerMap[from].state != notReady) && (m_trackSelected))
            {
                RACE("RaceServer::onPacket : sending track to player %d, trackname = %s", playerState->playerNumber, m_track);
                PacketLoadCustomTrack packetCustomTrack;
                packetCustomTrack.command = cmdLoadCustomTrack;
                if (strstr(_strlwr(m_track), "adv") == NULL)
                    packetCustomTrack.nrOfLaps = (UByte)m_game->raceSettings().nrOfLaps;
                else
                    packetCustomTrack.nrOfLaps = 1;
                if (!m_trackData.userDefined)
                    strcpy(packetCustomTrack.trackname, m_track);
                else
                    sprintf(packetCustomTrack.trackname, "custom");
                packetCustomTrack.trackWeather = (UByte)m_trackData.weather;
                packetCustomTrack.trackAmbience = (UByte)m_trackData.ambience;
                packetCustomTrack.trackLength = (UShort)m_trackData.length;
                for (UInt i = 0; i < m_trackData.length; ++i)
                {
                    packetCustomTrack.trackDefinition[i].type = (UByte)m_trackData.definition[i].type;
                    packetCustomTrack.trackDefinition[i].surface = (UByte)m_trackData.definition[i].surface;
                    packetCustomTrack.trackDefinition[i].noise = (UByte)m_trackData.definition[i].noise;
                    packetCustomTrack.trackDefinition[i].length = m_trackData.definition[i].length;
                    if (i == MAXMULTITRACKLENGTH-1)
                    {
                       packetCustomTrack.trackLength = MAXMULTITRACKLENGTH;
                       break;
                    }
                }
                sendPacketTo(from, &packetCustomTrack, sizeof(PacketLoadTrack) + (sizeof(MultiplayerDefinition) * m_trackData.length), true);
            }
            RACE("RaceServer::onPacket : updating the state for client %d from %d to %d", from, m_playerMap[from].state, playerState->state);
            m_playerMap[from].state         = playerState->state;
        }
        break;
    case cmdPlayerFinished:
        {
            PacketPlayer* playerFinished = static_cast<PacketPlayer*>(buffer);
            RACE("RaceServer::onPacket : received 'PlayerFinished' from player %d", playerFinished->playerNumber);
            m_raceResults[m_nRaceResults++] = playerFinished->playerNumber;
            sendPacketExceptTo(from, playerFinished, sizeof(PacketPlayer), true);
            if (nRacers() == 0)
                stopRace( );
        }
        break;
    case cmdPlayerFinalize:
        {
            PacketPlayerState* playerFinalize = static_cast<PacketPlayerState*>(buffer);
            RACE("RaceServer::onPacket : received 'PlayerFinalize' from player %d", playerFinalize->playerNumber);
            sendPacketExceptTo(from, playerFinalize, sizeof(PacketPlayerState), true);
            if (m_raceStarted)
            {
                PacketGeneral packet;
                packet.command = cmdStartRace;
                sendPacketTo(from, &packet, sizeof(PacketGeneral), true);
            }
        }
        break;
    case cmdPlayerStarted:
        {
            PacketPlayer* playerStarted = static_cast<PacketPlayer*>(buffer);
            RACE("RaceServer::onPacket : received 'PlayerStarted' from player %d", playerStarted->playerNumber);
            sendPacketExceptTo(from, playerStarted, sizeof(PacketPlayer), true);
        }
        break;
    case cmdPlayerCrashed:
        {
            PacketPlayer* playerCrashed = static_cast<PacketPlayer*>(buffer);
//            RACE("RaceServer::onPacket : received 'PlayerCrashed' from player %d", playerCrashed->playerNumber);
            sendPacketToRacersExceptTo(from, playerCrashed, sizeof(PacketPlayer), true);
        }
        break;
    default :
        break;
    }
}


void
RaceServer::onAddConnection(UInt id)
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceServer::onAddConnection : id = %d", id);
    PlayerData playerData;
    playerData.id     = id;
    playerData.playerNumber = 0;
    playerData.state  = notReady;
    playerData.car    = vehicle1;
    playerData.posX   = 0;
    playerData.posY   = 0;
    playerData.speed  = 0;
    playerData.frequency  = 22050;
    playerData.engineRunning = false;
    playerData.braking       = false;
    playerData.horning       = false;
    playerData.backfiring    = false;
    // find a free playernumber
    RACE("RaceServer::onAddConnection : searching free playernumber...");
    UInt i = 0;
    TPlayerDataMap::iterator it;
    for (i = 0; i < NMAXPLAYERS; ++i)
    {
        for (it = m_playerMap.begin( ); it != m_playerMap.end( ); ++it)
        {
            PlayerData player = (*it).second;
            if (player.playerNumber == i)
                break;
        }
        if (it == m_playerMap.end())
            break;
    }
    if (i == NMAXPLAYERS)
    {
        RACE("RaceServer::onAddConnection : no room");
        sendDisconnect(id);
        return;
    }
    RACE("RaceServer::onAddConnection : found free playernumber : %d", i);
    // if arrived here, a valid playernumber was found, it is contained in i
    playerData.playerNumber = (UByte)i;
    m_playerMap[id] = playerData;
    // send over the playernumber
    PacketPlayer packet;
    packet.command = cmdPlayerNumber;
    packet.playerId = id;
    packet.playerNumber = playerData.playerNumber;
    RACE("RaceServer::onAddConnection : sending playernumber %d", playerData.playerNumber);
    sendPacketTo(id, &packet, sizeof(PacketPlayer), true);
    RACE("RaceServer::onAddConnection : playernumber sent");
    if (m_trackSelected)
    {
        if (m_raceStarted)
        {
            PacketGeneral packet;
            packet.command = cmdStartRace;
            sendPacketTo(id, &packet, sizeof(PacketGeneral), true);
        }
        else
        {
            RACE("RaceServer::onPacket : sending track to player %d, trackname = %s", playerData.playerNumber, m_track);
            PacketLoadCustomTrack packetCustomTrack;
            packetCustomTrack.command = cmdLoadCustomTrack;
            if (strstr(_strlwr(m_track), "adv") == NULL)
                packetCustomTrack.nrOfLaps = (UByte)m_game->raceSettings().nrOfLaps;
            else
                packetCustomTrack.nrOfLaps = 1;
            if (!m_trackData.userDefined)
                strcpy(packetCustomTrack.trackname, m_track);
            else
                sprintf(packetCustomTrack.trackname, "custom");
            packetCustomTrack.trackWeather = (UByte)m_trackData.weather;
            packetCustomTrack.trackAmbience = (UByte)m_trackData.ambience;
            packetCustomTrack.trackLength = (UShort)m_trackData.length;
            for (UInt i = 0; i < m_trackData.length; ++i)
            {
                packetCustomTrack.trackDefinition[i].type = (UByte)m_trackData.definition[i].type;
                packetCustomTrack.trackDefinition[i].surface = (UByte)m_trackData.definition[i].surface;
                packetCustomTrack.trackDefinition[i].noise = (UByte)m_trackData.definition[i].noise;
                packetCustomTrack.trackDefinition[i].length = m_trackData.definition[i].length;
                if (i == MAXMULTITRACKLENGTH-1)
                {
                   packetCustomTrack.trackLength = MAXMULTITRACKLENGTH;
                   break;
                }
            }
            sendPacketTo(id, &packetCustomTrack, sizeof(PacketLoadTrack) + (sizeof(MultiplayerDefinition) * m_trackData.length), true);
        }
    }
}


void
RaceServer::onRemoveConnection(UInt id)
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceServer::onRemoveConnection : id = %d", id);
    if ((!m_finalizing) && (m_server != 0) && (m_playerMap.find(id) != m_playerMap.end( )))
    {
        sendPlayerDisconnected(id);
        m_playerMap.erase(id);
        if ((m_raceStarted) && (nRacers() == 0))
            stopRace( );
    }
}


void
RaceServer::onSessionLost( )
{
    Mutex::Guard guard(m_mutex);
    RACE("RaceServer::onSessionLost");
}

UInt
RaceServer::nRacers( )
{
    Mutex::Guard guard(m_mutex);
    UInt nRacers = 0;
    for (TPlayerDataMap::iterator it = m_playerMap.begin( ); it != m_playerMap.end( ); ++it)
    {
         PlayerData player = (*it).second;
         if (player.state == racing)
             ++nRacers;
    }
    return nRacers;
}

void
RaceServer::resetTrack( )
{
    Mutex::Guard guard(m_mutex);
    m_trackSelected = false;
	SAFE_DELETE_ARRAY(m_trackData.definition);
}
