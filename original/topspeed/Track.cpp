/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "Track.h"
#include "Game.h"
#include "resource.h"
#include "TrackDefs.h"

#define LANEWIDTH 15000
#define CALLLENGTH 3000
#define TYPES 9
#define SURFACES 5
#define NOISES 12
#define MINPARTLENGTH 5000

Track::Track() :
    m_userDefined(false),
    m_weather(sunny),
    m_ambience(noAmbience),
    m_soundCrowd(NULL),
    m_soundOcean(NULL),
    m_soundRain(NULL),
    m_soundWind(NULL),
    m_soundStorm(NULL),
    m_soundDesert(NULL),
    m_soundAirport(NULL),
    m_soundAirplane(NULL),
    m_soundClock(NULL),
    m_soundJet(NULL),
    m_soundThunder(NULL),
    m_soundPile(NULL),
    m_soundConstruction(NULL),
    m_soundRiver(NULL),
    m_soundHelicopter(NULL),
    m_soundOwl(NULL),
    m_lapDistance(0),
    m_lapCenter(0),
    m_currentRoad(0),
    m_relPos(0),
    m_lastCalled(0),
    m_factor(0),
    m_noiseLength(0),
    m_noiseStartPos(0),
    m_noiseEndPos(0),
    m_noisePlaying(false)
{
    RACE("(+) Track");
}

Track::Track(Char* trackName, TrackData data, Game* game) :
    m_game(game),
    m_laneWidth(LANEWIDTH),
    m_callLength(CALLLENGTH),
    m_relPos(0),
    m_weather(data.weather),
    m_ambience(data.ambience),
    m_soundCrowd(NULL),
    m_soundOcean(NULL),
    m_soundRain(NULL),
    m_soundWind(NULL),
    m_soundStorm(NULL),
    m_soundDesert(NULL),
    m_soundAirport(NULL),
    m_soundAirplane(NULL),
    m_soundClock(NULL),
    m_soundJet(NULL),
    m_soundThunder(NULL),
    m_soundPile(NULL),
    m_soundConstruction(NULL),
    m_soundRiver(NULL),
    m_soundHelicopter(NULL),
    m_soundOwl(NULL),
    m_userDefined(true),
    m_length(data.length),
    m_lapDistance(0),
    m_lapCenter(0),
    m_currentRoad(0),
    m_lastCalled(0),
    m_factor(0),
    m_noiseLength(0),
    m_noiseStartPos(0),
    m_noiseEndPos(0),
    m_noisePlaying(false)
{
    RACE("(+) Track : building custom track %s, length of track = %d", trackName, data.length);
    if (strlen(trackName) < 64)
    {
        strcpy(m_trackName, trackName);
    }
    else
    {
        strcpy(m_trackName, "");
    }
    m_definition = new Definition[m_length];
    for (UInt i = 0; i < m_length; ++i)
    {
		m_definition[i].type    = data.definition[i].type;
		m_definition[i].surface = data.definition[i].surface;
		m_definition[i].noise   = data.definition[i].noise;
		m_definition[i].length  = data.definition[i].length;
        // RACE("Track : building custom track %s, part %d: type=%d, surface=%d, noise=%d, length=%d", trackName, i+1, data.definition[i].type, data.definition[i].surface, data.definition[i].noise, data.definition[i].length);
    }
    m_soundCrowd        = m_game->soundManager( )->create(IDR_CROWD);
    m_soundOcean        = m_game->soundManager( )->create(IDR_OCEAN);
    if (m_weather == rain)
        m_soundRain     = m_game->soundManager( )->create(IDR_RAIN);
    else if (m_weather == wind)
        m_soundWind     = m_game->soundManager( )->create(IDR_WIND);
    else if (m_weather == storm)
        m_soundStorm    = m_game->soundManager( )->create(IDR_STORM);
    if (m_ambience == desert)
        m_soundDesert   = m_game->soundManager( )->create(IDR_DESERT);
    else if (m_ambience == airport)
        m_soundAirport   = m_game->soundManager( )->create(IDR_AIRPORT);
    m_soundAirplane     = m_game->soundManager( )->create(IDR_AIRPLANE);
    m_soundClock        = m_game->soundManager( )->create(IDR_CLOCK);
    m_soundJet          = m_game->soundManager( )->create(IDR_JET);
    m_soundThunder      = m_game->soundManager( )->create(IDR_THUNDER);
    m_soundPile         = m_game->soundManager( )->create(IDR_PILE);
    m_soundConstruction = m_game->soundManager( )->create(IDR_CONST);
    m_soundRiver        = m_game->soundManager( )->create(IDR_RIVER);
    m_soundHelicopter        = m_game->soundManager( )->create(IDR_HELICOPTER);
    m_soundOwl        = m_game->soundManager( )->create(IDR_OWL);
}

