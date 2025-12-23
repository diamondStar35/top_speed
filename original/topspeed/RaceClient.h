/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_RACECLIENT_H__
#define __RACING_RACECLIENT_H__

#include <DxCommon/If/Network.h>
#include <Common/If/Mutex.h>
#include "Packets.h"

class Game;
class Menu;
class RaceClient : public DirectX::IClient
{
public:
    RaceClient(Game* game);
    RaceClient(Menu* Menu);
    virtual ~RaceClient( );

public:
    void initialize( );
    void finalize( );
    void startEnumSessions(char* ipAddress = 0);
    void stopEnumSessions( );
    UInt nSessions( );
    UInt session(UInt i, DirectX::Client::SessionInfo& info);
    UInt joinSession(UInt session);
    UInt joinSessionAt(Char* ipAddress);
    void sendData(PlayerData data, Boolean secure = false);

    Boolean     trackSelected( )        { Mutex::Guard guard(m_mutex); return m_trackSelected;       }
    Char*       track( )                { Mutex::Guard guard(m_mutex); return m_track;               }
	Track::TrackData  trackData()		{ Mutex::Guard guard(m_mutex); return m_trackData;			}
    UInt        nrOfLaps( )             { Mutex::Guard guard(m_mutex); return m_nrOfLaps;            }
    void        resetTrack( );
    PlayerData  playerData(UInt player) { Mutex::Guard guard(m_mutex); return m_playerData[player];  }
    Boolean     connected( )            { Mutex::Guard guard(m_mutex); return m_connected;           }
    void        sessionLost(Boolean b)          { Mutex::Guard guard(m_mutex); m_sessionLost = b;         }
    Boolean     sessionLost( )          { Mutex::Guard guard(m_mutex); return m_sessionLost;         }
    void        forceDisconnected(Boolean b)          { Mutex::Guard guard(m_mutex); m_forceDisconnected = b;        }
    Boolean     forceDisconnected( )          { Mutex::Guard guard(m_mutex); return m_forceDisconnected;         }
    void        raceAborted(Boolean b)          { Mutex::Guard guard(m_mutex); m_raceAborted = b;         }
    Boolean     raceAborted( )          { Mutex::Guard guard(m_mutex); return m_raceAborted;         }
    Boolean     startRace( )            { Mutex::Guard guard(m_mutex); return m_startRace;           }
    void        startRace(Boolean b)    { Mutex::Guard guard(m_mutex); m_startRace = b;              }
    UInt        playerNumber( )         { Mutex::Guard guard(m_mutex); return m_playerNumber;        }
    UInt        playerId( )             { Mutex::Guard guard(m_mutex); return m_playerId;            }
    Boolean     playerFinished(UInt player);
    Boolean     playerFinalize(UInt player);
    Boolean     playerStarted(UInt player);
    Boolean     playerCrashed(UInt player);
    Boolean     playerBumped(Int& bumpX, Int& bumpY, Int& bumpSpeed);
    void        playerState(PlayerState state)  { Mutex::Guard guard(m_mutex); m_prevPlayerState = m_playerState; m_playerState = state;    }
    PlayerState playerState( )                  { Mutex::Guard guard(m_mutex); return m_playerState;     }

public:
    // void playerReadyToStart(CarType car);
    void playerFinished( );
    void playerFinalize( );
    void playerStarted( );
    void playerCrashed( );
    void playerUpdateState( );
    UInt raceResults(UInt* &results);
    void resetResults( );

public:
    virtual void    onPacket(UInt from, void* buffer, UInt size);
    virtual void    onSessionLost( );

    void sendPacket(PacketBase* packet, UInt size, Boolean secure);

private:
    UInt                m_playerNumber;
    UInt                m_playerId;
    Game*               m_game;
    Menu*               m_menu;
    DirectX::Client*    m_client;
    Mutex               m_mutex;
    Char                m_track[32];
    UInt                m_nrOfLaps;
    Boolean             m_trackSelected;
    Track::TrackData	m_trackData;
    PlayerData          m_playerData[NMAXPLAYERS];
    Boolean             m_playerFinished[NMAXPLAYERS];
    Boolean             m_playerFinalize[NMAXPLAYERS];
    Boolean             m_playerStarted[NMAXPLAYERS];
    Boolean             m_playerCrashed[NMAXPLAYERS];
    Boolean             m_playerBumped;
    Int                 m_playerBumpX;
    Int                 m_playerBumpY;
    Int                 m_playerBumpSpeed;
    UInt                m_nResults;
    UInt                m_results[NMAXPLAYERS];
    PlayerState         m_prevPlayerState;
    PlayerState         m_playerState;

    // state flags
    Boolean             m_connected;
    Boolean             m_startRace;
    Boolean             m_sessionLost;
    Boolean             m_forceDisconnected;
    Boolean             m_raceAborted;
};


#endif /* __RACING_RACECLIENT_H__ */
