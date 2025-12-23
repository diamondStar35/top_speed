#include "Car.h"
#include "Track.h"
#include "Common/If/Algorithm.h"
#include "DxCommon/If/Common.h"
#include "resource.h"
#include "RaceInput.h"
#include "CarDefs.h"

#define MAXSURFACEFREQ 100000


Car::Car(Game* game, Track* track, UInt vehicle, Char* vehicleFile) :
    m_track(track),
    m_surface(track->definition()[0].surface),
    m_gear(1),
    m_state(stopped),
    m_manualTransmission(false),
    m_hasWipers(0),
    m_switchingGear(0),
    m_listener(0),
    m_effectStart(0),
    m_effectCrash(0),
    m_effectSpring(0),
    m_effectGravel(0),
    m_effectEngine(0),
    m_effectCurbLeft(0),
    m_effectCurbRight(0),
    m_effectBumpLeft(0),
    m_effectBumpRight(0),
    m_game(game),
    m_soundManager(game->soundManager( )),
    m_soundThrottle(0),
    m_soundBackfire(0),
    m_backfirePlayed(false),
    m_backfirePlayedAuto(false),
    m_thrust(0),
    m_prevFrequency(0),
    m_prevBrakeFrequency(0),
    m_brakeFrequency(0),
    m_prevSurfaceFrequency(0),
    m_surfaceFrequency(0),
    m_prevThrottleVolume(0.0f),
    m_laneWidth(0),
    m_relPos(0),
    m_panPos(0),
    m_currentSteering(0),
    m_currentThrottle(0),
    m_currentBrake(0),
    m_currentAcceleration(0),
    m_currentDeceleration(0),
    m_speedDiff(0),
    m_factor1(100),
    m_factor2(1.0),
    m_speed(0),
    m_frame(1),
    m_throttleVolume(0.0f),
    m_userDefined(false),
    m_soundEngine(0),
    m_soundStart(0),
    m_soundHorn(0),
        m_soundCrash(0),
    m_soundBrake(0)
    {
    RACE("(+) Car");
    Car::Parameters officialParameters;
    if (vehicleFile == NULL)
    {
        officialParameters = vehicles[vehicle];
        m_carType = (CarType)vehicle;
        m_acceleration  = officialParameters.acceleration;
        m_deceleration  = officialParameters.deceleration;
        m_topspeed      = officialParameters.topspeed;
        m_idlefreq      = officialParameters.idlefreq;
        m_topfreq       = officialParameters.topfreq;
        m_shiftfreq     = officialParameters.shiftfreq;
        m_gears         = officialParameters.gears;
        m_steering      = officialParameters.steering;
        m_steeringFactor= officialParameters.steeringFactor;
        m_frequency     = m_idlefreq;
        m_soundEngine   = m_soundManager->create(officialParameters.engineSound);
        // m_soundEngine->playInSoftware(true);
        m_soundStart    = m_soundManager->create(officialParameters.startSound);
        m_soundHorn     = m_soundManager->create(officialParameters.hornSound);
        if (officialParameters.throttleSound)
        {
            m_soundThrottle = m_soundManager->create(officialParameters.throttleSound);
            // m_soundThrottle->playInSoftware(true);
        }
        m_soundCrash     = m_soundManager->create(officialParameters.crashSound);
        m_soundBrake     = m_soundManager->create(officialParameters.brakeSound);
        if (officialParameters.backfireSound)
            m_soundBackfire = m_soundManager->create(officialParameters.backfireSound);
        if ((officialParameters.hasWipers == 1) && (m_track->weather() == Track::rain))
            // ugly, but effective
            m_hasWipers = 1;
    }
    else
    {
        m_userDefined = true;
        Int length = ::strlen(vehicleFile);
        ::strncpy(m_customFile, vehicleFile, length-4);
        m_customFile[length-4] = '\0';
        File file(vehicleFile, File::read);
        file.readInt("acceleration", m_acceleration, 10);
        file.readInt("deceleration", m_deceleration, 40);
        file.readInt("topspeed", m_topspeed, 15000);
        file.readInt("idlefreq", m_idlefreq, 11000);
        file.readInt("topfreq", m_topfreq, 50000);
        file.readInt("shiftfreq", m_shiftfreq, 40000);
        file.readInt("numberofgears", m_gears, 5);
        file.readInt("steering", m_steering, 100);
        file.readInt("steeringfactor", m_steeringFactor, 40);
        Char    engineSound[64];
        Char    throttleSound[64];
        Char    startSound[64];
        Char    hornSound[64];
        Char    backfireSound[64];
        Char    crashSound[64];
//        Char    monoCrashSound[64];
        Char    brakeSound[64];
        file.readString("enginesound", engineSound, 128, "builtin1");
        file.readString("throttlesound", throttleSound, 128, "");
        file.readString("startsound", startSound, 128, "builtin1");
        file.readString("hornsound", hornSound, 128, "builtin1");
        file.readString("backfiresound", backfireSound, 128, "");
        file.readString("crashsound", crashSound, 128, "builtin1");
//        file.readString("monoCrashsound", monoCrashSound, 128, "builtin1");
        file.readString("brakesound", brakeSound, 128, "builtin1");
        if (m_track->weather() == Track::rain)
            file.readInt("haswipers", m_hasWipers, 1);

        m_carType	  = (CarType)0; // default value

        // load the engine sound
        if (strncmp(engineSound, "builtin", 7) == 0)
        {
            m_soundEngine = m_soundManager->create(vehicles[atoi(engineSound+7)-1].engineSound);
            // m_soundEngine->playInSoftware(true);
        }
        else
        {
            Char engineSound2[150];
            sprintf(engineSound2, "Vehicles\\%s", engineSound);
            RACE("Car::Car : loading engine sound '%s'", engineSound2);
            m_soundEngine = m_soundManager->create(engineSound2);
            // m_soundEngine->playInSoftware(true);
        }

        // load the throttle sound
        if (strlen(throttleSound) > 0)
        {
            if (strncmp(throttleSound, "builtin", 7) == 0)
            {
                UInt throttleSound2 = atoi(throttleSound+7)-1;
                if (vehicles[throttleSound2].throttleSound != 0)
                {
                    m_soundThrottle = m_soundManager->create(vehicles[throttleSound2].throttleSound);
                    // m_soundThrottle->playInSoftware(true);
                }
            }
            else
            {
                Char throttleSound2[150];
                sprintf(throttleSound2, "Vehicles\\%s", throttleSound);
                RACE("Car::Car : loading throttle sound '%s'", throttleSound2);
                m_soundThrottle = m_soundManager->create(throttleSound2);
                // m_soundThrottle->playInSoftware(true);
            }
        }
        
        // load the start sound
        if (strncmp(startSound, "builtin", 7) == 0)
        {
            m_soundStart = m_soundManager->create(vehicles[atoi(startSound+7)-1].startSound);
        }
        else
        {
            Char startSound2[150];
            sprintf(startSound2, "Vehicles\\%s", startSound);
            RACE("Car::Car : loading start sound '%s'", startSound2);
            m_soundStart = m_soundManager->create(startSound2);
        }

        // load the horn sound
        if (strncmp(hornSound, "builtin", 7) == 0)
        {
            m_soundHorn = m_soundManager->create(vehicles[atoi(hornSound+7)-1].hornSound);
        }
        else
        {
            Char hornSound2[150];
            sprintf(hornSound2, "Vehicles\\%s", hornSound);
            RACE("Car::Car : loading horn sound '%s'", hornSound2);
            m_soundHorn = m_soundManager->create(hornSound2);
        }

        // load the crash sound
        if (strncmp(crashSound, "builtin", 7) == 0)
        {
            m_soundCrash = m_soundManager->create(vehicles[atoi(crashSound+7)-1].crashSound);
        }
        else
        {
            Char crashSound2[150];
            sprintf(crashSound2, "Vehicles\\%s", crashSound);
            RACE("Car::Car : loading crash sound '%s'", crashSound2);
            m_soundCrash = m_soundManager->create(crashSound2);
        }

        // load the brake sound
        if (strncmp(brakeSound, "builtin", 7) == 0)
        {
            m_soundBrake = m_soundManager->create(vehicles[atoi(brakeSound+7)-1].brakeSound);
        }
        else
        {
            Char brakeSound2[150];
            sprintf(brakeSound2, "Vehicles\\%s", brakeSound);
            RACE("Car::Car : loading brake sound '%s'", brakeSound2);
            m_soundBrake = m_soundManager->create(brakeSound2);
        }

        // load the backfire sound
        if (strlen(backfireSound) > 0)
        {
            if (strncmp(backfireSound, "builtin", 7) == 0)
            {
                UInt backfireSound2 = atoi(backfireSound+7)-1;
                if (vehicles[backfireSound2].backfireSound != 0)
                {
                    m_soundBackfire = m_soundManager->create(vehicles[backfireSound2].backfireSound);
                }
            }
            else
            {
                Char backfireSound2[150];
                sprintf(backfireSound2, "Vehicles\\%s", backfireSound);
                RACE("Car::Car : loading backfire sound '%s'", backfireSound2);
                m_soundBackfire = m_soundManager->create(backfireSound2);
            }
        }
    }

    if (m_hasWipers == 1)
        m_soundWipers	= m_soundManager->create(IDR_WIPERS);
    m_soundAsphalt  = m_soundManager->create(IDR_ASPHALT);
    m_soundAsphalt->playInSoftware(true);
    m_soundGravel   = m_soundManager->create(IDR_GRAVEL);
    m_soundGravel->playInSoftware(true);
    m_soundWater    = m_soundManager->create(IDR_WATER);
    m_soundWater->playInSoftware(true);
    m_soundSand   = m_soundManager->create(IDR_SAND);
    m_soundSand->playInSoftware(true);
    m_soundSnow   = m_soundManager->create(IDR_SNOW);
    m_soundSnow->playInSoftware(true);
    m_soundMiniCrash= m_soundManager->create(IDR_CRASH_SHORT);
    m_soundBump1    = m_soundManager->create(IDR_BUMP1);
    m_soundBadSwitch= m_soundManager->create(IDR_BADSWITCH);
    
    if (m_game->inputManager()->joystick( ))
    {
        if ((m_game->inputManager()->joystick( )->FFcapable( )) &&
            (m_game->raceSettings().forceFeedback) &&
            (m_game->raceSettings().useJoystick))
        {
            m_effectStart = new DirectX::ForceFeedbackEffect(m_game->inputManager()->joystick(),
                                                            "effects\\carstart.ffe");
            m_effectCrash = new DirectX::ForceFeedbackEffect(m_game->inputManager()->joystick(),
                                                            "effects\\crash.ffe");
            m_effectSpring = new DirectX::ForceFeedbackEffect(m_game->inputManager()->joystick(),
                                                            "effects\\spring.ffe");
            m_effectEngine = new DirectX::ForceFeedbackEffect(m_game->inputManager()->joystick(),
                                                            "effects\\engine.ffe");
            m_effectCurbLeft = new DirectX::ForceFeedbackEffect(m_game->inputManager()->joystick(),
                                                            "effects\\curbleft.ffe");
            m_effectCurbRight = new DirectX::ForceFeedbackEffect(m_game->inputManager()->joystick(),
                                                            "effects\\curbright.ffe");
            m_effectGravel = new DirectX::ForceFeedbackEffect(m_game->inputManager()->joystick(),
                                                            "effects\\gravel.ffe");
            m_effectBumpLeft = new DirectX::ForceFeedbackEffect(m_game->inputManager()->joystick(),
                                                            "effects\\bumpleft.ffe");
            m_effectBumpRight = new DirectX::ForceFeedbackEffect(m_game->inputManager()->joystick(),
                                                            "effects\\bumpright.ffe");
            m_effectGravel->gain(0);
        }
    }
}


