/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "LevelTimeTrial.h"


const Char highscoreFile[] = "highscore.cfg";

LevelTimeTrial::LevelTimeTrial(Game* game, UInt nrOfLaps, Char* track, Boolean automaticTransmission, UInt vehicle, Char* vehicleFile) :
    Level(game, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile),
    m_soundVehicle(0)
{
    RACE("(+) LevelTimeTrial");
    Char filename[64];
    if (!m_car->userDefined( ))
    {
        sprintf(filename, "vehicles\\vehicle%d", (Int)m_car->carType( )+1);
        m_soundVehicle = m_game->loadLanguageSound(filename);
    }
    else
    {
        sprintf(filename, "%s.wav", m_car->customFile( ));
        m_soundVehicle = m_game->soundManager( )->create(filename);
    }
}


LevelTimeTrial::~LevelTimeTrial( )
{
    RACE("(-) LevelTimeTrial");
}


void
LevelTimeTrial::initialize( )
{
    RACE("LevelTimeTrial::initialize");
    initializeLevel( );
    m_soundTheme4    = m_game->loadLanguageSound("music\\theme4");
    m_soundPause    = m_game->loadLanguageSound("race\\pause");
    m_soundUnpause  = m_game->loadLanguageSound("race\\unpause");
    m_soundTheme4->volume(50);
}


void
LevelTimeTrial::finalize( )
{
    RACE("LevelTimeTrial::finalize");
    finalizeLevel( );
}


