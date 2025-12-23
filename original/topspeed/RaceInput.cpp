/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "RaceInput.h"
#include "Game.h"
#include <Common/If/Algorithm.h>

RaceInput::RaceInput(Game* game) :
    m_game(game),
    m_useJoystick(true)
{
    RACE("(+) RaceInput");
}


RaceInput::~RaceInput( )
{
    RACE("(-) RaceInput");
}



void
RaceInput::initialize( )
{
    RACE("RaceInput::initialize");
    m_left = axisNone;
    m_right = axisNone;
    m_throttle = axisNone;
    m_brake = axisNone;
    m_gearUp = axisNone;
    m_gearDown = axisNone;
    m_horn = axisNone;
    m_requestInfo = axisNone;
    m_currentGear = axisNone;
    m_currentLapNr = axisNone;
    m_currentRacePerc = axisNone;
    m_currentLapPerc = axisNone;
    m_currentRaceTime = axisNone;
    readFromSettings( );
    m_kbPlayer1 = DIK_F1;
    m_kbPlayer2 = DIK_F2;
    m_kbPlayer3 = DIK_F3;
    m_kbPlayer4 = DIK_F4;
    m_kbPlayer5 = DIK_F5;
    m_kbPlayer6 = DIK_F6;
    m_kbPlayer7 = DIK_F7;
    m_kbPlayer8 = DIK_F8;
    m_kbTrackName = DIK_F9;
    m_kbPlayerNumber = DIK_F11;
    m_kbPause = DIK_F12;
    m_kbPlayerPos1 = DIK_1;
    m_kbPlayerPos2 = DIK_2;
    m_kbPlayerPos3 = DIK_3;
    m_kbPlayerPos4 = DIK_4;
    m_kbPlayerPos5 = DIK_5;
    m_kbPlayerPos6 = DIK_6;
    m_kbPlayerPos7 = DIK_7;
    m_kbPlayerPos8 = DIK_8;
    m_kbFlush = DIK_LMENU;
    if (m_game->inputManager( )->joystick( ) != 0)
    {
        m_game->inputManager( )->joystick( )->autocenter(false);
    }
}


void
RaceInput::finalize( )
{
    RACE("RaceInput::finalize");
}


void 
RaceInput::run(DirectX::Input::State& input)
{
    m_lastState = input;
}

Int  
RaceInput::getSteering( )
{
    if (!m_useJoystick)
    {
        if (m_lastState.keys[m_kbLeft])
            return -100;
        else if (m_lastState.keys[m_kbRight])
            return +100;
        else
            return 0;
    }
    else
    {
        Int left = getAxis(m_left);
        Int right = getAxis(m_right);
        if (left)
            return -left;
        else
            return right;
    }
}


Int
RaceInput::getThrottle( )
{
    if (!m_useJoystick)
    {
        if (m_lastState.keys[m_kbThrottle])
            return +100;
        else
            return 0;
    }
    else
    {
        return getAxis(m_throttle);
    }
}


Int
RaceInput::getBrake( )
{
    if (!m_useJoystick)
    {
        if (m_lastState.keys[m_kbBrake])
            return -100;
        else
            return 0;
    }
    else
        return -(getAxis(m_brake));
}


Boolean
RaceInput::getGearUp( )
{
    if (!m_useJoystick)
    {
        return ((Boolean)(m_lastState.keys[m_kbGearUp]));
    }
    else
    {
        return (getAxis(m_gearUp) > 50);
    }
}


Boolean
RaceInput::getGearDown( )
{
    if (!m_useJoystick)
    {
        return ((Boolean)(m_lastState.keys[m_kbGearDown]));
    }
    else
    {
        return (getAxis(m_gearDown) > 50);
    }
}


Boolean
RaceInput::getHorn( )
{
    if (!m_useJoystick)
    {
        return ((Boolean)(m_lastState.keys[m_kbHorn]));
    }
    else
    {
        return (getAxis(m_horn) > 50);
    }
}

Boolean
RaceInput::getRequestInfo( )
{
    if (!m_useJoystick)
    {
        return ((Boolean)(m_lastState.keys[m_kbRequestInfo]));
    }
    else
    {
        return (getAxis(m_requestInfo) > 50);
    }
}

Boolean
RaceInput::getCurrentGear( )
{
    if (!m_useJoystick)
    {
        return ((Boolean)(m_lastState.keys[m_kbCurrentGear]));
    }
    else
    {
        return (getAxis(m_currentGear) > 50);
    }
}