Car::~Car( )
{
    RACE("(-) Car");
    SAFE_DELETE(m_soundEngine);
    SAFE_DELETE(m_soundThrottle);
    SAFE_DELETE(m_soundHorn);
    SAFE_DELETE(m_soundStart);
    SAFE_DELETE(m_soundCrash);
    SAFE_DELETE(m_soundBrake);
    SAFE_DELETE(m_soundAsphalt);
    SAFE_DELETE(m_soundGravel);
    SAFE_DELETE(m_soundWater);
    SAFE_DELETE(m_soundSand);
    SAFE_DELETE(m_soundSnow);
    SAFE_DELETE(m_soundMiniCrash);
    if (m_hasWipers == 1)
        SAFE_DELETE(m_soundWipers);
    SAFE_DELETE(m_soundBump1);
    SAFE_DELETE(m_effectStart);
    SAFE_DELETE(m_effectCrash);
    SAFE_DELETE(m_effectSpring);
    SAFE_DELETE(m_effectEngine);
    SAFE_DELETE(m_effectCurbLeft);
    SAFE_DELETE(m_effectCurbRight);
    SAFE_DELETE(m_effectGravel);
    SAFE_DELETE(m_effectBumpLeft);
    SAFE_DELETE(m_effectBumpRight);
    SAFE_DELETE(m_soundBadSwitch);
    SAFE_DELETE(m_soundBackfire);
}



