/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "ComputerPlayer.h"
#include "Track.h"
#include "Common/If/Algorithm.h"
#include "DxCommon/If/Common.h"
#include "resource.h"
#include "RaceInput.h"
#include "Car.h"

#define CALLLENGTH 3000
extern Car::Parameters vehicles[NVEHICLES];

ComputerPlayer::ComputerPlayer(Game* game, UInt vehicle, Track* track, Int playerNumber) :
    m_track(track),
    m_surface(Track::asphalt),
    m_gear(1),
    m_state(stopped),
    m_switchingGear(0),
    m_playerNumber(playerNumber),
    // m_position(playerNumber),
    m_horning(false),
    m_game(game),
    m_soundManager(game->soundManager( )),
    m_difficulty(game->raceSettings( ).difficulty),
    m_soundEngine(0),
    m_soundStart(0),
    m_soundHorn(0),
    m_soundBackfire(0),
    m_backfirePlayedAuto(false),
    m_carType(vehicle1),
    m_prevFrequency(0),
    m_prevBrakeFrequency(0),
    m_brakeFrequency(0),
    m_laneWidth(0),
    m_relPos(0),
    m_nextRelPos(0),
    m_diffX(0),
    m_diffY(0),
    m_currentSteering(0),
    m_currentThrottle(0),
    m_currentBrake(0),
    m_currentAcceleration(0),
    m_currentDeceleration(0),
    m_speedDiff(0),
    m_thrust(0),
    m_speed(0),
    m_frame(1),
    m_finished(false),
    m_random(random(100))
{
    RACE("(+) ComputerPlayer");
    m_carType     = (CarType)vehicle;
    m_acceleration  = vehicles[vehicle].acceleration;
    m_deceleration  = vehicles[vehicle].deceleration;
    m_topspeed      = vehicles[vehicle].topspeed;
    m_idlefreq      = vehicles[vehicle].idlefreq;
    m_topfreq       = vehicles[vehicle].topfreq;
    m_shiftfreq     = vehicles[vehicle].shiftfreq;
    m_gears         = vehicles[vehicle].gears;
    m_steering      = vehicles[vehicle].steering;
    m_steeringFactor= vehicles[vehicle].steeringFactor;
    m_frequency     = m_idlefreq;
    m_soundEngine   = m_soundManager->create(vehicles[vehicle].engineSound, m_game->threeD( ));
    m_soundStart    = m_soundManager->create(vehicles[vehicle].startSound, m_game->threeD( ));
    m_soundHorn     = m_soundManager->create(vehicles[vehicle].hornSound, m_game->threeD( ));
    m_soundCrash     = m_soundManager->create(vehicles[vehicle].monoCrashSound, m_game->threeD( ));
    m_soundBrake     = m_soundManager->create(vehicles[vehicle].brakeSound, m_game->threeD( ));
    if (vehicles[vehicle].backfireSound)
        m_soundBackfire = m_soundManager->create(vehicles[vehicle].backfireSound, m_game->threeD( ));
    m_soundMiniCrash= m_soundManager->create(IDR_CRASH_SHORT, m_game->threeD( ));
    m_soundBump1    = m_soundManager->create(IDR_BUMP1, m_game->threeD( ));
    if (m_game->threeD( ))
    {
        m_soundEngine->initializeBuffer3D( );
        m_soundStart->initializeBuffer3D( );
        m_soundHorn->initializeBuffer3D( );
        m_soundCrash->initializeBuffer3D( );
        m_soundBrake->initializeBuffer3D( );
        if (m_soundBackfire != 0)
            m_soundBackfire->initializeBuffer3D( );
        m_soundMiniCrash->initializeBuffer3D( );
        m_soundBump1->initializeBuffer3D( );
    }
/*
    Char soundFile[64];
    sprintf(soundFile, "race\\info\\front%d", playerNumber+1);
    m_soundInFront = m_game->loadLanguageSound(soundFile);
    sprintf(soundFile, "race\\info\\tail%d", playerNumber+1);
    m_soundOnTail  = m_game->loadLanguageSound(soundFile);
*/
RACE("***** successfully loaded vehicle%d for player %d",     m_carType, m_playerNumber);
}


