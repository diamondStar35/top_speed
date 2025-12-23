/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "LevelSingleRace.h"
#include <Common/If/Algorithm.h>

#define NVEHICLES     12

LevelSingleRace::LevelSingleRace(Game* game, UInt nrOfLaps, Char* track, Boolean automaticTransmission, UInt vehicle, Char* vehicleFile) :
    Level(game, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile),
    m_nComputerPlayers(game->raceSettings().nrOfComputers),
    m_playerNumber(1),
    m_lastComment(0.0f),
    m_infoKeyReleased(true),
    m_positionFinish(0)
{
    RACE("(+) LevelSingleRace");
}


LevelSingleRace::~LevelSingleRace( )
{
    RACE("(-) LevelSingleRace");
}


ComputerPlayer*
LevelSingleRace::generateRandomPlayer(int playerNumber)
{
    return new ComputerPlayer(m_game, random(NVEHICLES), m_track, playerNumber);
}

void
LevelSingleRace::initialize(Int playerNumber)
{
    RACE("LevelSingleRace::initialize");
    initializeLevel( );
    m_playerNumber = playerNumber;
    m_position     = playerNumber + 1;
    m_positionComment = playerNumber + 1;
    Int positionX = 0;
    if (m_playerNumber % 2)
        positionX = 3000;
    else
        positionX = -3000;
    Int positionY = 14000 - m_playerNumber*2000;
    m_car->position(positionX, positionY);
    for (UInt i = 0; i < m_nComputerPlayers; ++i)
    {
        UInt playerNumber = i;
        if (playerNumber >= m_playerNumber)
            ++playerNumber;
        m_computerPlayer[i] = generateRandomPlayer(playerNumber);
        if (playerNumber % 2)
            positionX = 3000;
        else
            positionX = -3000;
        positionY = 14000 - playerNumber*2000;
        m_computerPlayer[i]->initialize(positionX, positionY, m_track->length( ));
    }
    Char filename[64];
    for (UInt i = 0; i <= m_nComputerPlayers; ++i)
    {
            sprintf(filename, "race\\info\\player%d", i+1);
            m_soundPlayerNr[i] = m_game->loadLanguageSound(filename);
        if (i == m_nComputerPlayers)
        {
            sprintf(filename, "race\\info\\youarepos%d", NMAXPLAYERS);
            m_soundPosition[i] = m_game->loadLanguageSound(filename);
            sprintf(filename, "race\\info\\finished%d", NMAXPLAYERS);
            m_soundFinished[i] = m_game->loadLanguageSound(filename);
        }
        else
        {
            sprintf(filename, "race\\info\\youarepos%d", i+1);
            m_soundPosition[i] = m_game->loadLanguageSound(filename);
            sprintf(filename, "race\\info\\finished%d", i+1);
            m_soundFinished[i] = m_game->loadLanguageSound(filename);
        }
        if (i < m_playerNumber)
        {
            sprintf(filename, "vehicles\\vehicle%d", (Int)m_computerPlayer[i]->carType( )+1);
            m_soundVehicle[i] = m_game->loadLanguageSound(filename);
        }
        else if (i > m_playerNumber)
        {
            sprintf(filename, "vehicles\\vehicle%d", (Int)m_computerPlayer[i-1]->carType( )+1);
            m_soundVehicle[i] = m_game->loadLanguageSound(filename);
        }
        else
        {
            if (!m_car->userDefined( ))
            {
                sprintf(filename, "vehicles\\vehicle%d", (Int)m_car->carType( )+1);
                m_soundVehicle[i] = m_game->loadLanguageSound(filename);
            }
            else
            {
                sprintf(filename, "%s.wav", m_car->customFile( ));
                m_soundVehicle[i] = m_game->soundManager( )->create(filename);
            }
        }
    }
    loadRandomSounds(front, "race\\info\\front");
    loadRandomSounds(tail, "race\\info\\tail");
    m_soundYouAre    = m_game->loadLanguageSound("race\\youare");
    m_soundPlayer    = m_game->loadLanguageSound("race\\player");
    m_soundTheme4    = m_game->loadLanguageSound("music\\theme4");
    m_soundPause    = m_game->loadLanguageSound("race\\pause");
    m_soundUnpause  = m_game->loadLanguageSound("race\\unpause");
    m_soundTheme4->volume(50);
    RACE("LevelSingleRace : saying my name: Player %d", playerNumber+1);
    speak(m_soundYouAre);
    speak(m_soundPlayer);		
    speak(m_game->m_soundNumbers[m_playerNumber+1]);
}