Boolean
RaceInput::getCurrentLapNr( )
{
    if (!m_useJoystick)
    {
        return ((Boolean)(m_lastState.keys[m_kbCurrentLapNr]));
    }
    else
    {
        return (getAxis(m_currentLapNr) > 50);
    }
}

Boolean
RaceInput::getCurrentRacePerc( )
{
    if (!m_useJoystick)
    {
        return ((Boolean)(m_lastState.keys[m_kbCurrentRacePerc]));
    }
    else
    {
        return (getAxis(m_currentRacePerc) > 50);
    }
}

Boolean
RaceInput::getCurrentLapPerc( )
{
    if (!m_useJoystick)
    {
        return ((Boolean)(m_lastState.keys[m_kbCurrentLapPerc]));
    }
    else
    {
        return (getAxis(m_currentLapPerc) > 50);
    }
}

Boolean
RaceInput::getCurrentRaceTime( )
{
    if (!m_useJoystick)
    {
        return ((Boolean)(m_lastState.keys[m_kbCurrentRaceTime]));
    }
    else
    {
        return (getAxis(m_currentRaceTime) > 50);
    }
}

Boolean
RaceInput::getPlayerInfo(UInt& player)
{
    if (m_lastState.keys[m_kbPlayer1])
    {
        player = 0;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayer2])
    {
        player = 1;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayer3])
    {
        player = 2;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayer4])
    {
        player = 3;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayer5])
    {
        player = 4;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayer6])
    {
        player = 5;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayer7])
    {
        player = 6;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayer8])
    {
        player = 7;
        return true;
    }
    return false;
}

Boolean
RaceInput::getPlayerPosition(UInt& player)
{
    if (m_lastState.keys[m_kbPlayerPos1])
    {
        player = 0;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayerPos2])
    {
        player = 1;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayerPos3])
    {
        player = 2;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayerPos4])
    {
        player = 3;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayerPos5])
    {
        player = 4;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayerPos6])
    {
        player = 5;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayerPos7])
    {
        player = 6;
        return true;
    }
    else if (m_lastState.keys[m_kbPlayerPos8])
    {
        player = 7;
        return true;
    }
    return false;
}

Int
RaceInput::getAxis(JoystickAxisOrButton a)
{
    switch (a)
    {
        case axisNone:
            return 0;
        case axisXneg:
            if (m_centerInput.x - m_lastState.x > 0)
                return minimum<Int>(m_centerInput.x - m_lastState.x, 100);
            break;
        case axisXpos:
            if (m_lastState.x - m_centerInput.x > 0)
                return minimum<Int>(m_lastState.x - m_centerInput.x, 100);
            break;
        case axisYneg:
            if (m_centerInput.y - m_lastState.y > 0)
                return minimum<Int>(m_centerInput.y - m_lastState.y, 100);
            break;
        case axisYpos:
            if (m_lastState.y - m_centerInput.y > 0)
                return minimum<Int>(m_lastState.y - m_centerInput.y, 100);
            break;
        case axisZneg:
            if (m_centerInput.z - m_lastState.z > 0)
                return minimum<Int>(m_centerInput.z - m_lastState.z, 100);
            break;
        case axisZpos:
            if (m_lastState.z - m_centerInput.z > 0)
                return minimum<Int>(m_lastState.z - m_centerInput.z, 100);
            break;
        case axisRXneg:
            if (m_centerInput.rx - m_lastState.rx > 0)
                return minimum<Int>(m_centerInput.rx - m_lastState.rx, 100);
            break;
        case axisRXpos:
            if (m_lastState.rx - m_centerInput.rz > 0)
                return minimum<Int>(m_lastState.rx - m_centerInput.rx, 100);
            break;
        case axisRYneg:
            if (m_centerInput.ry - m_lastState.ry > 0)
                return minimum<Int>(m_centerInput.ry - m_lastState.ry, 100);
            break;
        case axisRYpos:
            if (m_lastState.ry - m_centerInput.ry > 0)
                return minimum<Int>(m_lastState.ry - m_centerInput.ry, 100);
            break;
        case axisRZneg:
            if (m_centerInput.rz - m_lastState.rz > 0)
                return minimum<Int>(m_centerInput.rz - m_lastState.rz, 100);
            break;
        case axisRZpos:
            if (m_lastState.rz - m_centerInput.rz > 0)
                return minimum<Int>(m_lastState.rz - m_centerInput.rz, 100);
            break;
        case axisSlider1neg:
            if (m_centerInput.slider1 - m_lastState.slider1 > 0)
                return minimum<Int>(m_centerInput.slider1 - m_lastState.slider1, 100);
            break;
        case axisSlider1pos:
            if (m_lastState.slider1 - m_centerInput.slider1 > 0)
                return minimum<Int>(m_lastState.slider1 - m_centerInput.slider1, 100);
            break;
        case axisSlider2neg:
            if (m_centerInput.slider2 - m_lastState.slider2 > 0)
                return minimum<Int>(m_centerInput.slider2 - m_lastState.slider2, 100);
            break;
        case axisSlider2pos:
            if (m_lastState.slider2 - m_centerInput.slider2 > 0)
                return minimum<Int>(m_lastState.slider2 - m_centerInput.slider2, 100);
            break;
        case button1:
            if (m_lastState.b1)
                return 100;
            break;
        case button2:
            if (m_lastState.b2)
                return 100;
            break;
        case button3:
            if (m_lastState.b3)
                return 100;
            break;
        case button4:
            if (m_lastState.b4)
                return 100;
            break;
        case button5:
            if (m_lastState.b5)
                return 100;
            break;
        case button6:
            if (m_lastState.b6)
                return 100;
            break;
        case button7:
            if (m_lastState.b7)
                return 100;
            break;
        case button8:
            if (m_lastState.b8)
                return 100;
            break;
        case button9:
            if (m_lastState.b9)
                return 100;
            break;
        case button10:
            if (m_lastState.b10)
                return 100;
            break;
        case button11:
            if (m_lastState.b11)
                return 100;
            break;
        case button12:
            if (m_lastState.b12)
                return 100;
            break;
        case button13:
            if (m_lastState.b13)
                return 100;
            break;
        case button14:
            if (m_lastState.b14)
                return 100;
            break;
        case button15:
            if (m_lastState.b15)
                return 100;
            break;
        case button16:
            if (m_lastState.b16)
                return 100;
            break;
        case pov1:
            if (m_lastState.pov1)
                return 100;
            break;
        case pov2:
            if (m_lastState.pov2)
                return 100;
            break;
        case pov3:
            if (m_lastState.pov3)
                return 100;
            break;
        case pov4:
            if (m_lastState.pov4)
                return 100;
            break;
        case pov5:
            if (m_lastState.pov5)
                return 100;
            break;
        case pov6:
            if (m_lastState.pov6)
                return 100;
            break;
        case pov7:
            if (m_lastState.pov7)
                return 100;
            break;
        case pov8:
            if (m_lastState.pov8)
                return 100;
            break;
        default:
            break;
    }
    return 0;
}

