/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_LEVELMULTIPLAYER_H__
#define __RACING_LEVELMULTIPLAYER_H__

#include "Game.h"
#include "Car.h"
#include "Track.h"
#include "NetworkPlayer.h"
#include "Packets.h"
#include "Level.h"


class LevelMultiplayer : public Level, public CarListener
{
public:
    LevelMultiplayer(Game* game, UInt nrOfLaps, Char* track, Track::TrackData trackData, Boolean automaticTransmission, UInt vehicle, Char* vehicleFile = NULL);
    virtual ~LevelMultiplayer( );

public:
    void initialize(Boolean isServer);
    void finalize( );

    void run(Float elapsed);

    Boolean started( )           { return m_started;            }
    Boolean manualTransmission( )  { return m_manualTransmission;   }
    void playerDisconnected(UInt player);
    UInt calculatePlayerPerc(UInt player);

public:
    virtual void onStart( );
    virtual void onCrash( );

private:
    // void    handleFinish( );
    void    comment(/* Float elapsed, */ Boolean automatic = true);
    void    updateResults( );

private:
    Boolean                 m_isServer;
    // Float                   m_lastLoadTrack;
    Float                   m_updateClient;
    NetworkPlayer           m_players[NMAXPLAYERS];
    DirectX::Sound*         m_soundYouAre;
    DirectX::Sound*         m_soundPlayer;
    DirectX::Sound*         m_soundPosition[NMAXPLAYERS];
    DirectX::Sound*         m_soundPlayerNr[NMAXPLAYERS];
    DirectX::Sound*         m_soundFinished[NMAXPLAYERS];
    DirectX::Sound*         m_soundVehicle[NVEHICLES];
    DirectX::Sound*         m_soundWaitingForPlayers;
    DirectX::Sound*         m_soundHasJoinedServer;
    DirectX::Sound*         m_soundHasJoinedRace;
    DirectX::Sound*         m_soundHasLeftServer;
    DirectX::Sound*         m_soundHasLeftRace;
    DirectX::Sound*         m_soundPressEnterToStart;
    DirectX::Sound*         m_soundRaceAborted;
    DirectX::Sound*         m_soundConnectedToServer;
    Float                   m_lastComment;
    UInt                    m_position;
    Boolean                 m_infoKeyReleased;
    Boolean                 m_startPressed;
};


#endif // __RACING_LEVELMULTIPLAYER_H__