ComputerPlayer::~ComputerPlayer( )
{
    RACE("(-) ComputerPlayer");
    SAFE_DELETE(m_soundEngine);
    SAFE_DELETE(m_soundHorn);
    SAFE_DELETE(m_soundStart);
    SAFE_DELETE(m_soundCrash);
    SAFE_DELETE(m_soundBrake);
        if (m_soundBackfire != 0)
        SAFE_DELETE(m_soundBackfire);
    SAFE_DELETE(m_soundMiniCrash);
    SAFE_DELETE(m_soundBump1);

//    SAFE_DELETE(m_soundInFront);
//    SAFE_DELETE(m_soundOnTail);
}



void
ComputerPlayer::initialize(Int positionX, Int positionY, Int trackLength)
{
    RACE("ComputerPlayer::initialize");
    m_positionX = positionX;
    m_positionY = positionY;
    m_trackLength = trackLength;
    m_laneWidth = m_track->laneWidth();
//RACE("m_laneWidth = %d", m_laneWidth);
}


void
ComputerPlayer::finalize( )
{
    RACE("ComputerPlayer::finalize");
    m_soundEngine->stop( );
}

void
ComputerPlayer::pendingStart( )
{
    RACE("ComputerPlayer::pendingStart");
    Int randomTime = random(100);
    Float startTime = 1.5f + (3.0f*randomTime)/100;
    pushEvent(Event::carComputerStart, startTime);
}


void 
ComputerPlayer::start( )
{
    RACE("ComputerPlayer::start");
    pushEvent(Event::carStart, m_soundStart->length()-0.1f);
    m_soundStart->play( );
    m_speed = 0;
    m_prevFrequency = m_idlefreq;
    m_frequency = m_idlefreq;
    m_prevBrakeFrequency = 0;
    m_brakeFrequency = 0;
    m_switchingGear = 0;
    m_state = starting;
}


void
ComputerPlayer::crash(Int newPosition)
{
    RACE("ComputerPlayer::crash");
    m_speed = 0;
    m_soundCrash->play( );
    m_soundEngine->stop( );
    m_soundEngine->reset( );
    m_soundEngine->pan(0);
    m_soundBrake->stop( );
    m_soundBrake->reset( );
    m_soundHorn->stop( );
    m_gear = 1;
    // reposition to the center of the road
    m_positionX = newPosition;
    m_state = crashing;
    pushEvent(Event::carRestart, m_soundCrash->length() + 1.25f);
}


void
ComputerPlayer::miniCrash(Int newPosition)
{
    RACE("ComputerPlayer::miniCrash");
    m_speed = m_speed / 4;
    m_positionX = newPosition;
    m_soundMiniCrash->play( );
}

void 
ComputerPlayer::bump(Int bumpX, Int bumpY, Int bumpSpeed)
{
    RACE("ComputerPlayer::bump(%d, %d)", bumpX, bumpY);
    if (bumpY > 0)
    {
        m_speed -= bumpSpeed;
        m_positionY += bumpY;
    }
    else if (bumpY < 0)
    {
        m_speed -= bumpSpeed;
        m_positionY += bumpY;
    }
    if (bumpX > 0)
    {
        m_positionX += 2*bumpX;
        m_speed -= m_speed/5;
    }
    else if (bumpX < 0)
    {
        m_positionX += 2*bumpX;
        m_speed -= m_speed/5;
    }
    if (m_speed < 0)
        m_speed = 0;
    m_soundBump1->play( );
    horn( );
}


void
ComputerPlayer::stop( )
{
    RACE("ComputerPlayer::stop");
    m_state = stopping;
}


void
ComputerPlayer::quiet( )
{
    m_soundBrake->stop( );
    m_soundHorn->stop( );
    m_soundEngine->volume(80);
    if (m_soundBackfire)
        m_soundBackfire->volume(80);
}