void
LevelTimeTrial::run(Float elapsed)
{
    if (m_elapsedTotal == 0.0f)
    {
        pushEvent(Event::carStart, 1.5f);
        pushEvent(Event::raceStart, 5.0f);
        m_soundStart->play( );
    }
    // Handle events
    Event* e = 0;
    while (e = m_eventList.next(e))
    {
        if (e->time <= m_elapsedTotal)
        {
            switch (e->type)
            {
            case Event::carStart:
                m_car->start( );
                break;
            case Event::raceStart:
                m_raceTime = 0;
                // start stopwatch
                m_stopwatch.elapsed( );
                m_lap = 0;
                m_started = true;
                break;
            case Event::raceFinish:
                pushEvent(Event::playSound, m_sayTimeLength, m_soundYourTime);
                m_sayTimeLength += m_soundYourTime->length() + 0.5f;
                sayTime(m_raceTime);
                m_highscore = readHighScore(/* m_track */);
                if ((m_raceTime < m_highscore) || (m_highscore == 0))
                {
                    writeHighScore(/* m_track, m_raceTime */);
                    pushEvent(Event::playSound, m_sayTimeLength, m_soundNewTime);
                    m_sayTimeLength += m_soundNewTime->length();
                }
                else
                {
                    pushEvent(Event::playSound, m_sayTimeLength, m_soundBestTime);
                    m_sayTimeLength += m_soundBestTime->length() + 0.5f;
                    sayTime(m_highscore);
                }
                pushEvent(Event::raceTimeFinalize, m_sayTimeLength);
                break;
            case Event::playSound:
                if (e->sound)
                {
                    e->sound->reset( );
                    e->sound->play( );
                }
                break;
            /* case Event::playSoundAndDelete:
                if (e->sound)
                {
                    e->sound->play( );
                    pushEvent(Event::deleteSound, e->sound->length( ), e->sound);
                }
                break;
            case Event::deleteSound:
                if (e->sound)
                {
                    SAFE_DELETE(e->sound);
                }
                break; */
            case Event::raceTimeFinalize:
                m_sayTimeLength = 0.0f;
                delete e;
                e = 0;
                m_game->state(Game::menu);
                return;
            case Event::playRadioSound:
                --m_unkeyQueue;
                if (m_unkeyQueue == 0)
                    speak(m_soundUnkey[random(NUNKEYS)]);
                break;
            case Event::acceptPlayerInfo:
                m_acceptPlayerInfo = true;
                break;
            case Event::acceptCurrentRaceInfo:
                m_acceptCurrentRaceInfo = true;
                break;
            default:
                break;
            }
            m_eventList.purge(e);
            delete e;
            e = 0;
        }
    }

    m_car->run(elapsed);
    m_track->run(/* elapsed, */ m_car->positionY( ));
    Track::Road road = m_track->road(m_car->positionY( ));
    m_car->evaluate(road);
    Track::Road nextRoad;
    if (m_track->nextRoad(nextRoad, m_car->positionY( ), m_car->speed()))
        callNextRoad(nextRoad);
    if (m_track->lap(m_car->positionY( )) > m_lap)
    {
        m_lap = m_track->lap(m_car->positionY( ));
        if (m_lap > m_nrOfLaps)
        {
            speak(m_randomSounds[finish][random(m_totalRandomSounds[finish])], true);
            m_car->manualTransmission(false);
            m_car->quiet( );
            m_car->stop( );
            m_raceTime = m_stopwatch.elapsed( ) - m_stopwatchDiff;
            // handleFinish( );                
            pushEvent(Event::raceFinish, 2.0f);
        }
        else if ((m_game->raceSettings( ).automaticInfo) && (m_lap > 1) && (m_lap < m_nrOfLaps + 1))
            speak(m_soundLaps[m_nrOfLaps - m_lap], true);
    }
    if ((m_game->raceInput()->getCurrentGear( )) && (m_started) && (m_acceptCurrentRaceInfo) && (m_lap <= m_nrOfLaps))
    {
        m_acceptCurrentRaceInfo = false;
        Int gear = m_car->gear( );
        m_game->m_soundNumbers[gear]->play( );
        pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[gear]->length( ));
    }
    if ((m_game->raceInput()->getCurrentLapNr( )) && (m_started) && (m_acceptCurrentRaceInfo) && (m_lap <= m_nrOfLaps))
    {
        m_acceptCurrentRaceInfo = false;
        m_game->m_soundNumbers[m_lap]->play( );
        pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[m_lap]->length( ));
    }
    if ((m_game->raceInput()->getCurrentRacePerc( )) && (m_started) && (m_acceptCurrentRaceInfo) && (m_lap <= m_nrOfLaps))
    {
        m_acceptCurrentRaceInfo = false;
        Float perc = ((Float)m_car->positionY( ) / ((Float)m_track->length( ) * m_nrOfLaps)) * 100.0f;
        UInt units = (UInt)perc;
        UInt decs = UInt((perc - (Float)units) * 100.0f);
        m_game->m_soundNumbers[units]->play( );
        if (decs > 0)
        {
            pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ), m_soundPoint);
            if (decs < 10)
            {
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ), m_game->m_soundNumbers[0]);
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ), m_game->m_soundNumbers[decs]);
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ) + m_game->m_soundNumbers[decs]->length( ), m_soundPercent);
                pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ) + m_game->m_soundNumbers[decs]->length( ) + m_soundPercent->length( ));
            }
            else
            {
                if (decs % 10 == 0)
                    decs = decs/10;
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ), m_game->m_soundNumbers[decs]);
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[decs]->length( ), m_soundPercent);
                pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[decs]->length( ) + m_soundPercent->length( ));
            }
        }
        else
        {
            pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ), m_soundPercent);
            pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPercent->length( ));
        }
    }
    if ((m_game->raceInput()->getCurrentLapPerc( )) && (m_started) && (m_acceptCurrentRaceInfo) && (m_lap <= m_nrOfLaps))
    {
        m_acceptCurrentRaceInfo = false;
        Float perc = (((Float)m_car->positionY( ) - (m_track->length( ) * (m_lap - 1))) / (Float)m_track->length( )) * 100.0f;
        UInt units = (UInt)perc;
        // UInt decs = UInt((perc - (Float)units) * 100.0f);
        m_game->m_soundNumbers[units]->play( );
        /* if (decs > 0)
        {
            pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ), m_soundPoint);
            if (decs < 10)
            {
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ), m_game->m_soundNumbers[0]);
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ), m_game->m_soundNumbers[decs]);
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ) + m_game->m_soundNumbers[decs]->length( ), m_soundPercent);
                pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[0]->length( ) + m_game->m_soundNumbers[decs]->length( ) + m_soundPercent->length( ));
            }
            else
            {
                if (decs % 10 == 0)
                    decs = decs/10;
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ), m_game->m_soundNumbers[decs]);
                pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[decs]->length( ), m_soundPercent);
                pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPoint->length( ) + m_game->m_soundNumbers[decs]->length( ) + m_soundPercent->length( ));
            }
        }
        else
        { */
            pushEvent(Event::playSound, m_game->m_soundNumbers[units]->length( ), m_soundPercent);
            pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[units]->length( ) + m_soundPercent->length( ));
        // }
    }
    if ((m_game->raceInput()->getCurrentRaceTime( )) && (m_started) && (m_acceptCurrentRaceInfo) && (m_lap <= m_nrOfLaps))
    {
        m_acceptCurrentRaceInfo = false;
        m_sayTimeLength = 0.0f;
        sayTime(m_stopwatch.elapsed(false) - m_stopwatchDiff, false);
        pushEvent(Event::acceptCurrentRaceInfo, m_sayTimeLength);
    }
    // check for player info requests
    UInt player = 0;
    if ((m_game->raceInput()->getPlayerInfo(player)) && (m_acceptPlayerInfo) && (player == 0))
    {
        m_acceptPlayerInfo = false;
        m_soundVehicle->play( );
        pushEvent(Event::acceptPlayerInfo, m_soundVehicle->length( ));
    }
    if ((m_game->raceInput()->getTrackName( )) && (m_acceptCurrentRaceInfo))
    {
        m_acceptCurrentRaceInfo = false;
        m_soundTrackName->play( );
        pushEvent(Event::acceptCurrentRaceInfo, m_soundTrackName->length( ));
    }
    if ((!m_game->raceInput()->getPause( )) && (!m_game->pauseKeyReleased( )))
    {
        m_game->pauseKeyReleased(true);
    }
    else if ((m_game->raceInput()->getPause( )) && (m_game->pauseKeyReleased( )) && (m_started) && (m_lap <= m_nrOfLaps) && (m_car->state( ) == Car::running))
    {
        m_game->pauseKeyReleased(false);
        m_game->state(Game::paused);
    }
    // if (m_game->raceInput()->getFlush( )) 
        // flushPendingSounds( );
    // update elapsed time
    m_elapsedTotal += elapsed;
}