Track::Track(Char* filename, Game* game) :
    m_game(game),
    m_laneWidth(LANEWIDTH),
    m_callLength(CALLLENGTH),
    m_relPos(0),
    m_userDefined(false),
    m_weather(sunny),
    m_ambience(noAmbience),
    m_soundCrowd(NULL),
    m_soundOcean(NULL),
    m_soundRain(NULL),
    m_soundWind(NULL),
    m_soundStorm(NULL),
    m_soundDesert(NULL),
    m_soundAirport(NULL),
    m_soundAirplane(NULL),
    m_soundClock(NULL),
    m_soundJet(NULL),
    m_soundThunder(NULL),
    m_soundPile(NULL),
    m_soundConstruction(NULL),
    m_soundRiver(NULL),
    m_soundHelicopter(NULL),
    m_soundOwl(NULL),
    m_lapDistance(0),
    m_lapCenter(0),
    m_currentRoad(0),
    m_lastCalled(0),
    m_factor(0),
    m_noiseLength(0),
    m_noiseStartPos(0),
    m_noiseEndPos(0),
    m_noisePlaying(false)
{
    RACE("(+) Track : filename = %s", filename);
    if (strlen(filename) < 64)
    {
        strcpy(m_trackName, filename);
    }
    else
    {
        strcpy(m_trackName, "");
    }
    if (strcmp(filename, "america") == 0)
    {
        m_definition = _trAmerica;
        m_length     = sizeof(_trAmerica)/sizeof(Definition);
    }
    else if (strcmp(filename, "austria") == 0)
    {
        m_definition = _trAustria;
        m_length     = sizeof(_trAustria)/sizeof(Definition);
    }
    else if (strcmp(filename, "belgium") == 0)
    {
        m_definition = _trBelgium;
        m_length     = sizeof(_trBelgium)/sizeof(Definition);
    }
    else if (strcmp(filename, "brazil") == 0)
    {
        m_definition = _trBrazil;
        m_length     = sizeof(_trBrazil)/sizeof(Definition);
    }
    else if (strcmp(filename, "china") == 0)
    {
        m_definition = _trChina;
        m_length     = sizeof(_trChina)/sizeof(Definition);
    }
    else if (strcmp(filename, "england") == 0)
    {
        m_definition = _trEngland;
        m_length     = sizeof(_trEngland)/sizeof(Definition);
    }
    else if (strcmp(filename, "finland") == 0)
    {
        m_definition = _trFinland;
        m_length     = sizeof(_trFinland)/sizeof(Definition);
    }
    else if (strcmp(filename, "france") == 0)
    {
        m_definition = _trFrance;
        m_length     = sizeof(_trFrance)/sizeof(Definition);
    }
    else if (strcmp(filename, "germany") == 0)
    {
        m_definition = _trGermany;
        m_length     = sizeof(_trGermany)/sizeof(Definition);
    }
    else if (strcmp(filename, "ireland") == 0)
    {
        m_definition = _trIreland;
        m_length     = sizeof(_trIreland)/sizeof(Definition);
    }
    else if (strcmp(filename, "italy") == 0)
    {
        m_definition = _trItaly;
        m_length     = sizeof(_trItaly)/sizeof(Definition);
    }
    else if (strcmp(filename, "netherlands") == 0)
    {
        m_definition = _trNetherlands;
        m_length     = sizeof(_trNetherlands)/sizeof(Definition);
    }
    else if (strcmp(filename, "portugal") == 0)
    {
        m_definition = _trPortugal;
        m_length     = sizeof(_trPortugal)/sizeof(Definition);
    }
    else if (strcmp(filename, "russia") == 0)
    {
        m_definition = _trRussia;
        m_length     = sizeof(_trRussia)/sizeof(Definition);
    }
    else if (strcmp(filename, "spain") == 0)
    {
        m_definition = _trSpain;
        m_length     = sizeof(_trSpain)/sizeof(Definition);
    }
    else if (strcmp(filename, "sweden") == 0)
    {
        m_definition = _trSweden;
        m_length     = sizeof(_trSweden)/sizeof(Definition);
    }
    else if (strcmp(filename, "switserland") == 0)
    {
        m_definition = _trSwitserland;
        m_length     = sizeof(_trSwitserland)/sizeof(Definition);
    }
    else if (strcmp(filename, "advHills") == 0)
    {
        m_definition = _trAdvHills;
        m_length     = sizeof(_trAdvHills)/sizeof(Definition);
    }
    else if (strcmp(filename, "advCoast") == 0)
    {
        m_definition = _trAdvCoast;
        m_length     = sizeof(_trAdvCoast)/sizeof(Definition);
    }
    else if (strcmp(filename, "advCountry") == 0)
    {
        m_definition = _trAdvCountry;
        m_length     = sizeof(_trAdvCountry)/sizeof(Definition);
        m_weather = rain;
    }
    else if (strcmp(filename, "advAirport") == 0)
    {
        m_definition = _trAirport;
        m_length     = sizeof(_trAirport)/sizeof(Definition);
        m_ambience = airport;
    }
    else if (strcmp(filename, "advDesert") == 0)
    {
        m_definition = _trDesert;
        m_length     = sizeof(_trDesert)/sizeof(Definition);
        m_ambience = desert;
    }
    else if (strcmp(filename, "advRush") == 0)
    {
        m_definition = _trAdvRush;
        m_length     = sizeof(_trAdvRush)/sizeof(Definition);
    }
    else if (strcmp(filename, "advEscape") == 0)
    {
        m_definition = _trAdvEscape;
        m_length     = sizeof(_trAdvEscape)/sizeof(Definition);
        m_weather = wind;
    }
    else
    {
        m_userDefined = true;
        File file(filename, File::read);
        // Find out the length
        m_length = 0;
        while (file.readInt( ) >= 0)
        {
            file.readInt( );
            if (file.readInt( ) < MINPARTLENGTH)
                file.readInt( );
            ++m_length;
        }
        file.rewind( );
        RACE("Track : reading trackfile, length of track = %d", m_length);
        if (m_length == 0)
        {
            m_length = 1;
            m_definition = new Definition[1];
            m_definition[0].type = (Type)0;
            m_definition[0].surface = (Surface)0;
            m_definition[0].noise = (Noise)0;
            m_definition[0].length = MINPARTLENGTH;
            m_weather  = (Weather)0;
            m_ambience = (Ambience)0;
        }
        else
        {
            m_definition = new Definition[m_length];
            Int temp = 0;
            for (UInt i = 0; i < m_length; ++i)
            {
                m_definition[i].type    = (Type)file.readInt( );
                m_definition[i].surface = (Surface)file.readInt( );
                temp = file.readInt( );
                if (temp < NOISES)
                {
                    m_definition[i].noise   = (Noise)temp;
                    m_definition[i].length  = file.readInt( );
                }
                else
                {
                    if (m_definition[i].type >= TYPES)
                    {
                        m_definition[i].noise = (Noise)((m_definition[i].type - TYPES) + 1);
                        m_definition[i].type = (Type)0;
                    }
                    else
                    {
                        m_definition[i].noise = (Noise)0;
                    }
                    m_definition[i].length = temp;
                }
                if (m_definition[i].type >= TYPES)
                    m_definition[i].type = (Type)0;
                if (m_definition[i].surface >= SURFACES)
                    m_definition[i].surface = (Surface)0;
                if (m_definition[i].noise >= NOISES)
                    m_definition[i].noise = (Noise)0;
                if (m_definition[i].length < MINPARTLENGTH)
                    m_definition[i].length = MINPARTLENGTH;
            }
            file.readInt( ); // -1
            m_weather  = (Weather)file.readInt( );
            if (m_weather < 0)
                m_weather = (Weather)0;
            m_ambience = (Ambience)file.readInt( );
            if (m_ambience < 0)
                m_ambience = (Ambience)0;
        }
        RACE("Track : done reading trackfile");
    }
    m_soundCrowd        = m_game->soundManager( )->create(IDR_CROWD);
    m_soundOcean        = m_game->soundManager( )->create(IDR_OCEAN);
    if (m_weather == rain)
        m_soundRain     = m_game->soundManager( )->create(IDR_RAIN);
    else if (m_weather == wind)
        m_soundWind     = m_game->soundManager( )->create(IDR_WIND);
    else if (m_weather == storm)
        m_soundStorm    = m_game->soundManager( )->create(IDR_STORM);
    if (m_ambience == desert)
        m_soundDesert   = m_game->soundManager( )->create(IDR_DESERT);
    else if (m_ambience == airport)
        m_soundAirport   = m_game->soundManager( )->create(IDR_AIRPORT);
    m_soundAirplane     = m_game->soundManager( )->create(IDR_AIRPLANE);
    m_soundClock        = m_game->soundManager( )->create(IDR_CLOCK);
    m_soundJet          = m_game->soundManager( )->create(IDR_JET);
    m_soundThunder      = m_game->soundManager( )->create(IDR_THUNDER);
    m_soundPile         = m_game->soundManager( )->create(IDR_PILE);
    m_soundConstruction = m_game->soundManager( )->create(IDR_CONST);
    m_soundRiver        = m_game->soundManager( )->create(IDR_RIVER);
    m_soundHelicopter        = m_game->soundManager( )->create(IDR_HELICOPTER);
    m_soundOwl        = m_game->soundManager( )->create(IDR_OWL);
}

