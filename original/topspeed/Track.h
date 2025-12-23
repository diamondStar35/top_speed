/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_TRACK_H__
#define __RACING_TRACK_H__

#include "Common\If\Common.h"
#include "DxCommon\If\Common.h" 

class Game;

class Track
{
public:
    enum Type
    {
        straight        = 0,
        easyLeft        = 1,
        left            = 2,
        hardLeft        = 3,
        hairpinLeft     = 4,
        easyRight       = 5,
        right           = 6,
        hardRight       = 7,
        hairpinRight    = 8
    };

    enum Surface
    {
        asphalt         = 0,
        gravel          = 1,
        water           = 2,
        sand            = 3,
        snow            = 4
    };

    enum Noise
    {
        noNoise      = 0,
        crowd        = 1,
        ocean        = 2, 
        runway       = 3,
        clock        = 4,
        jet          = 5,
        thunder      = 6,
        pile         = 7,
        construction = 8,
        river        = 9,
        helicopter        = 10,
        owl        = 11
    };

    enum Weather
    {
        sunny           = 0,
        rain            = 1,
        wind            = 2,
        storm           = 3
    };

    enum Ambience
    {
        noAmbience      = 0,
        desert          = 1,
        airport         = 2
    };

    struct Definition
    {
        Type            type;
        Surface         surface;
        Noise           noise;
        UInt            length;
    };

    struct Road
    {
        Int             left;
        Int             right;
        Surface         surface;
        Type            type;
        UInt            length;
    };

    struct TrackData
    {
        Boolean userDefined;
        Weather		weather;
        Ambience		ambience;
        UInt		 length;
        Definition*  definition;
    };

public:
    Track(Char* trackName, TrackData data, Game* game);
    Track(Char* filename, Game* game);
    virtual ~Track( );

private:
    Track();

public:
    void initialize( );
    void finalize( );

    void        laneWidth(UInt laneWidth)   { m_laneWidth = laneWidth;     }
    UInt        laneWidth()   { return m_laneWidth;     }
    Definition* definition()		 { return m_definition;			}
    UInt        trackLength()					 { return m_length;				}
    Weather     weather( ) { return m_weather; }
    Ambience    ambience( ) { return m_ambience; }
    Boolean     userDefined( ) { return m_userDefined; }
    void        run(/* Float elapsed, */ Int position);
    Road        road(Int position);
    Road        roadComputer(Int position);
    Boolean     nextRoad(Road& road, Int position, Int speed);
    Int         roadAt(Int position);
    void        calculateNoiseLength( );
    UInt        lap(Int position)          { return (position/m_lapDistance) + 1; }
    // Int         number( )                  { return m_number;      }
    Char*       trackName( )               { return m_trackName;   }
    UInt        length( )                  { return m_lapDistance; }

public:
    static Track* readTrack(Char* filename);

private:
    Game*               m_game;
    Boolean             m_userDefined;
    // Int                 m_number;
    UInt                m_length;
    UInt                m_lapDistance;
    UInt                m_lapCenter;
    Definition*         m_definition;
    UInt                m_relPos;
    UInt                m_currentRoad;
    UInt                m_prevRelPos;
    UInt                m_laneWidth;
    UInt                m_callLength;
    Int                 m_lastCalled;
    Float               m_factor;
    UInt                m_noiseLength;
    Int                 m_noiseStartPos;
    Int                 m_noiseEndPos;
    Boolean             m_noisePlaying;
    DirectX::Sound*     m_soundCrowd;
    DirectX::Sound*     m_soundOcean;
    DirectX::Sound*     m_soundRain;
    DirectX::Sound*     m_soundWind;
    DirectX::Sound*     m_soundStorm;
    DirectX::Sound*     m_soundDesert;
    DirectX::Sound*     m_soundAirport;
    DirectX::Sound*     m_soundClock;
    DirectX::Sound*     m_soundAirplane;
    DirectX::Sound*     m_soundJet;
    DirectX::Sound*     m_soundThunder;
    DirectX::Sound*     m_soundPile;
    DirectX::Sound*     m_soundConstruction;
    DirectX::Sound*     m_soundRiver;
    DirectX::Sound*     m_soundHelicopter;
    DirectX::Sound*     m_soundOwl;
    Weather          m_weather;
    Ambience          m_ambience;
    Char                m_trackName[64];
};



#endif // __RACING_TRACK_H__