void 
RaceInput::setLeft(JoystickAxisOrButton a)
{ 
    m_left = a;           
    m_game->raceSettings().joystickLeft = a;
}


void 
RaceInput::setLeft(UByte key)
{
    m_kbLeft = key;
    m_game->raceSettings().keyLeft = key;
}


void 
RaceInput::setRight(JoystickAxisOrButton a)
{ 
    m_right = a;
    m_game->raceSettings().joystickRight = a;
}


void
RaceInput::setRight(UByte key)
{
    m_kbRight = key;
    m_game->raceSettings().keyRight = key;
}


void 
RaceInput::setThrottle(JoystickAxisOrButton a)
{ 
    m_throttle = a;       
    m_game->raceSettings().joystickThrottle = a;
}


void
RaceInput::setThrottle(UByte key)
{
    m_kbThrottle = key;
    m_game->raceSettings().keyThrottle = key;
}


void 
RaceInput::setBrake(JoystickAxisOrButton a)
{ 
    m_brake = a;
    m_game->raceSettings().joystickBrake = a;
}


void
RaceInput::setBrake(UByte key)
{
    m_kbBrake = key;
    m_game->raceSettings().keyBrake = key;
}


void 
RaceInput::setGearUp(JoystickAxisOrButton a)
{ 
    m_gearUp = a;
    m_game->raceSettings().joystickGearUp = a;
}


void
RaceInput::setGearUp(UByte key)
{
    m_kbGearUp = key;
    m_game->raceSettings().keyGearUp = key;
}


void
RaceInput::setGearDown(JoystickAxisOrButton a)
{ 
    m_gearDown = a;
    m_game->raceSettings().joystickGearDown = a;
}


void
RaceInput::setGearDown(UByte key)
{
    m_kbGearDown = key;
    m_game->raceSettings().keyGearDown = key;
}


void 
RaceInput::setHorn(JoystickAxisOrButton a)
{ 
    m_horn = a;
    m_game->raceSettings().joystickHorn = a;
}


void
RaceInput::setHorn(UByte key)
{
    m_kbHorn = key;
    m_game->raceSettings().keyHorn = key;
}


void
RaceInput::setRequestInfo(JoystickAxisOrButton a)
{
    m_requestInfo = a;
    m_game->raceSettings().joystickRequestInfo = a;
}