Track* 
Track::readTrack(Char* filename)
{
    Track* result = new Track();
    if (strcmp(filename, "america") == 0)
    {
        result->m_definition = _trAmerica;
        result->m_length     = sizeof(_trAmerica)/sizeof(Definition);
    }
    else if (strcmp(filename, "austria") == 0)
    {
        result->m_definition = _trAustria;
        result->m_length     = sizeof(_trAustria)/sizeof(Definition);
    }
    else if (strcmp(filename, "belgium") == 0)
    {
        result->m_definition = _trBelgium;
        result->m_length     = sizeof(_trBelgium)/sizeof(Definition);
    }
    else if (strcmp(filename, "brazil") == 0)
    {
        result->m_definition = _trBrazil;
        result->m_length     = sizeof(_trBrazil)/sizeof(Definition);
    }
    else if (strcmp(filename, "china") == 0)
    {
        result->m_definition = _trChina;
        result->m_length     = sizeof(_trChina)/sizeof(Definition);
    }
    else if (strcmp(filename, "england") == 0)
    {
        result->m_definition = _trEngland;
        result->m_length     = sizeof(_trEngland)/sizeof(Definition);
    }
    else if (strcmp(filename, "finland") == 0)
    {
        result->m_definition = _trFinland;
        result->m_length     = sizeof(_trFinland)/sizeof(Definition);
    }
    else if (strcmp(filename, "france") == 0)
    {
        result->m_definition = _trFrance;
        result->m_length     = sizeof(_trFrance)/sizeof(Definition);
    }
    else if (strcmp(filename, "germany") == 0)
    {
        result->m_definition = _trGermany;
        result->m_length     = sizeof(_trGermany)/sizeof(Definition);
    }
    else if (strcmp(filename, "ireland") == 0)
    {
        result->m_definition = _trIreland;
        result->m_length     = sizeof(_trIreland)/sizeof(Definition);
    }
    else if (strcmp(filename, "italy") == 0)
    {
        result->m_definition = _trItaly;
        result->m_length     = sizeof(_trItaly)/sizeof(Definition);
    }
    else if (strcmp(filename, "netherlands") == 0)
    {
        result->m_definition = _trNetherlands;
        result->m_length     = sizeof(_trNetherlands)/sizeof(Definition);
    }
    else if (strcmp(filename, "portugal") == 0)
    {
        result->m_definition = _trPortugal;
        result->m_length     = sizeof(_trPortugal)/sizeof(Definition);
    }
    else if (strcmp(filename, "russia") == 0)
    {
        result->m_definition = _trRussia;
        result->m_length     = sizeof(_trRussia)/sizeof(Definition);
    }
    else if (strcmp(filename, "spain") == 0)
    {
        result->m_definition = _trSpain;
        result->m_length     = sizeof(_trSpain)/sizeof(Definition);
    }
    else if (strcmp(filename, "sweden") == 0)
    {
        result->m_definition = _trSweden;
        result->m_length     = sizeof(_trSweden)/sizeof(Definition);
    }
    else if (strcmp(filename, "switserland") == 0)
    {
        result->m_definition = _trSwitserland;
        result->m_length     = sizeof(_trSwitserland)/sizeof(Definition);
    }
    else if (strcmp(filename, "advHills") == 0)
    {
        result->m_definition = _trAdvHills;
        result->m_length     = sizeof(_trAdvHills)/sizeof(Definition);
    }
    else if (strcmp(filename, "advCoast") == 0)
    {
        result->m_definition = _trAdvCoast;
        result->m_length     = sizeof(_trAdvCoast)/sizeof(Definition);
    }
    else if (strcmp(filename, "advCountry") == 0)
    {
        result->m_definition = _trAdvCountry;
        result->m_length     = sizeof(_trAdvCountry)/sizeof(Definition);
        result->m_weather = rain;
    }
    else if (strcmp(filename, "advAirport") == 0)
    {
        result->m_definition = _trAirport;
        result->m_length     = sizeof(_trAirport)/sizeof(Definition);
        result->m_ambience = airport;
    }
    else if (strcmp(filename, "advDesert") == 0)
    {
        result->m_definition = _trDesert;
        result->m_length     = sizeof(_trDesert)/sizeof(Definition);
        result->m_ambience = desert;
    }
    else if (strcmp(filename, "advRush") == 0)
    {
        result->m_definition = _trAdvRush;
        result->m_length     = sizeof(_trAdvRush)/sizeof(Definition);
    }
    else if (strcmp(filename, "advEscape") == 0)
    {
        result->m_definition = _trAdvEscape;
        result->m_length     = sizeof(_trAdvEscape)/sizeof(Definition);
        result->m_weather = wind;
    }
    else
    {
        result->m_userDefined = true;
        File file(filename, File::read);
        // Find out the length
        result->m_length = 0;
        while (file.readInt( ) >= 0)
        {
            file.readInt( );
            if (file.readInt( ) < MINPARTLENGTH)
                file.readInt( );
            ++result->m_length;
        }
        file.rewind( );
        RACE("Track : reading trackfile, length of track = %d", result->m_length);
        if (result->m_length == 0)
        {
            result->m_length = 1;
            result->m_definition = new Definition[1];
            result->m_definition[0].type = (Type)0;
            result->m_definition[0].surface = (Surface)0;
            result->m_definition[0].noise = (Noise)0;
            result->m_definition[0].length = MINPARTLENGTH;
            result->m_weather  = (Weather)0;
            result->m_ambience = (Ambience)0;
        }
        else
        {
            result->m_definition = new Definition[result->m_length];
            Int temp = 0;
            for (UInt i = 0; i < result->m_length; ++i)
            {
                result->m_definition[i].type    = (Type)file.readInt( );
                result->m_definition[i].surface = (Surface)file.readInt( );
                temp = file.readInt( );
                if (temp < NOISES)
                {
                    result->m_definition[i].noise   = (Noise)temp;
                    result->m_definition[i].length  = file.readInt( );
                }
                else
                {
                    if (result->m_definition[i].type >= TYPES)
                    {
                        result->m_definition[i].noise = (Noise)((result->m_definition[i].type - TYPES) + 1);
                        result->m_definition[i].type = (Type)0;
                    }
                    else
                    {
                        result->m_definition[i].noise = (Noise)0;
                    }
                    result->m_definition[i].length = temp;
                }
                if (result->m_definition[i].type >= TYPES)
                    result->m_definition[i].type = (Type)0;
                if (result->m_definition[i].surface >= SURFACES)
                    result->m_definition[i].surface = (Surface)0;
                if (result->m_definition[i].noise >= NOISES)
                    result->m_definition[i].noise = (Noise)0;
                if (result->m_definition[i].length < MINPARTLENGTH)
                    result->m_definition[i].length = MINPARTLENGTH;
            }
            file.readInt( ); // -1
            result->m_weather = (Weather)file.readInt( );
            if (result->m_weather < 0)
                result->m_weather = (Weather)0;
            result->m_ambience = (Ambience)file.readInt( );
            if (result->m_ambience < 0)
                result->m_ambience = (Ambience)0;
        }
        RACE("Track : done reading trackfile");
    }
    return result;
}

