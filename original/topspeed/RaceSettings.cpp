/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "RaceSettings.h"
#include "Game.h"
#include "Common\If\Algorithm.h"

#define KeyLeft         0xcb
#define KeyRight        0xcd
#define KeyUp           0xc8
#define KeyDown         0xd0
#define KeySpace        0x39 
#define KeyShiftUp      0x1e
#define KeyShiftDown    0x2c
#define KeyTab          0x0f
#define KeyQ            0x10
#define KeyW            0x11
#define KeyE            0x12
#define KeyR            0x13
#define KeyT            0x14

RaceSettings::RaceSettings( ) :
    joystickLeft(axisXneg),
    joystickRight(axisXpos),
    joystickThrottle(axisYpos),
    joystickBrake(axisYneg),
    joystickGearUp(button2),
    joystickGearDown(button1),
    joystickHorn(button3),
    joystickRequestInfo(button4),
    joystickCurrentGear(button5),
    joystickCurrentLapNr(button6),
    joystickCurrentRacePerc(button7),
    joystickCurrentLapPerc(button8),
    joystickCurrentRaceTime(button9),
    keyLeft(KeyLeft),
    keyRight(KeyRight),
    keyThrottle(KeyUp),
    keyBrake(KeyDown),
    keyGearUp(KeyShiftUp),
    keyGearDown(KeyShiftDown),
    keyHorn(KeySpace),
    keyRequestInfo(KeyTab),
    keyCurrentGear(KeyQ),
    keyCurrentLapNr(KeyW),
    keyCurrentRacePerc(KeyE),
    keyCurrentLapPerc(KeyR),
    keyCurrentRaceTime(KeyT),
    forceFeedback(0),
    useJoystick(0),
    automaticInfo(2),
    copilot(2),
    curveAnnouncement(1),
    nrOfLaps(3),
    nrOfComputers(3),
    difficulty(0),
    threeD(1),
    hardwareAcceleration(1),
    reverseStereo(0),
    randomCustomTracks(0),
    randomCustomVehicles(0),
    singleRaceCustomVehicles(0),
    serverNumber(random(4999) + 1000)
{
    RACE("(+) RaceSettings");
    strcpy(language, "en");
    memset(&joystickCenter, 0, sizeof(joystickCenter));
    read( );
}
  

RaceSettings::~RaceSettings( )
{
    RACE("(-) RaceSettings");
}



void 
RaceSettings::read( )
{
    RACE("RaceSettings : reading settings...");
    File settingsFile("TopSpeed.bin", File::read);
    if (settingsFile.opened( ) == false)
    {
        RACE("RaceSettings : failed to open 'TopSpeed.bin', creating new...");
        write( );
    }
    else
    {
        settingsFile.readString("lang", language, 64, "en");
        joystickLeft        = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickRight       = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickThrottle    = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickBrake       = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickGearUp      = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickGearDown    = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickHorn        = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickRequestInfo = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickCurrentGear = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickCurrentLapNr = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickCurrentRacePerc = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickCurrentLapPerc = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickCurrentRaceTime = (JoystickAxisOrButton) settingsFile.readInt( );
        joystickCenter.x    = settingsFile.readInt( );
        joystickCenter.y    = settingsFile.readInt( );
        joystickCenter.z    = settingsFile.readInt( );
        joystickCenter.rx   = settingsFile.readInt( );
        joystickCenter.ry   = settingsFile.readInt( );
        joystickCenter.rz   = settingsFile.readInt( );
        joystickCenter.slider1  = settingsFile.readInt( );
        joystickCenter.slider2  = settingsFile.readInt( );
        forceFeedback   = settingsFile.readInt( );
        useJoystick     = settingsFile.readInt( );
        automaticInfo   = settingsFile.readInt( );
        keyLeft             = (UByte)settingsFile.readInt( );
        keyRight            = (UByte)settingsFile.readInt( );
        keyThrottle         = (UByte)settingsFile.readInt( );
        keyBrake            = (UByte)settingsFile.readInt( );
        keyGearUp           = (UByte)settingsFile.readInt( );
        keyGearDown         = (UByte)settingsFile.readInt( );
        keyHorn             = (UByte)settingsFile.readInt( );
        keyRequestInfo      = (UByte)settingsFile.readInt( );
        keyCurrentGear      = (UByte)settingsFile.readInt( );
        keyCurrentLapNr      = (UByte)settingsFile.readInt( );
        keyCurrentRacePerc      = (UByte)settingsFile.readInt( );
        keyCurrentLapPerc      = (UByte)settingsFile.readInt( );
        keyCurrentRaceTime      = (UByte)settingsFile.readInt( );
        copilot             = settingsFile.readInt( );
        curveAnnouncement   = settingsFile.readInt( );
        nrOfLaps            = settingsFile.readInt( );
        serverNumber        = settingsFile.readInt( );
        nrOfComputers          = settingsFile.readInt( );
        difficulty          = settingsFile.readInt( );
        threeD          = settingsFile.readInt( );
        hardwareAcceleration          = settingsFile.readInt( );
        reverseStereo          = settingsFile.readInt( );
        randomCustomTracks          = settingsFile.readInt( );
        randomCustomVehicles          = settingsFile.readInt( );
        singleRaceCustomVehicles          = settingsFile.readInt( );
    }
}
    