void
Car::initialize(Int positionX, Int positionY)
{
    RACE("Car::initialize");
    m_positionX = positionX;
    m_positionY = positionY;
    m_laneWidth = m_track->laneWidth()*2;
    if (m_effectSpring)
        m_effectSpring->play( );
}


void
Car::finalize( )
{
    RACE("Car::finalize");
    m_soundEngine->stop( );
    if (m_soundThrottle)
        m_soundThrottle->stop( );
    if (m_effectSpring)
        m_effectSpring->stop( );
}


void 
Car::start( )
{
    RACE("Car::start");
    pushEvent(Event::carStart, m_soundStart->length()-0.1f);
    m_soundStart->play( );
    m_speed = 0;
    m_prevFrequency = m_idlefreq;
    m_frequency = m_idlefreq;
    m_prevBrakeFrequency = 0;
    m_brakeFrequency = 0;
    m_prevSurfaceFrequency = 0;
    m_surfaceFrequency = 0;
    m_switchingGear = 0;
    m_throttleVolume = 0.0f;
    m_soundAsphalt->frequency(m_surfaceFrequency);
    m_soundGravel->frequency(m_surfaceFrequency);
    m_soundWater->frequency(m_surfaceFrequency);
    m_soundSand->frequency(m_surfaceFrequency);
    m_soundSnow->frequency(m_surfaceFrequency);
    m_state = starting;
    if (m_listener)
        m_listener->onStart( );
    if (m_effectStart)
        m_effectStart->play( );
    // if ((m_effectGravel) && ((m_surface == Track::gravel) || (m_surface == Track::sand)))
        // m_effectGravel->play( );
    if (m_effectEngine)
        m_effectEngine->play( );
}


void
Car::crash( )
{
    // RACE("Car::crash");
    m_speed = 0;
    m_throttleVolume = 0.0f;
    m_soundCrash->play( );
    m_soundEngine->stop( );
    m_soundEngine->reset( );
    m_soundEngine->pan(0);
    if (m_soundThrottle)
    {
        m_soundThrottle->stop( );
        m_soundThrottle->reset( );
        m_soundThrottle->pan(0);
    }
    m_soundStart->pan(0);
    switch (m_surface)
    {
        case Track::asphalt:
            m_soundAsphalt->stop( );
            m_soundAsphalt->pan(0);
            m_soundAsphalt->volume(90);
            break;
        case Track::gravel:
            m_soundGravel->stop( );
            m_soundGravel->pan(0);
            m_soundGravel->volume(90);
            break;
        case Track::water:
            m_soundWater->stop( );
            m_soundWater->pan(0);
            m_soundWater->volume(90);
            break;
        case Track::sand:
            m_soundSand->stop( );
            m_soundSand->pan(0);
            m_soundSand->volume(90);
            break;
        case Track::snow:
            m_soundSnow->stop( );
            m_soundSnow->pan(0);
            m_soundSnow->volume(90);
            break;
        default:
            break;
    }
    m_soundBrake->stop( );
    m_soundBrake->reset( );
    m_soundBrake->pan(0);
    if (m_hasWipers == 1)
    {
        m_soundWipers->stop( );
        m_soundWipers->reset( );
        m_soundWipers->pan(0);
    }
    m_soundHorn->stop( );
    m_soundHorn->reset( );
    m_soundHorn->pan(0);
    m_gear = 1;
    m_switchingGear = 0;
    m_state = crashing;
    pushEvent(Event::carRestart, m_soundCrash->length() + 1.25f);
    if (m_listener)
        m_listener->onCrash( );
    // if (m_effectGravel)
        // m_effectGravel->stop( );
    if (m_effectEngine)
        m_effectEngine->stop( );
    if (m_effectCrash)
        m_effectCrash->play( );
    if (m_effectCurbLeft)
        m_effectCurbLeft->stop( );
    if (m_effectCurbRight)
        m_effectCurbRight->stop( );
}


void
Car::miniCrash(Int newPosition)
{
    // RACE("Car::miniCrash");
    m_speed /= 4;
    if ((m_effectBumpLeft) && (m_positionX < newPosition))
    {
        m_effectBumpLeft->play( );
    }
    if ((m_effectBumpRight) && (m_positionX > newPosition))
    {
        m_effectBumpRight->play( );
    }

    m_positionX = newPosition;
    m_throttleVolume = 0.0f;
    m_soundMiniCrash->reset( );
    m_soundMiniCrash->play( );
}

