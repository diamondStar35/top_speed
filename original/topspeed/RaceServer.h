/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_RACESERVER_H__
#define __RACING_RACESERVER_H__

#include <DxCommon/If/Network.h>
#include <Common/If/Mutex.h>
#include "Packets.h"
#include <map>
#include "RaceClient.h"
#include "Track.h"

#define SERVER_UPDATE_TIME      0.1f


class Game;




class RaceServer : public DirectX::IServer
{
public:
    /*
    enum State
    {
        offline,
        awaitingPlayers,
        awaitingReady,
        running
    };
    */
public:
    RaceServer(Game* game);
    virtual ~RaceServer( );

public:
    void initialize( );
    void finalize( );
    void run(Float elapsed);

public:
    // void loadTrack(Char* trackname, UInt nrOfLaps);
	void loadCustomTrack(Char* trackname);
    void sendDisconnect(Int id);
    void startRace( );
    void stopRace( );
    void abortRace( );
    Boolean     trackSelected( )        { Mutex::Guard guard(m_mutex); return m_trackSelected;       }
    Char*       track( )                { Mutex::Guard guard(m_mutex); return m_track;               }
	Track::TrackData  trackData()		{ Mutex::Guard guard(m_mutex); return m_trackData;			}
    void resetTrack( );
private:
    void sendPacket(PacketBase* packet, UInt size, Boolean secure);
    void sendPacketTo(UInt to, PacketBase* packet, UInt size, Boolean secure);
    void sendPacketExceptTo(UInt to, PacketBase* packet, UInt size, Boolean secure);
    void sendPacketToNotReady(PacketBase* packet, UInt size, Boolean secure);
    void sendPacketToRacers(PacketBase* packet, UInt size, Boolean secure);
    void sendPacketToRacersExceptTo(UInt to, PacketBase* packet, UInt size, Boolean secure);
    void sendPlayerDisconnected(UInt player);

public:
    virtual void    onPacket(UInt from, void* buffer, UInt size);
    virtual void    onAddConnection(UInt id);
    virtual void    onRemoveConnection(UInt id);
    virtual void    onSessionLost( );
private:
    UInt        nRacers( );
private:
    typedef std::map<UInt, PlayerData>   TPlayerDataMap;
    Game*                           m_game;
    DirectX::Server*                m_server;
    Mutex                           m_mutex;
    TPlayerDataMap                  m_playerMap;
    Float                           m_lastUpdateTime;
    Boolean                         m_raceStarted;
    UInt                            m_raceResults[NMAXPLAYERS];
    UInt                            m_nRaceResults;
    Boolean                         m_finalizing;
    Boolean                         m_trackSelected;
    Char                            m_track[32];
    Track::TrackData	             m_trackData;
};




#endif /* __RACING_RACESERVER_H__ */
