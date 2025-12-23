/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "Level.h"
#include "resource.h"
#include "Common/If/Algorithm.h"



Level::Level(Game* game, Char* track, Boolean automaticTransmission, UInt nrOfLaps, UInt vehicle, Char* vehicleFile) :
    m_nrOfLaps(nrOfLaps),
    m_lap(0),
    m_manualTransmission(!automaticTransmission),
    m_track(0),
    m_speakTime(0.0f),
    m_unkeyQueue(0),
    m_game(game),
    m_highscore(0),
    m_sayTimeLength(0),
    m_acceptPlayerInfo(true),
    m_acceptCurrentRaceInfo(true)
{
    RACE("(+) Level");
    m_track = new Track(track, m_game);
    m_car = new Car(m_game, m_track, vehicle, vehicleFile);

    if ((track != 0) && (strstr(_strlwr(track), "adv") != NULL))
    {
        m_track->laneWidth(ADVLANEWIDTH);
        m_nrOfLaps = 1;
    }
    m_soundStart        = m_game->loadLanguageSound("race\\start321");
    m_soundBestTime = m_game->loadLanguageSound("race\\time\\trackrecord");
    m_soundNewTime = m_game->loadLanguageSound("race\\time\\newrecord");
    m_soundYourTime = m_game->loadLanguageSound("race\\time\\yourtime");
    m_soundMinute = m_game->loadLanguageSound("race\\time\\minute");
    m_soundMinutes = m_game->loadLanguageSound("race\\time\\minutes");
    m_soundSecond = m_game->loadLanguageSound("race\\time\\second");
    m_soundSeconds = m_game->loadLanguageSound("race\\time\\seconds");
    m_soundPoint = m_game->loadLanguageSound("race\\time\\point");
    m_soundPercent = m_game->loadLanguageSound("race\\time\\percent");
    m_soundUnkey[0]	= m_game->soundManager()->create(IDR_UNKEY1);
    m_soundUnkey[1]	= m_game->soundManager()->create(IDR_UNKEY2);
    m_soundUnkey[2]	= m_game->soundManager()->create(IDR_UNKEY3);
    m_soundUnkey[3]	= m_game->soundManager()->create(IDR_UNKEY4);
    m_soundUnkey[4]	= m_game->soundManager()->create(IDR_UNKEY5);
    m_soundUnkey[5]	= m_game->soundManager()->create(IDR_UNKEY6);
    m_soundUnkey[6]	= m_game->soundManager()->create(IDR_UNKEY7);
    m_soundUnkey[7]	= m_game->soundManager()->create(IDR_UNKEY8);
    m_soundUnkey[8]	= m_game->soundManager()->create(IDR_UNKEY9);
    m_soundUnkey[9]	= m_game->soundManager()->create    (IDR_UNKEY10);
    m_soundUnkey[10]	= m_game->soundManager()->create(IDR_UNKEY11);
    m_soundUnkey[11]	= m_game->soundManager()->create(IDR_UNKEY12);
    loadRandomSounds(easyLeft, "race\\copilot\\easyleft");
    loadRandomSounds(left, "race\\copilot\\left");
    loadRandomSounds(hardLeft, "race\\copilot\\hardleft");
    loadRandomSounds(hairpinLeft, "race\\copilot\\hairpinleft");
    loadRandomSounds(easyRight, "race\\copilot\\easyright");
    loadRandomSounds(right, "race\\copilot\\right");
    loadRandomSounds(hardRight, "race\\copilot\\hardright");
    loadRandomSounds(hairpinRight, "race\\copilot\\hairpinright");
    loadRandomSounds(asphalt, "race\\copilot\\asphalt");
    loadRandomSounds(gravel, "race\\copilot\\gravel");
    loadRandomSounds(water, "race\\copilot\\water");
    loadRandomSounds(sand, "race\\copilot\\sand");
    loadRandomSounds(snow, "race\\copilot\\snow");
    loadRandomSounds(finish, "race\\info\\finish");
    Char tempName[21];
    for (UInt i = 0; i < NLAPS-1; ++i)
    {
        sprintf(tempName, "race\\info\\laps2go%d", i+1);
        m_soundLaps[i] = m_game->loadLanguageSound(tempName);
    }
    if (strcmp(m_track->trackName( ), "america") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\america");
    else if (strcmp(m_track->trackName( ), "austria") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\austria");
    else if (strcmp(m_track->trackName( ), "belgium") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\belgium");
    else if (strcmp(m_track->trackName( ), "brazil") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\brazil");
    else if (strcmp(m_track->trackName( ), "china") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\china");
    else if (strcmp(m_track->trackName( ), "england") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\england");
    else if (strcmp(m_track->trackName( ), "finland") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\finland");
    else if (strcmp(m_track->trackName( ), "france") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\france");
    else if (strcmp(m_track->trackName( ), "germany") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\germany");
    else if (strcmp(m_track->trackName( ), "ireland") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\ireland");
    else if (strcmp(m_track->trackName( ), "italy") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\italy");
    else if (strcmp(m_track->trackName( ), "netherlands") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\netherlands");
    else if (strcmp(m_track->trackName( ), "portugal") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\portugal");
    else if (strcmp(m_track->trackName( ), "russia") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\russia");
    else if (strcmp(m_track->trackName( ), "spain") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\spain");
    else if (strcmp(m_track->trackName( ), "sweden") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\sweden");
    else if (strcmp(m_track->trackName( ), "switserland") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\switserland");
    else if (strcmp(m_track->trackName( ), "advHills") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\rallyhills");
    else if (strcmp(m_track->trackName( ), "advCoast") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\frenchcoast");
    else if (strcmp(m_track->trackName( ), "advCountry") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\englishcountry");
    else if (strcmp(m_track->trackName( ), "advAirport") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\rideairport");
    else if (strcmp(m_track->trackName( ), "advDesert") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\rallydesert");
    else if (strcmp(m_track->trackName( ), "advRush") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\rushhour");
    else if (strcmp(m_track->trackName( ), "advEscape") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\polarescape");
    else
    {
        Int length = ::strlen(m_track->trackName( ));
        Char trackName[64];
        ::strncpy(trackName, m_track->trackName( ), length-4);
        trackName[length-4] = '\0';
        ::sprintf(trackName, "%s.wav", trackName);
        m_soundTrackName = m_game->soundManager()->create(trackName);
    }
}

Level::Level(Game* game, Char* track, Track::TrackData trackData, Boolean automaticTransmission, UInt nrOfLaps, UInt vehicle, Char* vehicleFile) :
    m_nrOfLaps(nrOfLaps),
    m_lap(0),
    m_manualTransmission(!automaticTransmission),
    m_track(0),
    m_speakTime(0.0f),
    m_unkeyQueue(0),
    m_game(game),
    m_acceptPlayerInfo(true),
    m_acceptCurrentRaceInfo(true)
{
    RACE("(+) Level");
    m_track = new Track(track, trackData, m_game);
    m_car = new Car(m_game, m_track, vehicle, vehicleFile);
    if ((track != 0) && (strstr(_strlwr(track), "adv") != NULL))
    {
        m_track->laneWidth(ADVLANEWIDTH);
        m_nrOfLaps = 1;
    }
    m_soundStart        = m_game->loadLanguageSound("race\\start321");
    m_soundBestTime = m_game->loadLanguageSound("race\\time\\trackrecord");
    m_soundNewTime = m_game->loadLanguageSound("race\\time\\newrecord");
    m_soundYourTime = m_game->loadLanguageSound("race\\time\\yourtime");
    m_soundMinute = m_game->loadLanguageSound("race\\time\\minute");
    m_soundMinutes = m_game->loadLanguageSound("race\\time\\minutes");
    m_soundSecond = m_game->loadLanguageSound("race\\time\\second");
    m_soundSeconds = m_game->loadLanguageSound("race\\time\\seconds");
    m_soundPoint = m_game->loadLanguageSound("race\\time\\point");
    m_soundPercent = m_game->loadLanguageSound("race\\time\\percent");
    m_soundUnkey[0]	= m_game->soundManager()->create(IDR_UNKEY1);
    m_soundUnkey[1]	= m_game->soundManager()->create(IDR_UNKEY2);
    m_soundUnkey[2]	= m_game->soundManager()->create(IDR_UNKEY3);
    m_soundUnkey[3]	= m_game->soundManager()->create(IDR_UNKEY4);
    m_soundUnkey[4]	= m_game->soundManager()->create(IDR_UNKEY5);
    m_soundUnkey[5]	= m_game->soundManager()->create(IDR_UNKEY6);
    m_soundUnkey[6]	= m_game->soundManager()->create(IDR_UNKEY7);
    m_soundUnkey[7]	= m_game->soundManager()->create(IDR_UNKEY8);
    m_soundUnkey[8]	= m_game->soundManager()->create(IDR_UNKEY9);
    m_soundUnkey[9]	= m_game->soundManager()->create    (IDR_UNKEY10);
    m_soundUnkey[10]	= m_game->soundManager()->create(IDR_UNKEY11);
    m_soundUnkey[11]	= m_game->soundManager()->create(IDR_UNKEY12);
    loadRandomSounds(easyLeft, "race\\copilot\\easyleft");
    loadRandomSounds(left, "race\\copilot\\left");
    loadRandomSounds(hardLeft, "race\\copilot\\hardleft");
    loadRandomSounds(hairpinLeft, "race\\copilot\\hairpinleft");
    loadRandomSounds(easyRight, "race\\copilot\\easyright");
    loadRandomSounds(right, "race\\copilot\\right");
    loadRandomSounds(hardRight, "race\\copilot\\hardright");
    loadRandomSounds(hairpinRight, "race\\copilot\\hairpinright");
    loadRandomSounds(asphalt, "race\\copilot\\asphalt");
    loadRandomSounds(gravel, "race\\copilot\\gravel");
    loadRandomSounds(water, "race\\copilot\\water");
    loadRandomSounds(sand, "race\\copilot\\sand");
    loadRandomSounds(snow, "race\\copilot\\snow");
    loadRandomSounds(finish, "race\\info\\finish");
    Char tempName[21];
    for (UInt i = 0; i < NLAPS-1; ++i)
    {
        sprintf(tempName, "race\\info\\laps2go%d", i+1);
        m_soundLaps[i] = m_game->loadLanguageSound(tempName);
    }
    if (strcmp(m_track->trackName( ), "america") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\america");
    else if (strcmp(m_track->trackName( ), "austria") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\austria");
    else if (strcmp(m_track->trackName( ), "belgium") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\belgium");
    else if (strcmp(m_track->trackName( ), "brazil") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\brazil");
    else if (strcmp(m_track->trackName( ), "china") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\china");
    else if (strcmp(m_track->trackName( ), "england") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\england");
    else if (strcmp(m_track->trackName( ), "finland") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\finland");
    else if (strcmp(m_track->trackName( ), "france") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\france");
    else if (strcmp(m_track->trackName( ), "germany") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\germany");
    else if (strcmp(m_track->trackName( ), "ireland") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\ireland");
    else if (strcmp(m_track->trackName( ), "italy") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\italy");
    else if (strcmp(m_track->trackName( ), "netherlands") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\netherlands");
    else if (strcmp(m_track->trackName( ), "portugal") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\portugal");
    else if (strcmp(m_track->trackName( ), "russia") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\russia");
    else if (strcmp(m_track->trackName( ), "spain") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\spain");
    else if (strcmp(m_track->trackName( ), "sweden") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\sweden");
    else if (strcmp(m_track->trackName( ), "switserland") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\switserland");
    else if (strcmp(m_track->trackName( ), "advHills") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\rallyhills");
    else if (strcmp(m_track->trackName( ), "advCoast") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\frenchcoast");
    else if (strcmp(m_track->trackName( ), "advCountry") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\englishcountry");
    else if (strcmp(m_track->trackName( ), "advAirport") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\rideairport");
    else if (strcmp(m_track->trackName( ), "advDesert") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\rallydesert");
    else if (strcmp(m_track->trackName( ), "advRush") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\rushhour");
    else if (strcmp(m_track->trackName( ), "advEscape") == 0)
        m_soundTrackName = m_game->loadLanguageSound("tracks\\polarescape");
        else if ((!m_game->serverStarted( )) && (strcmp(m_track->trackName( ), "custom") == 0))
        m_soundTrackName = m_game->loadLanguageSound("menu\\customtrack");
    else
    {
        Int length = ::strlen(m_track->trackName( ));
        Char trackName[64];
        ::strncpy(trackName, m_track->trackName( ), length-4);
        trackName[length-4] = '\0';
        ::sprintf(trackName, "%s.wav", trackName);
        m_soundTrackName = m_game->soundManager()->create(trackName);
    }
}

Level::~Level( )
{
    RACE("(-) Level");
    SAFE_DELETE(m_car);
    SAFE_DELETE(m_track);
    SAFE_DELETE(m_soundStart);
    SAFE_DELETE(m_soundBestTime);
    SAFE_DELETE(m_soundNewTime);
    SAFE_DELETE(m_soundYourTime);
    SAFE_DELETE(m_soundMinute);
    SAFE_DELETE(m_soundMinutes);
    SAFE_DELETE(m_soundSecond);
    SAFE_DELETE(m_soundSeconds);
    SAFE_DELETE(m_soundPoint);
    SAFE_DELETE(m_soundPercent);
    for (UInt i = 0; i < NUNKEYS; ++i)
    {
        SAFE_DELETE(m_soundUnkey[i]);
    }
    for (UInt i = 0; i < 14; ++i)
    {
        for (UInt j = 0; j < m_totalRandomSounds[i]; ++j)
        {
            SAFE_DELETE(m_randomSounds[i][j]);
        }
    }
    /*
    for (UInt i = 0; i < m_totalRandomSounds[finish]; ++i)
    {
        SAFE_DELETE(m_randomSounds[finish][i]);
    }
    */
    for (UInt i = 0; i < NLAPS-1; ++i)
    {
        SAFE_DELETE(m_soundLaps[i]);
    }
    SAFE_DELETE(m_soundTrackName);
}


void
Level::initializeLevel( )
{
    RACE("Level::initializeLevel");
    m_track->initialize( );
    m_car->initialize( );
    m_elapsedTotal = 0.0f;
    m_oldStopwatch = 0;
    m_stopwatchDiff = 0;
    m_started = false;
    m_finished = false;
    m_currentRoad.surface = m_track->definition()[0].surface;
    m_car->manualTransmission(m_manualTransmission);
}

void
Level::finalizeLevel( )
{
    RACE("Level::finalizeLevel");
    // m_elapsedTotal = 0.0f;
    m_car->finalize( );
    m_track->finalize( );
}


void
Level::sayTime(Int raceTime, Boolean detailed)
{
    RACE("Level::sayTime : racetime = %d", m_raceTime);
    // Get time
    UInt nminutes = raceTime / 60000;
    UInt nseconds = (raceTime % 60000) / 1000;

    if (nminutes != 0)
    {
        pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[nminutes]);
        m_sayTimeLength += m_game->m_soundNumbers[nminutes]->length();
        if (nminutes == 1)
        {
            pushEvent(Event::playSound, m_sayTimeLength, m_soundMinute);
            m_sayTimeLength += m_soundMinute->length();
        }
        else
        {
            pushEvent(Event::playSound, m_sayTimeLength, m_soundMinutes);
            m_sayTimeLength += m_soundMinutes->length();
        }
    }
    pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[nseconds]);
    m_sayTimeLength += m_game->m_soundNumbers[nseconds]->length();
    if (detailed)
    {
        UInt ntens = (((raceTime % 60000) / 100) % 10);
        UInt nhundreds = (((raceTime % 60000) / 10) % 10);
        UInt nthousands = ((raceTime % 60000) % 10);
        pushEvent(Event::playSound, m_sayTimeLength, m_soundPoint);
        m_sayTimeLength += m_soundPoint->length();
        pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[ntens]);
        m_sayTimeLength += m_game->m_soundNumbers[ntens]->length();
        pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[nhundreds]);
        m_sayTimeLength += m_game->m_soundNumbers[nhundreds]->length();
        pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[nthousands]);
        m_sayTimeLength += m_game->m_soundNumbers[nthousands]->length();
    }
    if ((!detailed) && (nseconds == 1))
    {
        pushEvent(Event::playSound, m_sayTimeLength, m_soundSecond);
        m_sayTimeLength += m_soundSecond->length();
    }
    else
    {
        pushEvent(Event::playSound, m_sayTimeLength, m_soundSeconds);
        m_sayTimeLength += m_soundSeconds->length();
    }
}


void
Level::callNextRoad(Track::Road& nextRoad)
{
    if ((m_game->raceSettings().copilot > 0) && (nextRoad.type != Track::straight))
       m_randomSounds[nextRoad.type-1][random(m_totalRandomSounds[nextRoad.type-1])]->play();
    if ((m_game->raceSettings().copilot > 1) && (nextRoad.surface != m_currentRoad.surface))
    {
            // call surface
            pushEvent(Event::playSound, 1.0f, m_randomSounds[nextRoad.surface+8][random(m_totalRandomSounds[nextRoad.surface+8])]);
    }
    m_currentRoad = nextRoad;
}


void 
Level::pushEvent(Event::Type type, Float time, DirectX::Sound* sound)
{
    Event* e = new Event;
    e->type = type;
    e->time = m_elapsedTotal + time;
    e->sound = sound;
    m_eventList.push(e);
}


void 
Level::speak(DirectX::Sound* sound, Boolean unKey)
{
    if (m_speakTime > m_elapsedTotal)
    {
        pushEvent(Event::playSound, m_speakTime - m_elapsedTotal, sound);
        m_speakTime += sound->length( );
    }
    else
    {
        sound->reset( );
        sound->play( );
        m_speakTime = m_elapsedTotal + sound->length( );
    }
    if (unKey)
    {
        ++m_unkeyQueue;
        pushEvent(Event::playRadioSound, sound->length( ));
    }
}

void 
Level::loadRandomSounds(RandomSound pos, Char* temp)
{
    Char filename[32];
    sprintf(filename, "%s1", temp);
    m_randomSounds[pos][0] = m_game->loadLanguageSound(filename, false, false);
    m_totalRandomSounds[pos] = 1;
    for (UInt i = 1; i < 32; ++i)
    {
        sprintf(filename, "%s%d", temp, i+1);
        m_randomSounds[pos][i] = m_game->loadLanguageSound(filename, false, true);
        if (m_randomSounds[pos][i] == 0)
        {
            m_totalRandomSounds[pos] = i;
RACE("Level::loadRandomSounds : Finished loading %d sounds for a total of %d", pos, m_totalRandomSounds[pos]);
            break;
        }
    }
}

void
Level::flushPendingSounds( )
{
    Event* e = 0;
    while (e = m_eventList.next(e))
    {
        if (e->sound)
        {
            m_eventList.purge(e);
            delete e;
            e = 0;
        }
    }
    delete e;
    e = 0;
}

void
Level::fadeIn( )
{
    Int volume = 50;
    for (UInt i = 0; i < 10; ++i)
    {
        volume += 5;
        m_soundTheme4->volume(volume);
        ::Sleep(25);
        m_game->resetTimer( );
    }
}

void
Level::fadeOut( )
{
    Int volume = 100;
    for (UInt i = 0; i < 10; ++i)
    {
        volume -= 5;
        m_soundTheme4->volume(volume);
        ::Sleep(25);
        m_game->resetTimer( );
    }
}