void 
Car::bump(Int bumpX, Int bumpY, Int bumpSpeed)
{
    RACE("Car::bump(%d, %d)", bumpX, bumpY);
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
        if (m_effectBumpLeft)
            m_effectBumpLeft->play( );
    }
    else if (bumpX < 0)
    {
        m_positionX += 2*bumpX;
        m_speed -= m_speed/5;
        if (m_effectBumpRight)
            m_effectBumpRight->play( );
    }
    if (m_speed < 0)
        m_speed = 0;
    m_soundBump1->play( );
}


void
Car::stop( )
{
    RACE("Car::stop");
    m_soundBrake->stop( );
    if (m_hasWipers == 1)
        m_soundWipers->stop( );
    if (m_effectCurbLeft)
        m_effectCurbLeft->stop( );
    if (m_effectCurbRight)
        m_effectCurbRight->stop( );
    m_state = stopping;
}


void
Car::quiet( )
{
    RACE("Car::quiet");
    m_soundBrake->stop( );
    m_soundEngine->volume(90);
    if (m_soundThrottle != 0)
        m_soundThrottle->stop( );
    if (m_soundBackfire != 0)
        m_soundBackfire->volume(90);
    m_soundAsphalt->volume(90);
    m_soundGravel->volume(90);
    m_soundWater->volume(90);
    m_soundSand->volume(90);
    m_soundSnow->volume(90);
    if (m_effectCurbLeft)
        m_effectCurbLeft->stop( );
    if (m_effectCurbRight)
        m_effectCurbRight->stop( );
    if (m_effectEngine)
        m_effectEngine->stop( );
}


void 
Car::run(Float elapsed)
{
    Boolean horning     = m_game->raceInput()->getHorn( );

    if ((m_state == running) && (m_game->started( )))
    {
       m_currentSteering    = m_game->raceInput()->getSteering( );
       m_currentThrottle    = m_game->raceInput()->getThrottle( );
       m_currentBrake       = m_game->raceInput()->getBrake( );
        Boolean gearUp      = m_game->raceInput()->getGearUp( );
        Boolean gearDown    = m_game->raceInput()->getGearDown( );
        
        m_currentAcceleration = m_acceleration;
        m_currentDeceleration = m_deceleration;
        m_speedDiff = 0;
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
                m_currentAcceleration = (m_currentAcceleration)/2;
                m_currentDeceleration = (m_currentDeceleration*3)/2;
                break;
            case Track::snow:
                // m_currentAcceleration = m_currentAcceleration;
                m_currentDeceleration = (m_currentDeceleration)/2;
                break;
            default:
                break;
        }
        m_factor1 = 100;
        if (m_manualTransmission)
        {
            static Boolean stickReleased = false;
            if ((!gearUp) && (!gearDown))
                stickReleased = true;
            m_factor1 = calculateAcceleration( );
            if ((gearDown) && (m_gear > 1) && (stickReleased))
            {
                stickReleased = false;
                m_switchingGear = -1;
                --m_gear;
                if (m_soundEngine->frequency( ) > 3 * m_topfreq / 2)
                    m_soundBadSwitch->play( );
                if (m_soundBackfire != 0)
                {
                    if ((!m_soundBackfire->playing()) && (random(5) == 1))
                    {
                        m_soundBackfire->play( );
                    }
                }
                pushEvent(Event::inGear, 0.2f);
            }
            else if ((gearUp) && (m_gear < m_gears) && (stickReleased))
            {
                stickReleased = false;
                m_switchingGear = 1;
                ++m_gear;
                if (m_soundEngine->frequency( ) < m_idlefreq)
                    m_soundBadSwitch->play( );
                if (m_soundBackfire != 0)
                {
                    if ((!m_soundBackfire->playing()) && (random(5) == 1))
                    {
                        m_soundBackfire->play( );
                    }
                }
                pushEvent(Event::inGear, 0.2f);
            }
        }
        if (m_soundThrottle)
        {
            if (m_soundEngine->playing( ))
            {
                if (m_currentThrottle > 50)
                {
                    if (!m_soundThrottle->playing( ))
                    {
                        m_soundThrottle->volume((Int)m_throttleVolume);
                        m_soundThrottle->play(0, true);
                    }
                    else 
                    {
                        if (m_throttleVolume >= 80.0f)
                            m_throttleVolume += (100.0f - m_throttleVolume) * elapsed;
                        else
                            m_throttleVolume = 80.0f;
                        if (m_throttleVolume > 100.0f)
                            m_throttleVolume = 100.0f;
                        if ((Int)m_throttleVolume != (Int)m_prevThrottleVolume)
                        {
                            m_soundThrottle->volume((Int)m_throttleVolume);
                            m_prevThrottleVolume = m_throttleVolume;
                        }
                    }
                }
                else
                {
                    m_throttleVolume -= 10.0f * elapsed;
                    if (m_throttleVolume < Float(m_speed * 95 / m_topspeed))
                        m_throttleVolume = Float(m_speed * 95 / m_topspeed);
                    if ((Int)m_throttleVolume != (Int)m_prevThrottleVolume)
                    {
                        m_soundThrottle->volume((Int)m_throttleVolume);
                        m_prevThrottleVolume = m_throttleVolume;
                    }
                }
            }
            else if (m_soundThrottle->playing( ))
                m_soundThrottle->stop( );
        }
        m_thrust   = m_currentThrottle;
        if (m_currentThrottle == 0)
            m_thrust = m_currentBrake;
        else if (m_currentBrake == 0)
            m_thrust = m_currentThrottle;
        else if (-m_currentBrake > m_currentThrottle)
            m_thrust = m_currentBrake;

		m_factor2 = 1.0;
		if ((m_currentSteering != 0) && (m_speed > m_topspeed/2))
		{
			m_factor2 = 1.0 - (1.5*m_speed/m_topspeed)*absval<int>(m_currentSteering)/100;			
		}

        if (m_thrust > 10)
        {
            m_speedDiff = Int((elapsed*m_thrust*m_currentAcceleration*m_factor1*m_factor2)/100);
            if (m_backfirePlayed == true)
                m_backfirePlayed = false;
        }
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

		if (m_thrust <= 0)
		{
                if (m_soundBackfire != 0)
                {
                    if ((!m_soundBackfire->playing()) && (m_backfirePlayed == false))
                    {
                        if (random(5) == 1)
                            m_soundBackfire->play( );
                    }
                    m_backfirePlayed = true;
                }
            }

            if ((m_thrust < -50) && (m_speed > 0))
            {  
                brakeSound();

                if (m_effectSpring)
                    m_effectSpring->gain(5000*m_speed/m_topspeed);
                m_currentSteering = m_currentSteering*2/3;
            }
        else if ((m_currentSteering != 0) && (m_speed > m_topspeed/2))
		{
			if (m_thrust > -50)
			{
				brakeCurveSound( );
			}
		}
		else
        {
            if (m_soundBrake->playing( ))
				m_soundBrake->stop( );
			m_soundAsphalt->volume(90);
                  m_soundGravel->volume(90);
                  m_soundWater->volume(90);
                  m_soundSand->volume(90);
                  m_soundSnow->volume(90);
        }

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
            if (m_speed <= 5000)
                m_soundBrake->volume(100 - (50 - (m_speed/100)));
            else
                m_soundBrake->volume(100);
            if (m_manualTransmission)
                updateEngineFreqManual( );
            else
                updateEngineFreq( );
            updateSoundRoad( );
            if (m_effectGravel)
            {
                if (m_surface == Track::gravel)
                    m_effectGravel->gain(m_speed*10000/m_topspeed);
                else
                    m_effectGravel->gain(0);
            }
            if (m_effectSpring)
            {
                if (m_speed == 0)
                    m_effectSpring->gain(10000);
                else
                    m_effectSpring->gain(10000*m_speed/m_topspeed);
            }
            if (m_effectEngine)
            {
                if (m_speed < m_topspeed / 10)
                    m_effectEngine->gain(10000 - m_speed*10/m_topspeed);
                else
                    m_effectEngine->gain(0);
            }
        }
        switch (m_surface)
        {
        case Track::asphalt:
            if (!m_soundAsphalt->playing())
            {
                if (m_surfaceFrequency <= MAXSURFACEFREQ)
                    m_soundAsphalt->frequency(m_surfaceFrequency);
                else
                    m_soundAsphalt->frequency(MAXSURFACEFREQ);
                m_soundAsphalt->play(0, true);
            }
            break;
        case Track::gravel:
            if (!m_soundGravel->playing())
            {
                if (m_surfaceFrequency <= MAXSURFACEFREQ)
                    m_soundGravel->frequency(m_surfaceFrequency);
                else
                    m_soundGravel->frequency(MAXSURFACEFREQ);
                m_soundGravel->play(0, true);
            }
            break;
        case Track::water:
            if (!m_soundWater->playing())
            {
                if (m_surfaceFrequency <= MAXSURFACEFREQ)
                    m_soundWater->frequency(m_surfaceFrequency);
                else
                    m_soundWater->frequency(MAXSURFACEFREQ);
                m_soundWater->play(0, true);
            }
            break;
        case Track::sand:
            if (!m_soundSand->playing())
            {
                m_soundSand->frequency(Int(m_surfaceFrequency / 2.5f));
                m_soundSand->play(0, true);
            }
            break;
        case Track::snow:
            if (!m_soundSnow->playing())
            {
                if (m_surfaceFrequency <= MAXSURFACEFREQ)
                    m_soundSnow->frequency(m_surfaceFrequency);
                else
                    m_soundSnow->frequency(MAXSURFACEFREQ);
                m_soundSnow->play(0, true);
            }
            break;
        default:
            break;
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
            updateSoundRoad( );
        }
    }

    if ((horning) && (m_state != stopped) && (m_state != crashing))
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
            switch (e->type)
            {
		case Event::carStart:
                m_soundEngine->frequency(m_idlefreq);
                if (m_soundThrottle)
                    m_soundThrottle->frequency(m_idlefreq);
                if (m_effectStart)
                    m_effectStart->stop( );
                m_soundEngine->play(0, true);
                if (m_hasWipers == 1)
                    m_soundWipers->play(0, true);
                m_state = running;
                break;
		case Event::carRestart:
                if (m_effectCrash)
                    m_effectCrash->stop( );
                start( );
                break;
		case Event::inGear:
                m_switchingGear = 0;
                break;
            default:
                break;
            }
            m_eventList.purge(e);
            delete e;
            e = 0;
        }
    }
}