void 
ComputerPlayer::run(Float elapsed, Int playerX, Int playerY)
{
    m_diffX = m_positionX - playerX;
    m_diffY = m_positionY - playerY;
    m_diffY = ((m_diffY % m_trackLength) + m_trackLength) % m_trackLength;
    if (m_diffY > m_trackLength/2)
        m_diffY = (m_diffY - m_trackLength) % m_trackLength;

    if ((!m_horning) && (m_diffY < -10000))
    {
        if (random(2500) == 1)
        {
            Int duration = random(80);
            m_horning = true;
            pushEvent(Event::stopHorn, 0.2f + (Float(duration) / 80.0f));
        }
    }
        
    DirectX::Vector3 relPos(Float(m_diffX) / Float(m_laneWidth), Float(m_diffY) / 12000.0f, 0.0f);
    if (m_game->threeD( ))
    {
        m_soundEngine->position(relPos);
        m_soundStart->position(relPos);
        m_soundHorn->position(relPos);
        m_soundCrash->position(relPos);
        m_soundBrake->position(relPos);
        if (m_soundBackfire != 0)
            m_soundBackfire->position(relPos);
        m_soundBump1->position(relPos);
        m_soundMiniCrash->position(relPos);
    }
    else
    {
        setSoundPosition(m_soundEngine, relPos);
        setSoundPosition(m_soundStart, relPos);
        setSoundPosition(m_soundHorn, relPos);
        setSoundPosition(m_soundCrash, relPos);
        setSoundPosition(m_soundBrake, relPos);
        if (m_soundBackfire != 0)
            setSoundPosition(m_soundBackfire, relPos);
        setSoundPosition(m_soundBump1, relPos);
        setSoundPosition(m_soundMiniCrash, relPos);
    }
    if ((m_state == running) && (m_game->started( )))
    {
        AI(/* playerY */);
        
        m_currentAcceleration = m_acceleration;
        m_currentDeceleration = m_deceleration;
        switch (m_surface)
        {
            case Track::gravel:
                m_currentAcceleration = (m_currentAcceleration*2)/3;
                m_currentDeceleration = (m_currentDeceleration*2)/3;
                break;
            case Track::water:
                m_currentAcceleration = (m_currentAcceleration*3)/5;
                m_currentDeceleration = (m_currentDeceleration*3)/5;
                break;
            case Track::sand:
                m_currentAcceleration = (m_currentAcceleration*3)/8;
                m_currentDeceleration = (m_currentDeceleration*5)/4;
                break;
            case Track::snow:
                // m_currentAcceleration = m_currentAcceleration;
                m_currentDeceleration = (m_currentDeceleration)/2;
                break;
            default:
                break;
        }

        if (m_currentThrottle == 0)
        {
            m_thrust = m_currentBrake;
            if (m_currentBrake != 0)
            {
                if ((m_surface == Track::asphalt) && (!m_soundBrake->playing( )))
                    m_soundBrake->play( );
                else if (m_surface != Track::asphalt)
                    m_soundBrake->stop( );
            }
        }
        else if (m_currentBrake == 0)
        {
            m_thrust = m_currentThrottle;
            if (m_soundBrake->playing( ))
                m_soundBrake->stop( );
        }
        else if (-m_currentBrake > m_currentThrottle)
            m_thrust = m_currentBrake;
        if (m_thrust > 10)
            m_speedDiff = Int(elapsed*m_thrust*m_currentAcceleration);
        else if (m_thrust < -10)
            m_speedDiff = Int(elapsed*m_thrust*m_currentDeceleration);
        else
            m_speedDiff = Int(elapsed*-1000);
        if (m_speedDiff > 0)
            m_speedDiff = (Int)(m_speedDiff * (2.0f - ((m_topspeed + m_speed)*1.0f/(2.0f*m_topspeed))));
        m_speed += m_speedDiff;
        if (m_speed > m_topspeed)
            m_speed = m_topspeed;
        if (m_speed < 0)
            m_speed = 0;
        if ((m_thrust < -50) && (m_speed > 5000))
            m_currentSteering = m_currentSteering*2/3;

        m_positionY += Int(m_speed*elapsed);
        if (m_surface != Track::snow)
            m_positionX += Int(m_currentSteering*elapsed*m_steering*((5000.0f + m_speed*m_steeringFactor/100)/m_topspeed));
        else
            m_positionX += Int(m_currentSteering*elapsed*(m_steering*1.44f)*((5000.0f + m_speed*m_steeringFactor/100)/m_topspeed));

        // update frequencies
        if (m_frame % 4 == 0)
        {
            m_frame = 0;
            m_brakeFrequency = 11025 + 22050*m_speed/m_topspeed;
            if (m_brakeFrequency != m_prevBrakeFrequency)
            {
                m_soundBrake->frequency(m_brakeFrequency);
                m_prevBrakeFrequency = m_brakeFrequency;
            }
            updateEngineFreq( );
        }
        Track::Road road = m_track->roadComputer(m_positionY);
        if (!finished( ))
            evaluate(road);
    }
    else if (m_state == stopping)
    {
        m_speed -= Int(elapsed*100*m_deceleration);
        if (m_speed < 0)
            m_speed = 0;
        // update frequencies
        if (m_frame % 4 == 0)
        {
            m_frame = 0;
            updateEngineFreq( );
        }
        ++m_frame;
    }

    if ((m_horning) && (m_state == running))
    {
        if (!m_soundHorn->playing( ))
            m_soundHorn->play(0, true);
    }
    else
    {
        if (m_soundHorn->playing( ))
            m_soundHorn->stop( );
    }

    // Handle events
    Event* e = 0;
    while (e = m_eventList.next(e))
    {
        if (e->time < m_game->currentTime( ))
        {
            m_eventList.purge(e);
            switch (e->type)
            {
		case Event::carStart:
                m_soundEngine->frequency(m_idlefreq);
                m_soundEngine->play(0, true);
                m_state = running;
                break;
            case Event::carComputerStart:
                start( );
                break;
            case Event::carRestart:
                start( );
                break;
            case Event::inGear:
                m_switchingGear = 0;
                break;
            case Event::stopHorn:
                m_horning = false;
                break;
            case Event::startHorn:
                m_horning = true;
                break;
            default:
                break;
            }
            delete e;
            e = 0;
        }
    }
}