Track::~Track( )
{
    RACE("(-) Track");
    if (m_userDefined)
    {
        SAFE_DELETE_ARRAY(m_definition);
    }
    if (m_weather == rain)
	{
        SAFE_DELETE(m_soundRain);
    }
    else if (m_weather == wind)
    {
        SAFE_DELETE(m_soundWind);
    }
    else if (m_weather == storm)
    {
        SAFE_DELETE(m_soundStorm);
    }
    if (m_ambience == desert)
    {
        SAFE_DELETE(m_soundDesert);
    }
    else if (m_ambience == airport)
    {
        SAFE_DELETE(m_soundAirport);
    }
    SAFE_DELETE(m_soundCrowd);
    SAFE_DELETE(m_soundOcean);
    SAFE_DELETE(m_soundAirplane);
    SAFE_DELETE(m_soundClock);
    SAFE_DELETE(m_soundJet);
    SAFE_DELETE(m_soundThunder);
    SAFE_DELETE(m_soundPile);
    SAFE_DELETE(m_soundConstruction);
    SAFE_DELETE(m_soundRiver);
    SAFE_DELETE(m_soundHelicopter);
    SAFE_DELETE(m_soundOwl);
}


void
Track::initialize( )
{
    RACE("Track::initialize");
    // calculate tracklength
    for (UInt i = 0; i < m_length; ++i)
    {
        m_lapDistance += m_definition[i].length;
        switch (m_definition[i].type)
        {
        case straight :
            break;
        case easyLeft :
            m_lapCenter -= m_definition[i].length/2; 
            break;
        case left :
            m_lapCenter -= m_definition[i].length*2/3;
            break;
        case hardLeft :
            m_lapCenter -= m_definition[i].length;
            break;
        case hairpinLeft :
            m_lapCenter -= m_definition[i].length*3/2;
            break;
        case easyRight :
            m_lapCenter += m_definition[i].length/2;
            break;
        case right :
            m_lapCenter += m_definition[i].length*2/3;
            break;
        case hardRight :
            m_lapCenter += m_definition[i].length;
            break;
        case hairpinRight :
            m_lapCenter += m_definition[i].length*3/2;
            break;
        default :
            break;
        }
    }
    if (m_weather == rain)
        m_soundRain->play(0, true);
    else if (m_weather == wind)
        m_soundWind->play(0, true);
    else if (m_weather == storm)
        m_soundStorm->play(0, true);
    if (m_ambience == desert)
        m_soundDesert->play(0, true);
    else if (m_ambience == airport)
        m_soundAirport->play(0, true);
}