void 
Car::brakeSound( )
{
    switch (m_surface)
    {
        case Track::asphalt:
            if (!m_soundBrake->playing( ))
            {
                m_soundAsphalt->volume(90);
                // m_soundBrake->pan(0);
                m_soundBrake->play(0, true);
            }
            break;
        case Track::gravel:
            if (m_soundBrake->playing( ))
                m_soundBrake->stop( );
            if (m_speed <= 5000)
                m_soundGravel->volume(100 - (10 - (m_speed/500)));
            else
                m_soundGravel->volume(100);
            break;
        case Track::water:
            if (m_soundBrake->playing( ))
                m_soundBrake->stop( );
            if (m_speed <= 5000)
                m_soundWater->volume(100 - (10 - (m_speed/500)));
            else
                m_soundWater->volume(100);
            break;
        case Track::sand:
            if (m_soundBrake->playing( ))
                m_soundBrake->stop( );
            if (m_speed <= 5000)
                m_soundSand->volume(100 - (10 - (m_speed/500)));
            else
                m_soundSand->volume(100);
            break;
        case Track::snow:
            if (m_soundBrake->playing( ))
                m_soundBrake->stop( );
            if (m_speed <= 5000)
                m_soundSnow->volume(100 - (10 - (m_speed/500)));
            else
                m_soundSnow->volume(100);
            break;
        default:
            break;
    }
}