void
RaceInput::setRequestInfo(UByte key)
{
    m_kbRequestInfo = key;
    m_game->raceSettings().keyRequestInfo = key;
}

void
RaceInput::setCurrentGear(JoystickAxisOrButton a)
{
    m_currentGear = a;
    m_game->raceSettings().joystickCurrentGear = a;
}

void
RaceInput::setCurrentGear(UByte key)
{
    m_kbCurrentGear = key;
    m_game->raceSettings().keyCurrentGear = key;
}

void
RaceInput::setCurrentLapNr(JoystickAxisOrButton a)
{
    m_currentLapNr = a;
    m_game->raceSettings().joystickCurrentLapNr = a;
}

void
RaceInput::setCurrentLapNr(UByte key)
{
    m_kbCurrentLapNr = key;
    m_game->raceSettings().keyCurrentLapNr = key;
}

void
RaceInput::setCurrentRacePerc(JoystickAxisOrButton a)
{
    m_currentRacePerc = a;
    m_game->raceSettings().joystickCurrentRacePerc = a;
}

void
RaceInput::setCurrentRacePerc(UByte key)
{
    m_kbCurrentRacePerc = key;
    m_game->raceSettings().keyCurrentRacePerc = key;
}

void
RaceInput::setCurrentLapPerc(JoystickAxisOrButton a)
{
    m_currentLapPerc = a;
    m_game->raceSettings().joystickCurrentLapPerc = a;
}

void
RaceInput::setCurrentLapPerc(UByte key)
{
    m_kbCurrentLapPerc = key;
    m_game->raceSettings().keyCurrentLapPerc = key;
}

void
RaceInput::setCurrentRaceTime(JoystickAxisOrButton a)
{
    m_currentRaceTime = a;
    m_game->raceSettings().joystickCurrentRaceTime = a;
}


void
RaceInput::setCurrentRaceTime(UByte key)
{
    m_kbCurrentRaceTime = key;
    m_game->raceSettings().keyCurrentRaceTime = key;
}

void 
RaceInput::setCenter(DirectX::Input::State& c)
{ 
    m_centerInput = c;
    m_game->raceSettings().joystickCenter = c;
}


void
RaceInput::setDevice(Boolean useJoystick)
{
    m_useJoystick = useJoystick;
    m_game->raceSettings().useJoystick = useJoystick;
}


void 
RaceInput::readFromSettings( )
{
    RACE("RaceInput::readFromSettings");
    m_left          = m_game->raceSettings().joystickLeft;
    m_right         = m_game->raceSettings().joystickRight;
    m_throttle      = m_game->raceSettings().joystickThrottle;
    m_brake         = m_game->raceSettings().joystickBrake;
    m_gearUp        = m_game->raceSettings().joystickGearUp;
    m_gearDown      = m_game->raceSettings().joystickGearDown;
    m_horn          = m_game->raceSettings().joystickHorn;
    m_requestInfo   = m_game->raceSettings().joystickRequestInfo;
    m_currentGear   = m_game->raceSettings().joystickCurrentGear;
    m_currentLapNr   = m_game->raceSettings().joystickCurrentLapNr;
    m_currentRacePerc   = m_game->raceSettings().joystickCurrentRacePerc;
    m_currentLapPerc   = m_game->raceSettings().joystickCurrentLapPerc;
    m_currentRaceTime   = m_game->raceSettings().joystickCurrentRaceTime;
    m_centerInput   = m_game->raceSettings().joystickCenter;
    m_kbLeft        = m_game->raceSettings().keyLeft;
    m_kbRight       = m_game->raceSettings().keyRight;
    m_kbThrottle    = m_game->raceSettings().keyThrottle;
    m_kbBrake       = m_game->raceSettings().keyBrake;
    m_kbGearUp      = m_game->raceSettings().keyGearUp;
    m_kbGearDown    = m_game->raceSettings().keyGearDown;
    m_kbHorn        = m_game->raceSettings().keyHorn;
    m_kbRequestInfo = m_game->raceSettings().keyRequestInfo;
    m_kbCurrentGear = m_game->raceSettings().keyCurrentGear;
    m_kbCurrentLapNr = m_game->raceSettings().keyCurrentLapNr;
    m_kbCurrentRacePerc = m_game->raceSettings().keyCurrentRacePerc;
    m_kbCurrentLapPerc = m_game->raceSettings().keyCurrentLapPerc;
    m_kbCurrentRaceTime = m_game->raceSettings().keyCurrentRaceTime;
    m_useJoystick   = m_game->raceSettings().useJoystick;
}