void
Track::finalize( )
{
    RACE("Track::finalize");
    if (m_weather == rain)
        m_soundRain->stop( );
    else if (m_weather == wind)
        m_soundWind->stop( );
    else if (m_weather == storm)
        m_soundStorm->stop( );
    if (m_ambience == desert)
        m_soundDesert->stop( );
    else if (m_ambience == airport)
        m_soundAirport->stop( );
}


void 
Track::run(/* Float elapsed, */ Int position)
{
    if ((m_noisePlaying == true) && (position > m_noiseEndPos))
        m_noisePlaying = false;
    switch (m_definition[m_currentRoad].noise)
    {
    case crowd:
        if (!m_noisePlaying)
        {
            calculateNoiseLength( );
            m_noiseStartPos = position;
            m_noiseEndPos = position + m_noiseLength;
        }
        m_factor = (position - m_noiseStartPos) * 1.0f / m_noiseLength;
        if (m_factor < 0.5f)
            m_factor *= 2.0f;
        else
            m_factor = 2.0f * (1.0f - m_factor);
        m_soundCrowd->volume((Int)(80.0f + m_factor * 20.0f));
        if (!(m_soundCrowd->playing( )))
        {
            m_soundCrowd->play(0, true);
        }   
        break;
    case ocean:
        if (!m_noisePlaying)
        {
            calculateNoiseLength( );
            m_noiseStartPos = position;
            m_noiseEndPos = position + m_noiseLength;
        }
        m_factor = (position - m_noiseStartPos) * 1.0f / m_noiseLength;
        if (m_factor < 0.5f)
            m_factor *= 2.0f;
        else
            m_factor = 2.0f * (1.0f - m_factor);
        m_soundOcean->volume((Int)(80.0f + m_factor * 20.0f));
        if (!(m_soundOcean->playing( )))
        {
            m_soundOcean->pan(-10);
            m_soundOcean->play(0, true);
        }   
        break;
    case runway:
        if (!(m_soundAirplane->playing( )))
        {
            m_soundAirplane->play( );
        }
        break;
    case clock:
        if (!m_noisePlaying)
        {
            calculateNoiseLength( );
            m_noiseStartPos = position;
            m_noiseEndPos = position + m_noiseLength;
        }
        m_factor = (position - m_noiseStartPos) * 1.0f / m_noiseLength;
        if (m_factor < 0.5f)
            m_factor *= 2.0f;
        else
            m_factor = 2.0f * (1.0f - m_factor);
        m_soundClock->volume((Int)(80.0f + m_factor * 20.0f));
        if (!(m_soundClock->playing( )))
        {
            m_soundClock->pan(25);
            m_soundClock->play(0, true);
        }   
        break;
    case jet:
        if (!(m_soundJet->playing( )))
        {
            m_soundJet->play( );
        }
        break;
    case thunder:
        if (!(m_soundThunder->playing( )))
        {
            m_soundThunder->play( );
        }
        break;
    case pile:
        if (!m_noisePlaying)
        {
            calculateNoiseLength( );
            m_noiseStartPos = position;
            m_noiseEndPos = position + m_noiseLength;
        }
        m_factor = (position - m_noiseStartPos) * 1.0f / m_noiseLength;
        if (m_factor < 0.5f)
            m_factor *= 2.0f;
        else
            m_factor = 2.0f * (1.0f - m_factor);
        m_soundPile->volume((Int)(80.0f + m_factor * 20.0f));
        if (!(m_soundPile->playing( )))
        {
            m_soundPile->play(0, true);
        }   
        break;
    case construction:
        if (!m_noisePlaying)
        {
            calculateNoiseLength( );
            m_noiseStartPos = position;
            m_noiseEndPos = position + m_noiseLength;
        }
        m_factor = (position - m_noiseStartPos) * 1.0f / m_noiseLength;
        if (m_factor < 0.5f)
            m_factor *= 2.0f;
        else
            m_factor = 2.0f * (1.0f - m_factor);
        m_soundConstruction->volume((Int)(80.0f + m_factor * 20.0f));
        if (!(m_soundConstruction->playing( )))
        {
            m_soundConstruction->play(0, true);
        }   
        break;
    case river:
        if (!m_noisePlaying)
        {
            calculateNoiseLength( );
            m_noiseStartPos = position;
            m_noiseEndPos = position + m_noiseLength;
        }
        m_factor = (position - m_noiseStartPos) * 1.0f / m_noiseLength;
        if (m_factor < 0.5f)
            m_factor *= 2.0f;
        else
            m_factor = 2.0f * (1.0f - m_factor);
        m_soundRiver->volume((Int)(80.0f + m_factor * 20.0f));
        if (!(m_soundRiver->playing( )))
        {
            m_soundRiver->play(0, true);
        }   
        break;
    case helicopter:
        if (!(m_soundHelicopter->playing( )))
        {
            m_soundHelicopter->play( );
        }
        break;
    case owl:
        if (!(m_soundOwl->playing( )))
        {
            m_soundOwl->play( );
        }
        break;
    default:
        m_soundCrowd->stop( );
        m_soundOcean->stop( );
        m_soundClock->stop( );
        m_soundPile->stop( );
        m_soundConstruction->stop( );
        m_soundRiver->stop( );
        break;
    }
}


