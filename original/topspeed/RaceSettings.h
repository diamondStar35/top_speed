/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACE_RACESETTINGS_H__
#define __RACE_RACESETTINGS_H__


#include "RaceInput.h"


class RaceSettings
{
public:
    RaceSettings( );
    virtual ~RaceSettings( );

public:
    void read( );
    void write( );
    void restoreDefaults( );

public: /* yeah, I know, just being lazy */
    Char                         language[64];
    JoystickAxisOrButton        joystickLeft;
    JoystickAxisOrButton        joystickRight;
    JoystickAxisOrButton        joystickThrottle;
    JoystickAxisOrButton        joystickBrake;
    JoystickAxisOrButton        joystickGearUp;
    JoystickAxisOrButton        joystickGearDown;
    JoystickAxisOrButton        joystickHorn;
    JoystickAxisOrButton        joystickRequestInfo;
    JoystickAxisOrButton        joystickCurrentGear;
    JoystickAxisOrButton        joystickCurrentLapNr;
    JoystickAxisOrButton        joystickCurrentRacePerc;
    JoystickAxisOrButton        joystickCurrentLapPerc;
    JoystickAxisOrButton        joystickCurrentRaceTime;
    DirectX::Input::State       joystickCenter;

    UByte                       keyLeft;
    UByte                       keyRight;
    UByte                       keyThrottle;
    UByte                       keyBrake;
    UByte                       keyGearUp;
    UByte                       keyGearDown;
    UByte                       keyHorn;
    UByte                       keyRequestInfo;
    UByte                       keyCurrentGear;
    UByte                       keyCurrentLapNr;
    UByte                       keyCurrentRacePerc;
    UByte                       keyCurrentLapPerc;
    UByte                       keyCurrentRaceTime;

    Boolean                     forceFeedback;
    Boolean                     useJoystick;
    Int                         automaticInfo;
    Int                         copilot;
    Int                         curveAnnouncement;
    Int                         nrOfLaps;
    Int                         serverNumber;
    Int                         difficulty;
    Int                         nrOfComputers;
    Int                         threeD;
    Int                         hardwareAcceleration;
    Int                         reverseStereo;
    Int                         randomCustomTracks;
    Int                         randomCustomVehicles;
    Int                         singleRaceCustomVehicles;
};


#endif /* __RACE_RACESETTINGS_H__ */