void
LevelTimeTrial::writeHighScore(/*  Track* track, Int time */)
{
    File* file = new File(highscoreFile, File::read | File::write);
    if (!file->opened( ))
    {
        SAFE_DELETE(file);
        file = new File(highscoreFile, File::create | File::read | File::write);
    }
    Char trackLaps[64];
    sprintf(trackLaps, "%s;%d", m_track->trackName( ), m_nrOfLaps);
    file->writeKeyInt(trackLaps, m_raceTime); 
    // file.writeKeyInt(track->trackName( ), time); 
    SAFE_DELETE(file);
}


Int
LevelTimeTrial::readHighScore(/* Track* track */)
{
    File file(highscoreFile, File::read);
    if (!file.opened( ))
        return 0;
    Char trackLaps[64];
    sprintf(trackLaps, "%s;%d", m_track->trackName( ), m_nrOfLaps);
    Int val;
    file.readKeyInt(trackLaps, val, 0);
    return val;
}


/*
Boolean
LevelTimeTrial::readHighscores( )
{
    File file(highscoreFile, File::read);
    // Find out the length
    m_highscoresSize = 1;
    while (file.readInt( ) >= 0)
    {
        ++m_highscoresSize;
    }
    file.rewind( );
    Int checksum = 0x521265;
    m_highscores = new Int[m_highscoresSize];
    for (UInt i = 0; i < (Int) m_highscoresSize; ++i)
    {
        m_highscores[i] = file.readInt();
        checksum += m_highscores[i]*11*(i+5);
    }
    m_highscoreChecksum = file.readInt();
    if (m_highscoreChecksum != checksum)
    {
        for (UInt i = 0; i < m_highscoresSize - 1; ++i)
        {
            m_highscores[i] = 0;
        }
    }
    return (m_highscoreChecksum == checksum);
}

void 
LevelTimeTrial::writeHighscores( )
{
    File file(highscoreFile, File::write);
    m_highscoreChecksum = 0x521265;
    for (UInt i = 0; i < m_highscoresSize; ++i)
    {
        file.writeInt(m_highscores[i]);
        m_highscoreChecksum += m_highscores[i]*11*(i+5);
    }
    delete[] m_highscores;
    file.writeInt(m_highscoreChecksum);
}

void
LevelTimeTrial::handleFinish( )
{
    m_car->quiet( );
}
*/

void
LevelTimeTrial::pause( )
{
    fadeIn( );
    m_soundTheme4->play(0, true);
    m_car->pause( );
    m_soundPause->play( );
}

void
LevelTimeTrial::unpause( )
{
    m_car->unpause( );
    fadeOut( );
    m_soundTheme4->stop( );
    m_soundTheme4->reset( );
    m_soundUnpause->play( );
}
