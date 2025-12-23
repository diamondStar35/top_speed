/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_LEVEL_H__
#define __RACING_LEVEL_H__

#include "Game.h"
#include "Car.h"
#include "Track.h"
#include "Packets.h"
#include "Common/If/Algorithm.h"

#define NLAPS 16
#define NVEHICLES 12
#define NUNKEYS 12
#define ADVLANEWIDTH 8000

class Level
{
public:
    Level(Game* game, Char* track, Boolean automaticTransmission, UInt nrOfLaps, UInt vehicle, Char* vehicleFile = NULL);
	Level(Game* game, Char* track, Track::TrackData trackData, Boolean automaticTransmission, UInt nrOfLaps, UInt vehicle, Char* vehicleFile = NULL);
    virtual ~Level( );
    void startStopwatchDiff( ) { m_oldStopwatch = m_stopwatch.elapsed(false); }
    void stopStopwatchDiff( ) { m_stopwatchDiff += (m_stopwatch.elapsed(false) - m_oldStopwatch); }
    void fadeIn( );
    void fadeOut( );

public:
    enum RandomSound
    {
        easyLeft        = 0,
        left            = 1,
        hardLeft        = 2,
        hairpinLeft     = 3,
        easyRight       = 4,
        right           = 5,
        hardRight       = 6,
        hairpinRight    = 7,
        asphalt         = 8,
        gravel          = 9,
        water           = 10,
        sand            = 11,
        snow            = 12,
        finish          = 13,
        front           = 14,
        tail            = 15
    };

protected:
    void initializeLevel( );
    void finalizeLevel( );

protected:
    void sayTime(Int raceTime, Boolean detailed = true);
    void callNextRoad(Track::Road& nextRoad);
    void pushEvent(Event::Type type, Float time, DirectX::Sound* sound = 0);
    void speak(DirectX::Sound* sound, Boolean unKey = false);
    void loadRandomSounds(RandomSound pos, Char* temp);
    void flushPendingSounds( );

protected:
    Game*                   m_game;
    Car*                    m_car;
    Track*                  m_track;
    Boolean                 m_manualTransmission;
    UInt                    m_nrOfLaps;
    DirectX::Sound*         m_soundStart;
    DirectX::Sound*         m_soundLaps[NLAPS-1];
    DirectX::Sound*         m_soundBestTime;
    DirectX::Sound*         m_soundNewTime;
    DirectX::Sound*         m_soundYourTime;
    DirectX::Sound*         m_soundMinute;
    DirectX::Sound*         m_soundMinutes;
    DirectX::Sound*         m_soundSecond;
    DirectX::Sound*         m_soundSeconds;
    DirectX::Sound*         m_soundPoint;
    DirectX::Sound*         m_soundPercent;
    DirectX::Sound*         m_soundUnkey[NUNKEYS];
    DirectX::Sound*         m_soundTheme4;
    DirectX::Sound*         m_soundPause;
    DirectX::Sound*         m_soundUnpause;
    DirectX::Sound*         m_soundTrackName;
    DirectX::Timer          m_stopwatch;
    Float                   m_elapsedTotal;
    Int                     m_raceTime;
    UInt                    m_lap;
    Track::Road             m_currentRoad;
    EventList               m_eventList;
    Boolean                 m_started;
    Boolean                 m_finished;
    Boolean                 m_acceptPlayerInfo;
    Boolean                 m_acceptCurrentRaceInfo;
//    Int*                    m_highscores;
//    UInt                    m_highscoresSize;
//    Int                     m_highscoreChecksum;
    Int                     m_highscore;
    Float                   m_sayTimeLength;
    Float                   m_speakTime;
    UInt                    m_unkeyQueue;
    DirectX::Sound*         m_randomSounds[16][32];
    UInt                    m_totalRandomSounds[16];
    UInt                    m_oldStopwatch;
    UInt                    m_stopwatchDiff;
};


#endif // __RACING_LEVEL_H__