void 
ComputerPlayer::evaluate(Track::Road road)
{
    if ((m_state == running) && (m_game->started( )))
    {
        if (m_frame % 4 == 0)
        {
            m_relPos = (m_positionX - road.left) / (Float(m_laneWidth) *2.0f);
            /* if (m_relPos - 0.5f < 0)
            {
                m_panPos = Int((m_relPos - 0.5f)*(m_relPos - 0.5f)*(-100));
                m_soundEngine->pan(m_panPos);
            }
            else
            {
                m_panPos = Int((m_relPos - 0.5f)*(m_relPos - 0.5f)*100);
                m_soundEngine->pan(m_panPos);
            } */
            if ((m_relPos < 0) || (m_relPos > 1))
            {
                if (m_speed < m_topspeed/2)
                    miniCrash((road.right + road.left)/2);
                else
                    crash((road.right + road.left)/2);
            }
        }
    }
    m_surface = road.surface;
    ++m_frame;
}


void 
ComputerPlayer::pushEvent(Event::Type type, Float time)
{
    Event* e = new Event;
    e->type = type;
    e->time = m_game->currentTime( ) + time;
    m_eventList.push(e);
}


void 
ComputerPlayer::updateEngineFreq( )
{
    Int gearRange = m_topspeed/(m_gears+1);
    if ((m_speed / gearRange) < 2)
    {
        Float gearSpeed = (m_speed / (2.0f*gearRange));
        m_frequency = Int(gearSpeed*(m_topfreq - m_idlefreq)) + m_idlefreq;
    }
    else
    {
        Int gear = m_speed / gearRange;
        Float gearSpeed = (m_speed - gear*gearRange)/(1.0f*gearRange);
        if (gearSpeed < 0.07f)
        {
            m_frequency = Int(((0.07f - gearSpeed)/0.07f)*(m_topfreq - m_shiftfreq) + m_shiftfreq);
            if (m_soundBackfire != 0)
            {
                if (m_backfirePlayedAuto == false)
                {
                    if ((random(5) == 1) && (!m_soundBackfire->playing()))
                        m_soundBackfire->play( );
                }
                m_backfirePlayedAuto = true;
            }
        }
        else
        {
            m_frequency = Int(gearSpeed*(m_topfreq - m_shiftfreq) + m_shiftfreq);
            if (m_soundBackfire != 0)
            {
                if (m_backfirePlayedAuto == true)
                    m_backfirePlayedAuto = false;
            }
        }        
    }
    if (m_frequency != m_prevFrequency)
    {
        m_soundEngine->frequency(m_frequency);
        m_prevFrequency = m_frequency;
    }
}


