/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_PACKETS_H__
#define __RACING_PACKETS_H__

#include <Common/If/Common.h>
#include "Track.h"

#define         NMAXPLAYERS     8
#define         MAXMULTITRACKLENGTH    8192

const UByte _TopSpeedVersion = 0x1E;

#pragma pack(push)
#pragma pack(1)

enum CarType
{
    vehicle1 = 0,
    vehicle2 = 1,
    vehicle3 = 2,
    vehicle4 = 3,
    vehicle5 = 4,
    vehicle6 = 5,
    vehicle7 = 6,
    vehicle8 = 7,
    vehicle9 = 8,
    vehicle10 = 9,
    vehicle11 = 10,
    vehicle12 = 11,
    customVehicle = 12
};

enum PlayerState
{
    undefined = 0,
    notReady = 1,
    awaitingStart = 2,
    // readyToStart = 3,
    racing = 3,
    finished = 4
};

struct PlayerData
{
    UInt           id;
    UByte          playerNumber;
    UByte          car;
    Int            posX;
    Int            posY;
    UShort         speed;
    Int            frequency;
    UByte          state;
    Boolean        engineRunning;
    Boolean        braking;
    Boolean        horning;
    Boolean        backfiring;
};


enum Command
{
    // cmdLoadTrack,
    cmdDisconnect,
    cmdPlayerNumber,
    // cmdPlayerReadyToStart,
    cmdPlayerData,
    cmdPlayerState,
    cmdStartRace,
    cmdStopRace,
    cmdRaceAborted,
    cmdPlayerDataToServer,
    cmdPlayerFinished,
    cmdPlayerFinalize,
    cmdPlayerStarted,
    cmdPlayerCrashed,
    cmdPlayerBumped,
    cmdPlayerDisconnected,
    cmdLoadCustomTrack
};



struct PlayerRaceData
{
    Int            positionX;
    Int            positionY;
    UShort         speed;    
    Int            frequency;    
};

struct MultiplayerDefinition
{
    UByte           type;
    UByte           surface;
    UByte           noise;
    UInt            length;
};

class PacketBase
{
public:
    UByte                version;
    UByte                command;
};


class PacketLoadTrack : public PacketBase
{
public:
    UByte           nrOfLaps;
    Char            trackname[12];
    UByte           trackWeather;
    UByte           trackAmbience;
    UShort          trackLength;
};

class PacketLoadCustomTrack : public PacketLoadTrack
{
public:
    MultiplayerDefinition trackDefinition[MAXMULTITRACKLENGTH];
};


class PacketGeneral : public PacketBase
{
};

class PacketPlayer : public PacketBase
{
public:
    UInt           playerId;
    UByte          playerNumber;
};

class PacketPlayerReady : public PacketPlayer
{
public:
    UByte           car;
    PlayerRaceData  racedata;
};

class PacketPlayerData : public PacketPlayer
{
public:
    UByte           car;
    PlayerRaceData  racedata;
    UByte           state;
    Boolean         engineRunning;
    Boolean         braking;
    Boolean         horning;
    Boolean         backfiring;
};

class PacketPlayerState : public PacketPlayer
{
public:
    UByte           state;
};

class PacketNPlayers : public PacketBase
{
public:
    UByte           nPlayers;
};

class PacketRaceResults : public PacketNPlayers
{
public:
    UByte           result[NMAXPLAYERS];
};


class PacketPlayerBumped : public PacketPlayer
{
public:
    Int             bumpX;
    Int             bumpY;
    UShort          bumpSpeed;
};

#pragma pack(pop)

#endif /* __RACING_PACKETS_H__ */