void
Car::brakeCurveSound( )
{
    switch (m_surface)
    {
        case Track::asphalt:
            if (m_soundBrake->playing( ))
                m_soundBrake->stop( );
            m_soundAsphalt->volume(92*absval<int>(m_currentSteering)/100);
            break;
        case Track::gravel:
            if (m_soundBrake->playing( ))
                m_soundBrake->stop( );
            m_soundGravel->volume(92*absval<int>(m_currentSteering)/100);
            break;
        case Track::water:
            if (m_soundBrake->playing( ))
                m_soundBrake->stop( );
            m_soundWater->volume(92*absval<int>(m_currentSteering)/100);
            break;
        case Track::sand:
            if (m_soundBrake->playing( ))
                m_soundBrake->stop( );
            m_soundSand->volume(92*absval<int>(m_currentSteering)/100);
            break;
        case Track::snow:
            if (m_soundBrake->playing( ))
                m_soundBrake->stop( );
            m_soundSnow->volume(92*absval<int>(m_currentSteering)/100);
            break;
        default:
            break;
    }
}

void 
Car::evaluate(Track::Road road)
{
    if (m_state == stopped)
    {
        if (m_frame % 4 == 0)
        {
            m_relPos = (m_positionX - road.left) / Float(m_laneWidth);
            if (m_relPos - 0.5f < 0)
            {
                m_panPos = Int((m_relPos - 0.5f)*(m_relPos - 0.5f)*(-100));
                m_soundStart->pan(m_panPos);
                m_soundEngine->pan(m_panPos);
                m_soundHorn->pan(m_panPos);
                if (m_hasWipers == 1)
                    m_soundWipers->pan(m_panPos);
            }
            else
            {
                m_panPos = Int((m_relPos - 0.5f)*(m_relPos - 0.5f)*100);
                m_soundStart->pan(m_panPos);
                m_soundEngine->pan(m_panPos);
                m_soundHorn->pan(m_panPos);
                if (m_hasWipers == 1)
                    m_soundWipers->pan(m_panPos);
            }
        }
    }
    if ((m_state == running) && (m_game->started( )))
    {
        if (m_frame % 4 == 0)
        {
            if ((m_surface == Track::asphalt) && (road.surface != Track::asphalt))
            {
                m_soundAsphalt->stop( );
//                m_prevSurfaceFrequency = 10;
                switch (road.surface)
                {
                case Track::gravel:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundGravel->frequency(m_surfaceFrequency);
                    else
                        m_soundGravel->frequency(MAXSURFACEFREQ);
                    m_soundGravel->play(0, true);
                    break;
                case Track::water:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundWater->frequency(m_surfaceFrequency);
                    else
                        m_soundWater->frequency(MAXSURFACEFREQ);
                    m_soundWater->play(0, true);
                    break;
                case Track::sand:
                    m_soundSand->frequency(Int(m_surfaceFrequency / 2.5f));
                    m_soundSand->play(0, true);
                    break;
                case Track::snow:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundSnow->frequency(m_surfaceFrequency);
                    else
                        m_soundSnow->frequency(MAXSURFACEFREQ);
                    m_soundSnow->play(0, true);
                    break;
                default:
                    break;
                }
            }
            else if ((m_surface == Track::gravel) && (road.surface != Track::gravel))
            {
                m_soundGravel->stop( );
//                m_prevSurfaceFrequency = 10;
                switch (road.surface)
                {
                case Track::asphalt:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundAsphalt->frequency(m_surfaceFrequency);
                    else
                        m_soundAsphalt->frequency(MAXSURFACEFREQ);
                    m_soundAsphalt->play(0, true);
                    break;
                case Track::water:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundWater->frequency(m_surfaceFrequency);
                    else
                        m_soundWater->frequency(MAXSURFACEFREQ);
                    m_soundWater->play(0, true);
                    break;
                case Track::sand:
                    m_soundSand->frequency(Int(m_surfaceFrequency / 2.5f));
                    m_soundSand->play(0, true);
                    break;
                case Track::snow:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundSnow->frequency(m_surfaceFrequency);
                    else
                        m_soundSnow->frequency(MAXSURFACEFREQ);
                    m_soundSnow->play(0, true);
                    break;
                default:
                    break;
                }
            }
            else if ((m_surface == Track::water) && (road.surface != Track::water))
            {
                m_soundWater->stop( );
//                m_prevSurfaceFrequency = 10;
                switch (road.surface)
                {
                case Track::asphalt:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundAsphalt->frequency(m_surfaceFrequency);
                    else
                        m_soundAsphalt->frequency(MAXSURFACEFREQ);
                    m_soundAsphalt->play(0, true);
                    break;
                case Track::gravel:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundGravel->frequency(m_surfaceFrequency);
                    else
                        m_soundGravel->frequency(MAXSURFACEFREQ);
                    m_soundGravel->play(0, true);
                    break;
                case Track::sand:
                    m_soundSand->frequency(Int(m_surfaceFrequency / 2.5f));
                    m_soundSand->play(0, true);
                    break;
                case Track::snow:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundSnow->frequency(m_surfaceFrequency);
                    else
                        m_soundSnow->frequency(MAXSURFACEFREQ);
                    m_soundSnow->play(0, true);
                    break;
                default:
                    break;
                }
            }
            else if ((m_surface == Track::sand) && (road.surface != Track::sand))
            {
                m_soundSand->stop( );
//                m_prevSurfaceFrequency = 10;
                switch (road.surface)
                {
                case Track::asphalt:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundAsphalt->frequency(m_surfaceFrequency);
                    else
                        m_soundAsphalt->frequency(MAXSURFACEFREQ);
                    m_soundAsphalt->play(0, true);
                    break;
                case Track::gravel:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundGravel->frequency(m_surfaceFrequency);
                    else
                        m_soundGravel->frequency(MAXSURFACEFREQ);
                    m_soundGravel->play(0, true);
                    break;
                case Track::water:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundWater->frequency(m_surfaceFrequency);
                    else
                        m_soundWater->frequency(MAXSURFACEFREQ);
                    m_soundWater->play(0, true);
                    break;
                case Track::snow:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundSnow->frequency(m_surfaceFrequency);
                    else
                        m_soundSnow->frequency(MAXSURFACEFREQ);
                    m_soundSnow->play(0, true);
                    break;
                default:
                    break;
                }
            }
            else if ((m_surface == Track::snow) && (road.surface != Track::snow))
            {
                m_soundSnow->stop( );
//                m_prevSurfaceFrequency = 10;
                switch (road.surface)
                {
                case Track::asphalt:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundAsphalt->frequency(m_surfaceFrequency);
                    else
                        m_soundAsphalt->frequency(MAXSURFACEFREQ);
                    m_soundAsphalt->play(0, true);
                    break;
                case Track::gravel:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundGravel->frequency(m_surfaceFrequency);
                    else
                        m_soundGravel->frequency(MAXSURFACEFREQ);
                    m_soundGravel->play(0, true);
                    break;
                case Track::water:
                    if (m_surfaceFrequency <= MAXSURFACEFREQ)
                        m_soundWater->frequency(m_surfaceFrequency);
                    else
                        m_soundWater->frequency(MAXSURFACEFREQ);
                    m_soundWater->play(0, true);
                    break;
                case Track::sand:
                    m_soundSand->frequency(Int(m_surfaceFrequency / 2.5f));
                    m_soundSand->play(0, true);
                    break;
                default:
                    break;
                }
            }
            m_surface = road.surface;

            m_relPos = (m_positionX - road.left) / Float(m_laneWidth);
            if (m_relPos - 0.5f < 0)
            {
                m_panPos = Int((m_relPos - 0.5f)*(m_relPos - 0.5f)*(-100));
                m_soundEngine->pan(m_panPos);
                if (m_soundThrottle)
                    m_soundThrottle->pan(m_panPos);
                m_soundHorn->pan(m_panPos);
                m_soundBrake->pan(m_panPos);
                if (m_soundBackfire != 0)
                    m_soundBackfire->pan(m_panPos);
                if (m_hasWipers == 1)
                    m_soundWipers->pan(m_panPos);
                switch (m_surface)
                {
                    case Track::asphalt:
                        m_soundAsphalt->pan(m_panPos);
                        break;
                    case Track::gravel:
                        m_soundGravel->pan(m_panPos);
                        break;
                    case Track::water:
                        m_soundWater->pan(m_panPos);
                        break;
                    case Track::sand:
                        m_soundSand->pan(m_panPos);
                        break;
                    case Track::snow:
                        m_soundSnow->pan(m_panPos);
                        break;
                    default:
                        break;
                 }
            }
            else
            {
                m_panPos = Int((m_relPos - 0.5f)*(m_relPos - 0.5f)*100);
                m_soundEngine->pan(m_panPos);
                if (m_soundThrottle)
                    m_soundThrottle->pan(m_panPos);
                m_soundHorn->pan(m_panPos);
                m_soundBrake->pan(m_panPos);
                if (m_soundBackfire != 0)
                    m_soundBackfire->pan(m_panPos);
                if (m_hasWipers == 1)
                    m_soundWipers->pan(m_panPos);
                switch (m_surface)
                {
                    case Track::asphalt:
                        m_soundAsphalt->pan(m_panPos);
                        break;
                    case Track::gravel:
                        m_soundGravel->pan(m_panPos);
                        break;
                    case Track::water:
                        m_soundWater->pan(m_panPos);
                        break;
                    case Track::sand:
                        m_soundSand->pan(m_panPos);
                        break;
                    case Track::snow:
                        m_soundSnow->pan(m_panPos);
                        break;
                    default:
                        break;
                }
            }
            if (m_effectCurbLeft)
            {
                if ((m_relPos < 0.05) && (m_speed > m_topspeed / 10))
                {
                    m_effectCurbLeft->play( );
                }
                else
                    m_effectCurbLeft->stop( );
            }
            if (m_effectCurbRight)
            {
                if ((m_relPos > 0.95) && (m_speed > m_topspeed / 10))
                {
                    m_effectCurbRight->play( );
                }
                else
                    m_effectCurbRight->stop( );
            }
            if ((m_relPos < 0) || (m_relPos > 1))
            {
                if (m_speed < m_topspeed/2)
                    miniCrash((road.right + road.left)/2);
                else
                    crash( );
            }
        }
    }
    else if (m_state == crashing)
    {
        // reposition to the center of the road
        m_positionX = (road.right + road.left)/2;
    }
    ++m_frame;
}


