/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_RACEINPUT_H__
#define __RACING_RACEINPUT_H__

#include <DxCommon/If/Common.h>

class Game;


enum JoystickAxisOrButton
{
    axisNone,
    axisXneg,
    axisXpos,
    axisYneg,
    axisYpos,
    axisZneg,
    axisZpos,
    axisRXneg,
    axisRXpos,
    axisRYneg,
    axisRYpos,
    axisRZneg,
    axisRZpos,
    axisSlider1neg,
    axisSlider1pos,
    axisSlider2neg,
    axisSlider2pos,
    button1,
    button2,
    button3,
    button4,
    button5,
    button6,
    button7,
    button8,
    button9,
    button10,
    button11,
    button12,
    button13,
    button14,
    button15,
    button16,
    pov1,
    pov2,
    pov3,
    pov4,
    pov5,
    pov6,
    pov7,
    pov8
};


class RaceInput
{
public:
    RaceInput(Game* game);
    ~RaceInput( );

public:
    void initialize( );
    void finalize( );

public:
    void setLeft(JoystickAxisOrButton a);
    void setLeft(UByte key);
    void setRight(JoystickAxisOrButton a);
    void setRight(UByte key);
    void setThrottle(JoystickAxisOrButton a);
    void setThrottle(UByte key);
    void setBrake(JoystickAxisOrButton a);
    void setBrake(UByte key);
    void setGearUp(JoystickAxisOrButton a);
    void setGearUp(UByte key);
    void setGearDown(JoystickAxisOrButton a);
    void setGearDown(UByte key);
    void setHorn(JoystickAxisOrButton a);
    void setHorn(UByte key);
    void setRequestInfo(JoystickAxisOrButton a);
    void setRequestInfo(UByte key);
    void setCurrentGear(JoystickAxisOrButton a);
    void setCurrentGear(UByte key);
    void setCurrentLapNr(JoystickAxisOrButton a);
    void setCurrentLapNr(UByte key);
    void setCurrentRacePerc(JoystickAxisOrButton a);
    void setCurrentRacePerc(UByte key);
    void setCurrentLapPerc(JoystickAxisOrButton a);
    void setCurrentLapPerc(UByte key);
    void setCurrentRaceTime(JoystickAxisOrButton a);
    void setCurrentRaceTime(UByte key);
    void setCenter(DirectX::Input::State& c);
    void setDevice(Boolean useJoystick);
    Int     getSteering( );
    Int     getThrottle( );
    Int     getBrake( );
    Boolean getGearUp( );
    Boolean getGearDown( );
    Boolean getHorn( );
    Boolean getRequestInfo( );
    Boolean getCurrentGear( );
    Boolean getCurrentLapNr( );
    Boolean getCurrentRacePerc( );
    Boolean getCurrentLapPerc( );
    Boolean getCurrentRaceTime( );
    Boolean getPlayerInfo(UInt& player);
    Boolean getTrackName( ) { return ((Boolean)(m_lastState.keys[m_kbTrackName])); }
    Boolean getPlayerNumber( ) { return ((Boolean)(m_lastState.keys[m_kbPlayerNumber])); }
    Boolean getPause( ) { return ((Boolean)(m_lastState.keys[m_kbPause])); }
    Boolean getPlayerPosition(UInt& player);
    Boolean getFlush( ) { return ((Boolean)(m_lastState.keys[m_kbFlush])); }
    DirectX::Input::State& getCenter( )         { return m_centerInput; }
    void readFromSettings( );

public:
    void run(DirectX::Input::State& input);

private:
    Int getAxis(JoystickAxisOrButton a);

private:
    Game*                   m_game;
    JoystickAxisOrButton    m_left;
    JoystickAxisOrButton    m_right;
    JoystickAxisOrButton    m_throttle;
    JoystickAxisOrButton    m_brake;
    JoystickAxisOrButton    m_gearUp;
    JoystickAxisOrButton    m_gearDown;
    JoystickAxisOrButton    m_horn;
    JoystickAxisOrButton    m_requestInfo;
    JoystickAxisOrButton    m_currentGear;
    JoystickAxisOrButton    m_currentLapNr;
    JoystickAxisOrButton    m_currentRacePerc;
    JoystickAxisOrButton    m_currentLapPerc;
    JoystickAxisOrButton    m_currentRaceTime;
    Boolean                 m_useJoystick;
    UByte                   m_kbLeft;
    UByte                   m_kbRight;
    UByte                   m_kbThrottle;
    UByte                   m_kbBrake;
    UByte                   m_kbGearUp;
    UByte                   m_kbGearDown;
    UByte                   m_kbHorn;
    UByte                   m_kbRequestInfo;
    UByte                   m_kbCurrentGear;
    UByte                   m_kbCurrentLapNr;
    UByte                   m_kbCurrentRacePerc;
    UByte                   m_kbCurrentLapPerc;
    UByte                   m_kbCurrentRaceTime;
    UByte                   m_kbPlayer1;
    UByte                   m_kbPlayer2;
    UByte                   m_kbPlayer3;
    UByte                   m_kbPlayer4;
    UByte                   m_kbPlayer5;
    UByte                   m_kbPlayer6;
    UByte                   m_kbPlayer7;
    UByte                   m_kbPlayer8;
    UByte                   m_kbTrackName;
    UByte                   m_kbPlayerNumber;
    UByte                   m_kbPause;
    UByte                   m_kbPlayerPos1;
    UByte                   m_kbPlayerPos2;
    UByte                   m_kbPlayerPos3;
    UByte                   m_kbPlayerPos4;
    UByte                   m_kbPlayerPos5;
    UByte                   m_kbPlayerPos6;
    UByte                   m_kbPlayerPos7;
    UByte                   m_kbPlayerPos8;
    UByte                   m_kbFlush;
    DirectX::Input::State   m_centerInput;
    DirectX::Input::State   m_lastState;
};



#endif /* __RACING_RACEINPUT_H__ */