void 
RaceSettings::write( )
{
    RACE("RaceSettings : writing settings...");
    File settingsFile("TopSpeed.bin", File::write | File::create);
settingsFile.writeKeyString("lang", language);
    settingsFile.writeInt((Int) joystickLeft);
    settingsFile.writeInt((Int) joystickRight);
    settingsFile.writeInt((Int) joystickThrottle);
    settingsFile.writeInt((Int) joystickBrake);
    settingsFile.writeInt((Int) joystickGearUp);
    settingsFile.writeInt((Int) joystickGearDown);
    settingsFile.writeInt((Int) joystickHorn);
    settingsFile.writeInt((Int) joystickRequestInfo);
    settingsFile.writeInt((Int) joystickCurrentGear);
    settingsFile.writeInt((Int) joystickCurrentLapNr);
    settingsFile.writeInt((Int) joystickCurrentRacePerc);
    settingsFile.writeInt((Int) joystickCurrentLapPerc);
    settingsFile.writeInt((Int) joystickCurrentRaceTime);
    settingsFile.writeInt((Int) joystickCenter.x);
    settingsFile.writeInt((Int) joystickCenter.y);
    settingsFile.writeInt((Int) joystickCenter.z);
    settingsFile.writeInt((Int) joystickCenter.rx);
    settingsFile.writeInt((Int) joystickCenter.ry);
    settingsFile.writeInt((Int) joystickCenter.rz);
    settingsFile.writeInt((Int) joystickCenter.slider1);
    settingsFile.writeInt((Int) joystickCenter.slider2);
    settingsFile.writeInt((Int) forceFeedback);
    settingsFile.writeInt((Int) useJoystick);
    settingsFile.writeInt((Int) automaticInfo);
    settingsFile.writeInt((Int) keyLeft);
    settingsFile.writeInt((Int) keyRight);
    settingsFile.writeInt((Int) keyThrottle);
    settingsFile.writeInt((Int) keyBrake);
    settingsFile.writeInt((Int) keyGearUp);
    settingsFile.writeInt((Int) keyGearDown);
    settingsFile.writeInt((Int) keyHorn);
    settingsFile.writeInt((Int) keyRequestInfo);
    settingsFile.writeInt((Int) keyCurrentGear);
    settingsFile.writeInt((Int) keyCurrentLapNr);
    settingsFile.writeInt((Int) keyCurrentRacePerc);
    settingsFile.writeInt((Int) keyCurrentLapPerc);
    settingsFile.writeInt((Int) keyCurrentRaceTime);
    settingsFile.writeInt((Int) copilot);
    settingsFile.writeInt((Int) curveAnnouncement);
    settingsFile.writeInt((Int) nrOfLaps);
    settingsFile.writeInt((Int) serverNumber);
    settingsFile.writeInt((Int) nrOfComputers);
    settingsFile.writeInt((Int) difficulty);
    settingsFile.writeInt((Int) threeD);
    settingsFile.writeInt((Int) hardwareAcceleration);
    settingsFile.writeInt((Int) reverseStereo);
    settingsFile.writeInt((Int) randomCustomTracks);
    settingsFile.writeInt((Int) randomCustomVehicles);
    settingsFile.writeInt((Int) singleRaceCustomVehicles);
}


void
RaceSettings::restoreDefaults( )
{
    RACE("RaceSettings : restoring default settings...");
    strcpy(language, "en");
    joystickLeft        = axisXneg;
    joystickRight       = axisXpos;
    joystickThrottle    = axisYpos;
    joystickBrake       = axisYneg;
    joystickGearUp      = button2;
    joystickGearDown    = button1;
    joystickHorn        = button3;
    joystickRequestInfo = button4;
    joystickCurrentGear = button5;
    joystickCurrentLapNr = button6;
    joystickCurrentRacePerc = button7;
    joystickCurrentLapPerc = button8;
    joystickCurrentRaceTime = button9;
    keyLeft             = KeyLeft;
    keyRight            = KeyRight;
    keyThrottle         = KeyUp;
    keyBrake            = KeyDown;
    keyGearUp           = KeyShiftUp;
    keyGearDown         = KeyShiftDown;
    keyHorn             = KeySpace;
    keyRequestInfo      = KeyTab;
    keyCurrentLapNr      = KeyQ;
    keyCurrentLapNr      = KeyW;
    keyCurrentRacePerc      = KeyE;
    keyCurrentLapPerc      = KeyR;
    keyCurrentRaceTime      = KeyT;
    memset(&joystickCenter, 0, sizeof(joystickCenter));
    forceFeedback       = 0;
    useJoystick         = 0;
    automaticInfo       = 2;
    copilot             = 2;
    curveAnnouncement   = 1;
    nrOfLaps            = 3;
    serverNumber        = random(4999) + 1000;
    nrOfComputers          = 3;
    difficulty          = 0;
    threeD          = 1;
    hardwareAcceleration          = 1;
    reverseStereo          = 0;
    randomCustomTracks          = 0;
    randomCustomVehicles          = 0;
    singleRaceCustomVehicles          = 0;
}