void 
Car::pushEvent(Event::Type type, Float time)
{
    Event* e = new Event;
    e->type = type;
    e->time = m_game->currentTime( ) + time;
    m_eventList.push(e);
}


void 
Car::updateEngineFreq( )
{
    Int gearRange = m_topspeed / (m_gears + 1);
    if ((m_speed / gearRange) < 2)
    {
        Float gearSpeed = (Float(m_speed) / (2.0f * Float(gearRange)));
        m_frequency = Int(gearSpeed * (m_topfreq - m_idlefreq)) + m_idlefreq;
    }
    else
    {
        // Int gear = m_speed / gearRange;
        m_gear = m_speed / gearRange;
        if (m_gear > m_gears)
            m_gear = m_gears;
        Float gearSpeed = (Float(m_speed) - Float(m_gear) * Float(gearRange)) / Float(gearRange);
        if (gearSpeed < 0.07f)
        {
            m_frequency = Int(((0.07f - gearSpeed) / 0.07f) * Float(m_topfreq - m_shiftfreq) + m_shiftfreq);
            if (m_soundBackfire != 0)
            {
                if (!m_backfirePlayedAuto)
                {
                    if ((random(5) == 1) && (!m_soundBackfire->playing( )))
                        m_soundBackfire->play( );
                }
                m_backfirePlayedAuto = true;
            }
        }
        else
        {
            m_frequency = Int(gearSpeed * (m_topfreq - m_shiftfreq) + m_shiftfreq);
            if (m_soundBackfire != 0)
            {
                if (m_backfirePlayedAuto)
                    m_backfirePlayedAuto = false;
            }
        }        
    }
    if (m_frequency != m_prevFrequency)
    {
        m_soundEngine->frequency(m_frequency);
        if (m_soundThrottle)
        {
            if ((Int)m_throttleVolume != (Int)m_prevThrottleVolume)
            {
                m_soundThrottle->volume((Int)m_throttleVolume);
                m_prevThrottleVolume = m_throttleVolume;
            }
            m_soundThrottle->frequency(m_frequency);
        }
        m_prevFrequency = m_frequency;
    }
}


