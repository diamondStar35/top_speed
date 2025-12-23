/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "NetworkPlayer.h"
#include "resource.h"
#include "Common/If/Algorithm.h"
#include "Car.h"

extern Car::Parameters vehicles[NVEHICLES];

NetworkPlayer::NetworkPlayer( ) :
    m_number(0),
    m_engineRunning(false),
    m_braking(false),
    m_horning(false),
    m_backfiring(false),
    m_game(0),
    m_soundEngine(0),
    m_soundHorn(0),
    m_soundBackfire(0),
    m_soundStart(0),
    m_soundCrash(0),
    m_soundBrake(0),
//    m_soundInFront(0),
//    m_soundOnTail(0),
    m_initialized(false),
    m_frame(1),
    m_finished(false),
    m_backfirePlayed(false),
    m_prevFrequency(0),
    m_frequency(0),
    m_prevBrakeFrequency(0),
    m_brakeFrequency(0),
    m_diffX(0),
    m_diffY(0),
    m_state(running),
    m_speed(0),
    m_positionX(0),
    m_positionY(0)
{
    RACE("(+) NetworkPlayer");
}

NetworkPlayer::~NetworkPlayer( )
{
    RACE("(-) NetworkPlayer");
    if (m_initialized)
        finalize();
}

void
NetworkPlayer::initialize(Game* game, UInt number, UInt vehicle, Int trackLength, UInt laneWidth)
{
    RACE("NetworkPlayer::initialize : number = %d, cartype = %d", number, vehicle);
    m_game        = game;
    m_number      = number;
    m_trackLength = trackLength;
    m_laneWidth   = laneWidth;
    m_carType     = (CarType)vehicle;

    m_topspeed      = vehicles[vehicle].topspeed;
    m_deceleration  = vehicles[vehicle].deceleration;
    m_idlefreq      = vehicles[vehicle].idlefreq;
    m_topfreq       = vehicles[vehicle].topfreq;
    m_shiftfreq     = vehicles[vehicle].shiftfreq;
    m_gears         = vehicles[vehicle].gears;
    m_frequency     = m_idlefreq;
    m_soundEngine   = m_game->soundManager()->create(vehicles[vehicle].engineSound, m_game->threeD( ));
    m_soundStart    = m_game->soundManager()->create(vehicles[vehicle].startSound, m_game->threeD( ));
    m_soundHorn     = m_game->soundManager()->create(vehicles[vehicle].hornSound, m_game->threeD( ));
    if (vehicles[vehicle].backfireSound)
        m_soundBackfire = m_game->soundManager()->create(vehicles[vehicle].backfireSound, m_game->threeD( ));
    m_soundCrash     = m_game->soundManager()->create(vehicles[vehicle].monoCrashSound, m_game->threeD( ));
    if (m_game->threeD( ))
    {
        m_soundBrake     = m_game->soundManager()->create(vehicles[vehicle].brakeSound, m_game->threeD( ));
        m_soundEngine->initializeBuffer3D( );
        m_soundStart->initializeBuffer3D( );
        m_soundHorn->initializeBuffer3D( );
        if (m_soundBackfire != 0)
            m_soundBackfire->initializeBuffer3D( );
        m_soundCrash->initializeBuffer3D( );
        m_soundBrake->initializeBuffer3D( );
    }
/*
    Char soundFile[64];
    sprintf(soundFile, "race\\info\\front%d", m_number+1);
    m_soundInFront = m_game->loadLanguageSound(soundFile);
    sprintf(soundFile, "race\\info\\tail%d", m_number+1);
    m_soundOnTail  = m_game->loadLanguageSound(soundFile);
  */  
    m_initialized = true;
}


void
NetworkPlayer::finalize( )
{
    RACE("NetworkPlayer[%d]::finalize", m_number);
    m_initialized = false;
    m_number = 0;
    SAFE_DELETE(m_soundEngine);
    SAFE_DELETE(m_soundHorn);
    if (m_soundBackfire != 0)
        SAFE_DELETE(m_soundBackfire);
    SAFE_DELETE(m_soundStart);
    SAFE_DELETE(m_soundCrash);
    if (m_soundBrake != 0)
        SAFE_DELETE(m_soundBrake);
//    SAFE_DELETE(m_soundInFront);
//    SAFE_DELETE(m_soundOnTail);
}