Track::Road
Track::road(Int position)
{
    UInt lap = (UInt)(position / m_lapDistance);
    UInt pos = position % m_lapDistance;
    UInt dist = 0;
    UInt center = lap*m_lapCenter;
    for (UInt i = 0; i < m_length; ++i)
    {
        if ((dist <= pos) && (dist + m_definition[i].length > pos))
        {
            m_prevRelPos = m_relPos;
            m_relPos = pos - dist;
            m_currentRoad = i;
            Road road;
            road.type    = m_definition[i].type;
            road.surface = m_definition[i].surface;
                        road.length  = m_definition[i].length;
            switch (m_definition[i].type)
            {
            case straight :
                road.left  = center - m_laneWidth;
                road.right = center + m_laneWidth;
                break;
            case easyLeft :
                road.left  = center - m_laneWidth - m_relPos/2;
                road.right = center + m_laneWidth - m_relPos/2;
                break;
            case left :
                road.left  = center - m_laneWidth - m_relPos*2/3;
                road.right = center + m_laneWidth - m_relPos*2/3;
                break;
            case hardLeft :
                road.left  = center - m_laneWidth - m_relPos;
                road.right = center + m_laneWidth - m_relPos;
                break;
            case hairpinLeft :
                road.left  = center - m_laneWidth - m_relPos*3/2;
                road.right = center + m_laneWidth - m_relPos*3/2;
                break;
            case easyRight :
                road.left  = center - m_laneWidth + m_relPos/2;
                road.right = center + m_laneWidth + m_relPos/2;
                break;
            case right :
                road.left  = center - m_laneWidth + m_relPos*2/3;
                road.right = center + m_laneWidth + m_relPos*2/3;
                break;
            case hardRight :
                road.left  = center - m_laneWidth + m_relPos;
                road.right = center + m_laneWidth + m_relPos;
                break;
            case hairpinRight :
                road.left  = center - m_laneWidth + m_relPos*3/2;
                road.right = center + m_laneWidth + m_relPos*3/2;
                break;
            default :
                road.left  = center - m_laneWidth;
                road.right = center + m_laneWidth;
                break;
            }
            
            return road;
        }
        switch (m_definition[i].type)
        {
        case straight :
            break;
        case easyLeft :
            center -= m_definition[i].length/2; 
            break;
        case left :
            center -= m_definition[i].length*2/3;
            break;
        case hardLeft :
            center -= m_definition[i].length;
            break;
        case hairpinLeft :
            center -= m_definition[i].length*3/2;
            break;
        case easyRight :
            center += m_definition[i].length/2;
            break;
        case right :
            center += m_definition[i].length*2/3;
            break;
        case hardRight :
            center += m_definition[i].length;
            break;
        case hairpinRight :
            center += m_definition[i].length*3/2;
            break;
        default :
            break;
        }
        
        dist += m_definition[i].length;
    }
    // shouldn't arrive here
    Road road = {0, 0, asphalt, straight, 5000};
    return road;
}


