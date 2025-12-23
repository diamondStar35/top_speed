/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_LEVELSINGLERACE_H__
#define __RACING_LEVELSINGLERACE_H__

#include "Game.h"
#include "Car.h"
#include "Track.h"
#include "ComputerPlayer.h"
#include "Level.h"


#define NCOMPUTERPLAYERS    7

class LevelSingleRace : public Level
{
public:
    LevelSingleRace(Game* game, UInt nrOfLaps, Char* track, Boolean automaticTransmission, UInt vehicle = 0, Char* vehicleFile = NULL);
    virtual ~LevelSingleRace( );

public:
    void initialize(Int playerNumber);
    void finalize( );

    void run(Float elapsed);

    Boolean started( )           { return m_started;            }
    Boolean manualTransmission( )  { return m_manualTransmission;   }
public:
    void pause( );
    void unpause( );
    UInt calculatePlayerPerc(UInt player);


private:
    void    handleFinish( );
    void    updatePositions( );    
    void    comment(/* Float elapsed, */ Boolean automatic = true);
    void    checkForBumps( );
    Boolean checkFinish( );
    ComputerPlayer* generateRandomPlayer(int playerNumber);

private:
    UInt                    m_playerNumber;
    UInt                    m_nComputerPlayers;
    Int                     m_position;
    UInt                    m_positionComment;
    Int                     m_positionFinish;
    ComputerPlayer*         m_computerPlayer[NCOMPUTERPLAYERS];
    Float                   m_lastComment;
    Boolean                 m_infoKeyReleased;
    DirectX::Sound*         m_soundYouAre;
    DirectX::Sound*         m_soundPlayer;
    DirectX::Sound*         m_soundPosition[NCOMPUTERPLAYERS+1];
    DirectX::Sound*         m_soundPlayerNr[NCOMPUTERPLAYERS+1];
    DirectX::Sound*         m_soundFinished[NCOMPUTERPLAYERS+1];
    DirectX::Sound*         m_soundVehicle[NCOMPUTERPLAYERS+1];

protected:
    Menu*                   m_menu;
};


#endif // __RACING_LEVELSINGLERACE_H__