Int
ComputerPlayer::calculateAcceleration( )
{
    Int gearSpeed = m_topspeed/m_gears;
    Int gearCenter = (Int)(gearSpeed*(m_gear - 0.82f));
    m_speedDiff = m_speed - gearCenter;
    Float relSpeedDiff = m_speedDiff/(gearSpeed*1.0f);
    if (relSpeedDiff > 1.1f)
    {
        m_switchingGear = 1;
        ++m_gear;
        pushEvent(Event::inGear, 0.2f);
    }
    else if (relSpeedDiff < -1.1f)
    {
        m_switchingGear = -1;
        --m_gear;
        pushEvent(Event::inGear, 0.2f);
    }
    if (absval<Float>(relSpeedDiff) < 1.9f)
    {
        Int acceleration = Int(100*(0.5f+cosf(relSpeedDiff*(DirectX::Pi)*0.5f)));
        if (acceleration < 5)
            return 5;
        else 
            return acceleration;
    }
    else
    {
        Int acceleration = Int(100*(0.5f+cosf(0.95f*(DirectX::Pi))));
        if (acceleration < 5)
            return 5;
        else
            return acceleration;
    }
}

void 
ComputerPlayer::AI(/* Int playerPosY */)
{
    /* switch (m_difficulty)
    {
        case 0: // easy
            if (playerPosY < m_positionY)
            {
                if (m_speed > m_topspeed*(35 + m_random/20)/60)
                    m_currentThrottle = 0;
                else
                    m_currentThrottle = 55 + m_random/10;
            }
            else 
                m_currentThrottle = 80;
            break;
        case 1: // normal
            if (playerPosY < m_positionY)
            {
                if (m_speed > m_topspeed*(45 + m_random/20)/60)
                    m_currentThrottle = 0;
                else
                    m_currentThrottle = 65 + m_random/10;
            }
            else 
                m_currentThrottle = 100;
            break;
        case 2: // hard
            if (playerPosY < m_positionY)
            {
                if (m_speed > m_topspeed*(55 + m_random/20)/60)
                    m_currentThrottle = 0;
                else
                    m_currentThrottle = 75 + m_random/10;
            }
            else 
                m_currentThrottle = 100;
            break;
        default:
            break;
    } */

    Track::Road road = m_track->roadComputer(m_positionY);
    m_relPos = Float(m_positionX - road.left) / (Float(m_laneWidth) *2.0f);
    Track::Road nextRoad = m_track->roadComputer(m_positionY + CALLLENGTH);
    m_nextRelPos = Float(m_positionX - nextRoad.left) / (Float(m_laneWidth) * 2.0f);
    m_currentThrottle = 100;
    m_currentSteering = 0;
    if ((road.type == Track::hairpinLeft) || (nextRoad.type == Track::hairpinLeft))
    {
        switch (m_difficulty)
        {
        case 0: // easy
            if (m_relPos > 0.65f)
                m_currentSteering = -100;
            // m_currentThrottle = 100;
            break;
        case 1: // normal
            if (m_relPos > 0.55f)
                m_currentSteering = -100;
            m_currentThrottle = 66;
            break;
        case 2: // hard
            if (m_relPos > 0.55f)
                m_currentSteering = -100;
            m_currentThrottle = 33;
            /* if ((m_currentBrake == 0) && (m_speed >= m_topspeed/2))
            {
                m_currentBrake = -100;
                m_currentThrottle = 0;
            }
            else if ((m_currentBrake == -100) && (m_speed <= m_topspeed/3))
            {
                m_currentBrake = 0;
                m_currentThrottle = 50;
            }
            else
            {
                m_currentBrake = -25;
                m_currentThrottle = 0;
            } */
            break;
        default:
            break;
        }
    }
    else if ((road.type == Track::hairpinRight) || (nextRoad.type == Track::hairpinRight))
    {
        switch (m_difficulty)
        {
        case 0: // easy
            if (m_relPos < 0.35f)
                m_currentSteering = 100;
            // m_currentThrottle = 100;
            break;
        case 1: // normal
            if (m_relPos < 0.45f)
                m_currentSteering = 100;
            m_currentThrottle = 66;
            break;
        case 2: // hard
            if (m_relPos < 0.45f)
                m_currentSteering = 100;
            m_currentThrottle = 33;
            /* if ((m_currentBrake == 0) && (m_speed >= m_topspeed/2))
            {
                m_currentBrake = -100;
                m_currentThrottle = 0;
            }
            else if ((m_currentBrake == -100) && (m_speed <= m_topspeed/3))
            {
                m_currentBrake = 0;
                m_currentThrottle = 50;
            }
            else
            {
                m_currentBrake = -25;
                m_currentThrottle = 0;
            } */
            break;
        default:
            break;
        }
    }
    else if (m_relPos < 0.40f)
    {
        if (m_relPos > 0.2f)
        {
            switch (m_difficulty)
            {
            case 0: // easy
                m_currentSteering = 100 - m_random/5;
                break;
            case 1: // normal
                m_currentSteering = 100 - m_random/10;
                break;
            case 2: // hard
                m_currentSteering = 100 - m_random/25;
                break;
            default:
                break;
            }
        }
        else
        {
            switch (m_difficulty)
            {
            case 0: // easy
                m_currentSteering = 100 - m_random/10;
                break;
            case 1: // normal
                m_currentSteering = 100 - m_random/20;
                m_currentThrottle = 75;
                break;
            case 2: // hard
                m_currentSteering = 100;
                m_currentThrottle = 50;
                break;
            default:
                break;
            }
        }
    }
    else if (m_relPos > 0.6f)
    {
        if (m_relPos < 0.8f)
        {
            switch (m_difficulty)
            {
            case 0: // easy
                m_currentSteering = -100 + m_random/5;
                break;
            case 1: // normal
                m_currentSteering = -100 + m_random/10;
                break;
            case 2: // hard
                m_currentSteering = -100 / m_random/25;
                break;
            default:
                break;
            }
        }
        else
        {
            switch (m_difficulty)
            {
            case 0: // easy
                m_currentSteering = -100 + m_random/10;
                break;
            case 1: // normal
                m_currentSteering = -100 + m_random/20;
                m_currentThrottle = 75;
                break;
            case 2: // hard
                m_currentSteering = -100;
                m_currentThrottle = 50;
                break;
            default:
                break;
            }
        }
    }
}