Track::Road
Track::roadComputer(Int position)
{
    UInt lap = (UInt)(position / m_lapDistance);
    UInt pos = position % m_lapDistance;
    UInt dist = 0;
    UInt center = lap*m_lapCenter;
    Int relPos = 0;
    for (UInt i = 0; i < m_length; ++i)
    {
        if ((dist <= pos) && (dist + m_definition[i].length > pos))
        {
            relPos = pos - dist;
            Road road;
            road.type    = m_definition[i].type;
            road.surface = m_definition[i].surface;
            road.length  = m_definition[i].length;
            switch (m_definition[i].type)
            {
            case straight :
                road.left  = center - m_laneWidth;
                road.right = center + m_laneWidth;
                break;
            case easyLeft :
                road.left  = center - m_laneWidth - relPos/2;
                road.right = center + m_laneWidth - relPos/2;
                break;
            case left :
                road.left  = center - m_laneWidth - relPos*2/3;
                road.right = center + m_laneWidth - relPos*2/3;
                break;
            case hardLeft :
                road.left  = center - m_laneWidth - relPos;
                road.right = center + m_laneWidth - relPos;
                break;
            case hairpinLeft :
                road.left  = center - m_laneWidth - relPos*3/2;
                road.right = center + m_laneWidth - relPos*3/2;
                break;
            case easyRight :
                road.left  = center - m_laneWidth + relPos/2;
                road.right = center + m_laneWidth + relPos/2;
                break;
            case right :
                road.left  = center - m_laneWidth + relPos*2/3;
                road.right = center + m_laneWidth + relPos*2/3;
                break;
            case hardRight :
                road.left  = center - m_laneWidth + relPos;
                road.right = center + m_laneWidth + relPos;
                break;
            case hairpinRight :
                road.left  = center - m_laneWidth + relPos*3/2;
                road.right = center + m_laneWidth + relPos*3/2;
                break;
            default :
                road.left  = center - m_laneWidth;
                road.right = center + m_laneWidth;
                break;
            }
            
            return road;
        }
        switch (m_definition[i].type)
        {
        case straight :
            break;
        case easyLeft :
            center -= m_definition[i].length/2; 
            break;
        case left :
            center -= m_definition[i].length*2/3;
            break;
        case hardLeft :
            center -= m_definition[i].length;
            break;
        case hairpinLeft :
            center -= m_definition[i].length*3/2;
            break;
        case easyRight :
            center += m_definition[i].length/2;
            break;
        case right :
            center += m_definition[i].length*2/3;
            break;
        case hardRight :
            center += m_definition[i].length;
            break;
        case hairpinRight :
            center += m_definition[i].length*3/2;
            break;
        default :
            break;
        }
        
        dist += m_definition[i].length;
    }
    // shouldn't arrive here
    Road road = {0, 0, asphalt, straight, 5000};
    return road;
}