void
Car::updateEngineFreqManual( )
{
    Int gearRange = m_topspeed / m_gears;
    // m_frequency = m_shiftfreq;
    if (m_gear == 1)
    {
        if (m_speed < Int((4.0f / 3.0f) * Float(gearRange)))
        {
            m_frequency = m_idlefreq + Int((Float(m_speed) * 3.0f / Float(2 * gearRange)) * Float(m_topfreq - m_idlefreq));
        }
        else
        {
            m_frequency = m_idlefreq + 2 * (m_topfreq - m_idlefreq);
        }
    }
    else
    {
        Float shiftPoint = ((2.0f / 3.0f) + Float(m_gear-1)) * Float(gearRange);
        m_frequency = Int((Float(m_speed) / shiftPoint) * Float(m_topfreq));
        if (m_frequency > 2 * m_topfreq)
            m_frequency = 2 * m_topfreq;
        if (m_frequency < m_idlefreq / 2)
            m_frequency = m_idlefreq / 2;
    }
    if (m_switchingGear != 0)
        m_frequency = (2 * m_prevFrequency + m_frequency) / 3;
    if (m_frequency != m_prevFrequency)
    {
        m_soundEngine->frequency(m_frequency);
        if (m_soundThrottle)
        {
            if ((Int)m_throttleVolume != (Int)m_prevThrottleVolume)
            {
                m_soundThrottle->volume((Int)m_throttleVolume);
                m_prevThrottleVolume = m_throttleVolume;
            }
            m_soundThrottle->frequency(m_frequency);
        }
        m_prevFrequency = m_frequency;
    }
}


Int
Car::calculateAcceleration( )
{
    Int gearSpeed = m_topspeed/m_gears;
    Int gearCenter = Int(Float(gearSpeed) * (Float(m_gear) - 0.82f));
    m_speedDiff = m_speed - gearCenter;
    Float relSpeedDiff = Float(m_speedDiff) / Float(gearSpeed);
    /* if (!m_manualTransmission)
    {
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
    } */
    if (absval<Float>(relSpeedDiff) < 1.9f)
    {
        Int acceleration = Int(100.0f * (0.5f + cosf(relSpeedDiff * (DirectX::Pi) * 0.5f)));
        if (acceleration < 5)
            return 5;
        else 
            return acceleration;
    }
    else
    {
        Int acceleration = Int(100.0f * (0.5f + cosf(0.95f * (DirectX::Pi))));
        if (acceleration < 5)
            return 5;
        else
            return acceleration;
    }
}


void
Car::updateSoundRoad( )
{
    m_surfaceFrequency = m_speed * 5;
    if (m_surfaceFrequency != m_prevSurfaceFrequency)
    {
        switch (m_surface)
        {
        case Track::asphalt:
            if (m_surfaceFrequency <= MAXSURFACEFREQ)
                m_soundAsphalt->frequency(m_surfaceFrequency);
            else
                m_soundAsphalt->frequency(MAXSURFACEFREQ);
            break;
        case Track::gravel:
            if (m_surfaceFrequency <= MAXSURFACEFREQ)
                m_soundGravel->frequency(m_surfaceFrequency);
            else
                m_soundGravel->frequency(MAXSURFACEFREQ);
            break;
        case Track::water:
            if (m_surfaceFrequency <= MAXSURFACEFREQ)
                m_soundWater->frequency(m_surfaceFrequency);
            else
                m_soundWater->frequency(MAXSURFACEFREQ);
            break;
        case Track::sand:
            m_soundSand->frequency(Int(m_surfaceFrequency / 2.5f));
            break;
        case Track::snow:
            if (m_surfaceFrequency <= MAXSURFACEFREQ)
                m_soundSnow->frequency(m_surfaceFrequency);
            else
                m_soundSnow->frequency(MAXSURFACEFREQ);
            break;
        default:
            break;
        }
        m_prevSurfaceFrequency = m_surfaceFrequency;
    }
}

Boolean
Car::backfiring( )
{
    if (m_soundBackfire != 0)
        return m_soundBackfire->playing( );
    else
        return false;
}

void
Car::pause( )
{
    m_soundEngine->stop( );
    if (m_soundThrottle)
        m_soundThrottle->stop( );
    if (m_soundBrake->playing( ))
        m_soundBrake->stop( );
    if (m_soundHorn->playing( ))
        m_soundHorn->stop( );
    if ((m_soundBackfire) && (m_soundBackfire->playing( )))
    {
        m_soundBackfire->stop( );
        m_soundBackfire->reset( );
    }
    if (m_hasWipers)
        m_soundWipers->stop( );
    switch (m_surface)
    {
        case Track::asphalt:
            m_soundAsphalt->stop( );
            break;
        case Track::gravel:
            m_soundGravel->stop( );
            break;
        case Track::water:
            m_soundWater->stop( );
            break;
        case Track::sand:
            m_soundSand->stop( );
            break;
        case Track::snow:
            m_soundSnow->stop( );
            break;
        default:
            break;
    }
}

void
Car::unpause( )
{
    m_soundEngine->play(0, true);
    if (m_soundThrottle)
        m_soundThrottle->play(0, true);
    if (m_hasWipers)
        m_soundWipers->play(0, true);
    switch (m_surface)
    {
        case Track::asphalt:
            m_soundAsphalt->play(0, true);
            break;
        case Track::gravel:
            m_soundGravel->play(0, true);
            break;
        case Track::water:
            m_soundWater->play(0, true);
            break;
        case Track::sand:
            m_soundSand->play(0, true);
            break;
        case Track::snow:
            m_soundSnow->play(0, true);
            break;
        default:
            break;
    }
}