void
NetworkPlayer::run(Float elapsed, Int playerX, Int playerY)
{
    m_diffX = m_positionX - playerX;
    m_diffY = m_positionY - playerY;
    m_diffY = ((m_diffY%m_trackLength) + m_trackLength) % m_trackLength;
    if (m_diffY > m_trackLength/2)
        m_diffY = (m_diffY - m_trackLength)%m_trackLength;
    DirectX::Vector3 relPos(Float(m_diffX) / Float(m_laneWidth), Float(m_diffY) / 12000.0f, 0.0f);
    if (m_game->threeD( ))
    {
        m_soundEngine->position(relPos);
        m_soundStart->position(relPos);
        m_soundHorn->position(relPos);
        if (m_soundBackfire != 0)
            m_soundBackfire->position(relPos);
        m_soundCrash->position(relPos);
        m_soundBrake->position(relPos);
    }
    else
    {
        setSoundPosition(m_soundEngine, relPos);
        setSoundPosition(m_soundStart, relPos);
        setSoundPosition(m_soundHorn, relPos);
        if (m_soundBackfire != 0)
            setSoundPosition(m_soundBackfire, relPos);
        setSoundPosition(m_soundCrash, relPos);
//        setSoundPosition(m_soundBrake, relPos);
    }
    if (m_state == running)
    {
        if (m_frame % 4 == 0)
        {
            m_frame = 0;
            if (m_frequency != m_prevFrequency)
            {
                m_soundEngine->frequency(m_frequency);
                m_prevFrequency = m_frequency;
            }
    if (m_game->threeD( ))
{
            m_brakeFrequency = 11025 + 22050*m_speed/m_topspeed;
            if (m_brakeFrequency != m_prevBrakeFrequency)
            {
                m_soundBrake->frequency(11025 + 22050*m_speed/m_topspeed);
                m_prevBrakeFrequency = m_brakeFrequency;
            }
            if (m_speed <= 5000)
            {
                m_soundBrake->volume(100 - (50 - (m_speed/100)));
            }
            else
            {
                m_soundBrake->volume(100);
            }
}
        }
        ++m_frame;
        if (m_engineRunning)
        {
            if (m_soundEngine->playing() == false)
                m_soundEngine->play(0, true);
        }
        else
        {
            if (m_soundEngine->playing())
                m_soundEngine->stop( );
                m_soundEngine->reset( );
        }
        if(m_game->threeD( ))
        {
            if (m_braking)
            {
                if (!m_soundBrake->playing())
                    m_soundBrake->play(0, true);
            }
            else
            {
                if (m_soundBrake->playing())
                    m_soundBrake->stop( );
            }
        }
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
    if (m_horning)
    {
       if (m_soundHorn->playing() == false)
            m_soundHorn->play(0, true);
    }
    else
    {
        if (m_soundHorn->playing())
            m_soundHorn->stop( );
    }    
    if (m_soundBackfire != 0)
    {
        if (m_backfiring)
        {
            if ((m_soundBackfire->playing() == false) && (m_backfirePlayed == false))
            {
                m_soundBackfire->play();
                m_backfirePlayed = true;
            }
        }
        else
        {
            if (m_backfirePlayed == true)
                m_backfirePlayed = false;
        }
    }
}


void 
NetworkPlayer::updateEngineFreq( )
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
        }
        else
        {
            m_frequency = Int(gearSpeed*(m_topfreq - m_shiftfreq) + m_shiftfreq);
        }        
    }
    if (m_frequency != m_prevFrequency)
    {
        m_soundEngine-> frequency(m_frequency);
//RACE("NetworkPlayer::updateEngineFreq : Frequency updated to %d, speed is %d", m_frequency, m_speed);
        m_prevFrequency = m_frequency;
    }
}


/*
void
NetworkPlayer::sayInFront( )
{
    if (m_initialized)
    {
//        RACE("NetworkPlayer : 'player %d is in front of you'", m_number+1);
        m_soundInFront->play( );
    }
}

void
NetworkPlayer::sayOnTail( )
{
    if (m_initialized)
    {
//        RACE("NetworkPlayer : 'player %d is on your tail'", m_number+1);
        m_soundOnTail->play( );
    }
}
*/

void
NetworkPlayer::setSoundPosition(DirectX::Sound* sound, DirectX::Vector3 relPos)
{
    Float distance = sqrt(sqrt(relPos.x*relPos.x) + sqrt(relPos.y*relPos.y));
    if (relPos.x < -2.0f)
        sound->pan(-100);
    else if (relPos.x > 2.0f)
        sound->pan(100);
    else
        sound->pan(Int(relPos.x*50.0f));
sound->volume(Int(100.0f - (distance*10.0f)));
}

void
NetworkPlayer::engineRunning(Boolean b)
{ 
    m_engineRunning = b;
    if (m_initialized)
    {
        if ((m_soundEngine->playing( )) && !b)
        {
            m_soundEngine->stop( );
            m_soundEngine->reset( );
        }
    }
}

void
NetworkPlayer::stop( )
{
    RACE("NetworkPlayer::stop");
    if (m_game->threeD( ))
        m_soundBrake->stop( );
    m_state = stopping;
}

void
NetworkPlayer::quiet( )
{
    m_soundEngine->volume(80);
    if (m_soundBackfire)
        m_soundBackfire->volume(80);
}