Boolean 
Track::nextRoad(Road& road, Int position, Int speed)
{
    if (m_game->raceSettings().curveAnnouncement == 0)
    {
        Int currentLength = m_definition[m_currentRoad].length;
        // if (currentLength > m_callLength)
        // {
            if ((m_relPos + m_callLength > (UInt)currentLength) &&
                (m_prevRelPos + m_callLength <= (UInt)currentLength))
            {
                road.type = m_definition[(m_currentRoad + 1)%m_length].type;        
                road.surface = m_definition[(m_currentRoad + 1)%m_length].surface;
                road.length = m_definition[(m_currentRoad + 1)%m_length].length;
                return true;
            }
        // }
        // nothing to say
        return false;
    }
    else
    {
        // determine the distance to look ahead
        Int lookAhead = m_callLength + speed/2;
        Int roadAhead = roadAt(position + lookAhead);
        if ((((roadAhead - m_lastCalled + m_length) % m_length) > 0) && 
            (((roadAhead - m_lastCalled + m_length) % m_length) <= m_length/2))
        {
            road.type       = m_definition[roadAhead].type;
            road.surface    = m_definition[roadAhead].surface;
            road.length     = m_definition[roadAhead].length;
            m_lastCalled = roadAhead;
            return true;
        }
        return false;
    }
}


Int
Track::roadAt(Int position)
{
    UInt pos = position % m_lapDistance;
    UInt dist = 0;
    for (UInt i = 0; i < m_length; ++i)
    {
        if ((dist <= pos) && (dist + m_definition[i].length > pos))
        {
            return i;
        }
        dist += m_definition[i].length;
    }
    return -1;
}

void
Track::calculateNoiseLength( )
{
    m_noiseLength = 0;
    UInt i = m_currentRoad;
    while ((i < m_length) && (m_definition[i].noise == m_definition[m_currentRoad].noise))
    {
        m_noiseLength += m_definition[i].length;
        ++i;
    }
    m_noisePlaying = true;
//RACE("calculate... 1: i = %d, m_noiseLength = %d", i, m_noiseLength);
}