void 
ComputerPlayer::horn( )
{
    int duration = random(80);
    pushEvent(Event::startHorn, 0.3f);
    pushEvent(Event::stopHorn, 0.5f + duration/80.0f);
}

void
ComputerPlayer::setSoundPosition(DirectX::Sound* sound, DirectX::Vector3 relPos)
{
    Float distance = sqrt(sqrt(relPos.x*relPos.x) + sqrt(relPos.y*relPos.y));
    if (relPos.x < -2.0f)
{
        sound->pan(-100);
}
    else if (relPos.x > 2.0f)
{
        sound->pan(100);
}
    else
        sound->pan(Int(relPos.x*50.0f));
sound->volume(Int(100.0f - (distance*10.0f)));
}

void
ComputerPlayer::pause( )
{
    if (m_state == starting)
        m_soundStart->stop( );
    else if ((m_state == running) || (m_state == stopping))
        m_soundEngine->stop( );
    if (m_soundBrake->playing( ))
        m_soundBrake->stop( );
    if (m_soundHorn->playing( ))
        m_soundHorn->stop( );
    if ((m_soundBackfire) && (m_soundBackfire->playing( )))
    {
        m_soundBackfire->stop( );
        m_soundBackfire->reset( );
    }
    if (m_soundCrash->playing( ))
    {
        m_soundCrash->stop( );
        m_soundCrash->reset( );
    }
}

void
ComputerPlayer::unpause( )
{
    if (m_state == starting)
        m_soundStart->play( );
    else if ((m_state == running) || (m_state == stopping))
        m_soundEngine->play(0, true);
}