void
LevelSingleRace::finalize( )
{
    RACE("LevelSingleRace::finalize");
    for (UInt i = 0; i < m_nComputerPlayers; ++i)
    {
        m_computerPlayer[i]->finalize( );
        SAFE_DELETE(m_computerPlayer[i]);
    }
    for (UInt j = 0; j <= m_nComputerPlayers; ++j)
    {
        SAFE_DELETE(m_soundPosition[j]);
        SAFE_DELETE(m_soundPlayerNr[j]);
        SAFE_DELETE(m_soundFinished[j]);
        SAFE_DELETE(m_soundVehicle[j]);
    }
    SAFE_DELETE(m_soundYouAre);
    SAFE_DELETE(m_soundPlayer);
    finalizeLevel( );
}


void
LevelSingleRace::run(Float elapsed)
{
    if (m_elapsedTotal == 0.0f)
    {
        for (UInt player = 0; player < m_nComputerPlayers; ++player)
            m_computerPlayer[player]->pendingStart( );
        pushEvent(Event::carStart, 3.0f);
        pushEvent(Event::raceStart, 6.5f);
        pushEvent(Event::playSound, 1.5f, m_soundStart);
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

    updatePositions( );
    m_car->run(elapsed);
    m_track->run(/* elapsed, */ m_car->positionY( ));
    for (UInt player = 0; player < m_nComputerPlayers; ++player)
    {
        m_computerPlayer[player]->run(elapsed, m_car->positionX( ), m_car->positionY( ));
        if ((m_track->lap(m_computerPlayer[player]->positionY( )) > m_nrOfLaps) && (m_computerPlayer[player]->finished( ) == false))
        {
            RACE("LevelSingleRace : computerplayer %d finished %d!", m_computerPlayer[player]->playerNumber(), m_positionFinish+1);
            if (!m_game->threeD( ))
                m_computerPlayer[player]->quiet( );
            m_computerPlayer[player]->stop( );
            m_computerPlayer[player]->finished(true);
            speak(m_soundPlayerNr[m_computerPlayer[player]->playerNumber()], true);
            speak(m_soundFinished[m_positionFinish++], true);
            if (checkFinish( ))
            {
                RACE("LevelSingleRace : pushing finish event");
                pushEvent(Event::raceFinish, 1.0f + m_speakTime - m_elapsedTotal);
            }
        }
    }
    Track::Road road = m_track->road(m_car->positionY( ));
    m_car->evaluate(road);
    Track::Road nextRoad;
    if (m_track->nextRoad(nextRoad, m_car->positionY( ), m_car->speed()))
    {
        callNextRoad(nextRoad);
    }
    if (m_track->lap(m_car->positionY( )) > m_lap)
    {
        m_lap = m_track->lap(m_car->positionY( ));
        if (m_lap > m_nrOfLaps)
        {
            RACE("LevelSingleRace : finish reached");
            speak(m_randomSounds[finish][random(m_totalRandomSounds[finish])], true);
            m_car->manualTransmission(false);
            m_car->quiet( );
            m_car->stop( );
            m_raceTime = m_stopwatch.elapsed( ) - m_stopwatchDiff;
            // handleFinish( );                
            RACE("LevelSingleRace : player %d finished %d!", m_playerNumber+1, m_positionFinish+1);
            speak(m_soundPlayerNr[m_playerNumber], true);
            speak(m_soundFinished[m_positionFinish++], true);
            if (checkFinish( ))
            {
                RACE("LevelSingleRace : pushing the finish event");
                pushEvent(Event::raceFinish, 1.0f + m_speakTime - m_elapsedTotal);
            }
        }
        else if ((m_game->raceSettings( ).automaticInfo) && (m_lap > 1) && (m_lap <= m_nrOfLaps))
            speak(m_soundLaps[m_nrOfLaps - m_lap], true);
    }
    // check for bumps
    checkForBumps( );
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
        m_game->m_soundNumbers[m_lap]->play();
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
                    decs = decs / 10;
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
    if ((m_game->raceInput()->getCurrentRaceTime( )) && (m_started) && (m_acceptCurrentRaceInfo))
    {
        m_acceptCurrentRaceInfo = false;
        m_sayTimeLength = 0.0f;
        if (m_lap <= m_nrOfLaps)
            sayTime(m_stopwatch.elapsed(false) - m_stopwatchDiff, false);
        else
            sayTime(m_raceTime, false);
        pushEvent(Event::acceptCurrentRaceInfo, m_sayTimeLength);
    }
    // update comments
    m_lastComment += elapsed;
    if ((m_game->raceSettings().automaticInfo > 1) && (m_lastComment > 6.0f))
    {
        comment(/* elapsed */);
        m_lastComment = 0.0f;
    }
    if ((m_game->raceInput()->getRequestInfo( )) && (m_infoKeyReleased))
    {
        if (m_lastComment > 2.0f)
        {
            m_infoKeyReleased = false;
            comment(/* elapsed, */ false);
            m_lastComment = 0.0f;
        }  
    }
    else if ((!m_game->raceInput()->getRequestInfo( )) && (!m_infoKeyReleased))
    {
        m_infoKeyReleased = true;
    }
    // check for player info requests
    UInt player = 0;
    if ((m_game->raceInput()->getPlayerInfo(player)) && (m_acceptPlayerInfo) && (player <= m_nComputerPlayers))
    {
        m_acceptPlayerInfo = false;
        m_soundVehicle[player]->play( );
        pushEvent(Event::acceptPlayerInfo, m_soundVehicle[player]->length( ));
    }
    if ((m_game->raceInput()->getPlayerPosition(player)) && (m_acceptPlayerInfo) && (player <= m_nComputerPlayers) && (m_started))
    {
        m_acceptPlayerInfo = false;
        UInt perc = calculatePlayerPerc(player);
        m_game->m_soundNumbers[perc]->play( );
        pushEvent(Event::playSound, m_game->m_soundNumbers[perc]->length( ), m_soundPercent);
        pushEvent(Event::acceptPlayerInfo, m_game->m_soundNumbers[perc]->length( ) + m_soundPercent->length( ));
    }
    if ((m_game->raceInput()->getTrackName( )) && (m_acceptCurrentRaceInfo))
    {
        m_acceptCurrentRaceInfo = false;
        m_soundTrackName->play( );
        pushEvent(Event::acceptCurrentRaceInfo, m_soundTrackName->length( ));
    }
    if ((m_game->raceInput()->getPlayerNumber( )) && (m_acceptCurrentRaceInfo))
    {
        m_acceptCurrentRaceInfo = false;
        m_game->m_soundNumbers[m_playerNumber+1]->play( );
        pushEvent(Event::acceptCurrentRaceInfo, m_game->m_soundNumbers[m_playerNumber+1]->length( ));
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

/*
void
LevelSingleRace::handleFinish( )
{
    m_car->quiet( );
}
*/

void
LevelSingleRace::updatePositions( )
{
    m_position = 1;
    for (UInt i = 0; i < m_nComputerPlayers; ++i)
    {
        if (m_computerPlayer[i]->positionY( ) > m_car->positionY( ))
        {
            ++m_position;
        }
    }
}



void
LevelSingleRace::comment(/* Float elapsed, */ Boolean automatic)
{
    if ((!m_started) || (m_lap > m_nrOfLaps))
        return;
    RACE("LevelSingleRace::comment : starting comment, automatic = %d", (int) automatic);
    UInt position = 1;
    Int inFront        = -1;
    Int inFrontDist    = 50000;
    Int onTail         = -1;
    Int onTailDist     = 50000;
    // Int nPlayers       = 1;
    for (UInt player = 0; player < m_nComputerPlayers; ++player)
    {
        if (m_computerPlayer[player]->positionY( ) > m_car->positionY( ))
        {
            ++position;
            Int dist = m_computerPlayer[player]->positionY( ) - m_car->positionY( );
            if (dist < inFrontDist)
            {
                inFront     = player;
                inFrontDist = dist;
            }
        }
        else if (m_computerPlayer[player]->positionY( ) < m_car->positionY( ))
        {
            Int dist = m_car->positionY( ) - m_computerPlayer[player]->positionY( );
            if (dist < onTailDist)
            {
                onTail      = player;
                onTailDist  = dist;
            }
        }
    }
    if ((automatic) && (position != m_positionComment))
    // && (m_game->raceSettings().automaticInfo > 1))
    {
        // if (position != m_positionComment)
        // {
            if (position == m_nComputerPlayers+1)
            {
                RACE("Comment : you're in last position");
                speak(m_soundPosition[m_nComputerPlayers], true);
            }
            else
            {
                RACE("Comment :  you're in position %d", position);
                speak(m_soundPosition[position-1], true);
            }
            m_positionComment = position;
            return;
        // }
    }
    if (inFrontDist < onTailDist)
    {
        if (inFront != -1)
        {
            RACE("Comment : player %d is in front of you", m_computerPlayer[inFront]->playerNumber());
//            speak(m_computerPlayer[inFront]->inFront( ));
            speak(m_soundPlayerNr[m_computerPlayer[inFront]->playerNumber()], true);
            speak(m_randomSounds[front][random(m_totalRandomSounds[front])], true);
            return;
        }
    }
    else
    {
        if (onTail != -1)
        {
            RACE("Comment : player %d is on your tail", m_computerPlayer[onTail]->playerNumber());
//            speak(m_computerPlayer[onTail]->onTail( ));
            speak(m_soundPlayerNr[m_computerPlayer[onTail]->playerNumber()], true);
            speak(m_randomSounds[tail][random(m_totalRandomSounds[tail])], true);
            return;
        }
    }
    if ((inFront == -1) && (onTail == -1) && (!automatic) )
    {
        if (position == m_nComputerPlayers+1)
        {
            RACE("Comment : you're in last position!");
            speak(m_soundPosition[m_nComputerPlayers], true);
        }
        else
        {
            RACE("Comment : you're in position %d!", position);
            speak(m_soundPosition[position-1], true);
        }
        m_positionComment = position;
        return;
    }
}

void
LevelSingleRace::checkForBumps( )
{
    for (UInt i = 0; i < m_nComputerPlayers; ++i)
    {
        if ((m_car->state( ) == Car::running) && (m_computerPlayer[i]->finished( ) == false))
        {
            // Int bumpX = 0;
            // Int bumpY = 0;
            if ((absval<Int>(m_car->positionX() - m_computerPlayer[i]->positionX()) < 1000) && (absval<Int>(m_car->positionY() - m_computerPlayer[i]->positionY()) < 500))
            {
                Int bumpX = m_car->positionX() - m_computerPlayer[i]->positionX();
                Int bumpY = m_car->positionY() - m_computerPlayer[i]->positionY();
                Int bumpSpeed = m_car->speed() - m_computerPlayer[i]->speed();
                m_car->bump(bumpX, bumpY, bumpSpeed);
                m_computerPlayer[i]->bump(-bumpX, -bumpY, -bumpSpeed);
            }
        }
    }
}


Boolean
LevelSingleRace::checkFinish( )
{
    for (UInt i = 0; i < m_nComputerPlayers; ++i)
    {
        if (m_computerPlayer[i]->finished() == false)
            return false;
    }
    if (m_lap <= m_nrOfLaps)
        return false;
    return true;
}

void
LevelSingleRace::pause( )
{
    m_soundTheme4->play(0, true);
    fadeIn( );
    m_car->pause( );
    for (UInt i = 0; i < m_nComputerPlayers; ++i)
        m_computerPlayer[i]->pause( );    
    m_soundPause->play( );
}

void
LevelSingleRace::unpause( )
{
    m_car->unpause( );
    for (UInt i = 0; i < m_nComputerPlayers; ++i)
        m_computerPlayer[i]->unpause( );    
    fadeOut( );
    m_soundTheme4->stop( );
    m_soundTheme4->reset( );
    m_soundUnpause->play( );
}

UInt
LevelSingleRace::calculatePlayerPerc(UInt player)
{
    UInt perc;
    if (player == m_playerNumber)
        perc = UInt(((Float)m_car->positionY( ) / ((Float)m_track->length( ) * m_nrOfLaps)) * 100.0f);    
    else if (player > m_playerNumber)
        perc = UInt(((Float)m_computerPlayer[player-1]->positionY( ) / ((Float)m_track->length( ) * m_nrOfLaps)) * 100.0f);    
    else
        perc = UInt(((Float)m_computerPlayer[player]->positionY( ) / ((Float)m_track->length( ) * m_nrOfLaps)) * 100.0f);    
    if (perc > 100)
        perc = 100;
    return perc;
}
