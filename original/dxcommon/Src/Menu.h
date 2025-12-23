/**
* DXCommon library
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#ifndef __RACING_MENU_H__
#define __RACING_MENU_H__

#include "Game.h"
#include "RaceInput.h"

#define NVEHICLES     12
#define NCIRCUITS     17
#define NADVENTURES   7
#define NTRACKS       24
#define MAXCUSTOMTRACKS 256
#define MAXCUSTOMVEHICLES 256
#define KEY_BACKSPACE 0x0e
#define KEY_ESC       0x01
#define KEY_F1        0x3b
#define KEY_SLASH        0x35

class Menu
{
public:
    struct Item
    {
        DirectX::Sound*  sound;
        UInt             action;
        UInt             param;
        Char*             name;
    };
    enum Goto
    {
        none,
        quickStart,
        timeTrial,
        singleRace,
        multiplayer,
        multiHost,
        multiJoin,
        options
    };

public:
    Menu(Game* game);
    virtual ~Menu( );

public:
    void initialize(Goto nextGoto);
    void finalize( );

    void run(Float elapsed);

public:
    void start( );
    void gotoMainMenu();
    void gotoTimeTrial( );
    void gotoTimeTrialCircuitTrack( );
    void gotoTimeTrialAdventureTrack( );
    void gotoTimeTrialCustomTrackTrack( );
    void gotoSingleRace( );
    void gotoSingleRaceCircuitTrack( );
    void gotoSingleRaceAdventureTrack( );
    void gotoSingleRaceCustomTrackTrack( );
    void gotoMultiplayer( );
    void gotoMultiHost( );
    void gotoMultiHostCircuitTrack( );
    void gotoMultiHostAdventureTrack( );
    void gotoMultiHostCustomTrackTrack( );
    void gotoMultiJoinVehicle( );
    void randomTrack( );
    void randomCircuit( );
    void randomAdventure( );
    void randomCustomTrack( );
    void randomVehicle(Boolean randomCustomVehicles);
    void sayHighScores(Char* trackName);
    void sayTime(Int nrOfLaps, Int raceTime);
private:
    void    pushEvent(Event::Type type, Float time, DirectX::Sound* sound = 0);
    void    flushEvents();
    Boolean handleMenuItem(Item& item);
    void    playCurrentMenuItem( );
    void    stopCurrentMenuItem( );

    void initializeLanguageMenu( );
    void initializeTrackMenu( );
    void initializeVehicleMenu( );

    void gotoTimeTrialCircuitVehicle( );
    void gotoSingleRaceCircuitVehicle( );
    void gotoTimeTrialAdventureVehicle( );
    void gotoSingleRaceAdventureVehicle( );
    void gotoTimeTrialCustomTrackVehicle( );
    void gotoSingleRaceCustomTrackVehicle( );
    void gotoTimeTrialCircuitTrans( );
    void gotoSingleRaceCircuitTrans( );
    void gotoTimeTrialAdventureTrans( );
    void gotoSingleRaceAdventureTrans( );
    void gotoTimeTrialCustomTrackTrans( );
    void gotoTimeTrialRandomVehicle( );
    void gotoTimeTrialRandomTrans( );
    void gotoSingleRaceCustomTrackTrans( );
    void gotoSingleRaceRandomVehicle( );
    void gotoSingleRaceRandomTrans( );
    void gotoMultiplayerListServers( );
    void gotoMultiHostCircuitVehicle( );
    void gotoMultiHostCircuitTrans( );
    void gotoMultiHostAdventureVehicle( );
    void gotoMultiHostAdventureTrans( );
    void gotoMultiHostCustomTrackVehicle( );
    void gotoMultiHostCustomTrackTrans( );
    void gotoMultiHostRandomVehicle( );
    void gotoMultiHostRandomTrans( );

    void gotoMultiJoinTrans( );
    void gotoOptions( );
    void gotoOptionsControls( );
    void gotoOptionsGameSettings( );
    void gotoOptionsRaceSettings( );
    void gotoOptionsControlsDevice( );
    void gotoOptionsForceFeedback( );
    void gotoOptionsRandomCustomTracks( );
    void gotoOptionsRandomCustomVehicles( );
    void gotoOptionsCopilot( );
    void gotoOptionsCurves( );
    void gotoOptionsRequestInfo( );
    void gotoOptionsNrOfLaps( );
    void gotoOptionsNrOfComputers( );
    void gotoOptionsLanguage( );
    void gotoOptionsDifficulty( );
//    void gotoOptionsSingleCustomVehicles( );
    void gotoOptionsThreeD( );
    void gotoOptionsHardwareAcceleration( );
    void gotoOptionsReverseStereo( );
    void gotoOptionsRestore( );
    void stopMultiplayer( );
    void calibrateJoystick(DirectX::Input::State& input);
    void calibrateKeyboard(DirectX::Input::State& input);
    void startCalibrating(Boolean joystick);
    void stopCalibrating( );
    void commitCalibrating( );
    Boolean                 axisOrButtonTriggered(JoystickAxisOrButton a, DirectX::Input::State& input);
    JoystickAxisOrButton    foundAxisOrButtonTriggered(DirectX::Input::State& input);
    UByte                   foundKey(DirectX::Input::State& input);
    void                    fadeTo(DirectX::Sound* target);
    void                    fadeOut();
    void                    fadeIn();
private:
    Game*                   m_game;
    DirectX::SoundManager*  m_soundManager;
    EventList               m_eventList;
    Float                   m_elapsedTotal;
    Boolean                 m_b1released;
    Boolean                 m_nextReleased;
    Boolean                 m_prevReleased;
    Boolean                 m_acceptInput;
    Float                   m_sayTimeLength;
    Boolean                 m_calibratingJoystick;
    Boolean                 m_calibratingKeyboard;
    UInt                    m_calibratingStep;
    JoystickAxisOrButton    m_usedAxis[13];
    UByte                   m_usedKeys[13];
    DirectX::Input::State   m_joystickCenter;

    // DirectX::Sound*         m_soundButton;
    DirectX::Sound*         m_soundTheme1;
    DirectX::Sound*         m_soundTheme2;
    DirectX::Sound*         m_soundTheme3;
    DirectX::Sound*         m_activeTheme;
    DirectX::Sound*         m_soundIntro;
    DirectX::Sound*         m_soundBack;
    DirectX::Sound*         m_soundQuickstart;
    DirectX::Sound*         m_soundTimeTrial;
    DirectX::Sound*         m_soundSingleRace;
    DirectX::Sound*         m_soundPickTrackType;
    DirectX::Sound*         m_soundNoHighScores;
    DirectX::Sound*         m_soundLaps;
    DirectX::Sound*         m_soundRaceTrack;
    DirectX::Sound*         m_soundAdventure;
    DirectX::Sound*         m_soundCustomTrack;
    DirectX::Sound*         m_soundPickCircuit;
    DirectX::Sound*         m_soundAmerica;
    DirectX::Sound*         m_soundAustria;
    DirectX::Sound*         m_soundBelgium;
    DirectX::Sound*         m_soundBrazil;
    DirectX::Sound*         m_soundChina;
    DirectX::Sound*         m_soundEngland;
    DirectX::Sound*         m_soundFinland;
    DirectX::Sound*         m_soundFrance;
    DirectX::Sound*         m_soundGermany;
    DirectX::Sound*         m_soundIreland;
    DirectX::Sound*         m_soundItaly;
    DirectX::Sound*         m_soundNetherlands;
    DirectX::Sound*         m_soundPortugal;
    DirectX::Sound*         m_soundRussia;
    DirectX::Sound*         m_soundSpain;
    DirectX::Sound*         m_soundSweden;
    DirectX::Sound*         m_soundSwitserland;
    DirectX::Sound*         m_soundPickVehicle;
    DirectX::Sound*         m_soundVehicles[NVEHICLES];
    DirectX::Sound*         m_soundRandom;
    DirectX::Sound*         m_soundPickAdventure;
    DirectX::Sound*         m_soundHills;
    DirectX::Sound*         m_soundCoast;
    DirectX::Sound*         m_soundCountry;
    DirectX::Sound*         m_soundAirport;
    DirectX::Sound*         m_soundDesert;
    DirectX::Sound*         m_soundRush;
    DirectX::Sound*         m_soundEscape;
    DirectX::Sound*         m_soundChoose;
    DirectX::Sound*         m_soundChangeOption;
    DirectX::Sound*         m_soundConfirm;
    DirectX::Sound*         m_soundSelectTrans;
    DirectX::Sound*         m_soundAutomatic;
    DirectX::Sound*         m_soundManual;
    DirectX::Sound*         m_soundMultiplayer;
    DirectX::Sound*         m_soundMultiJoin;
    DirectX::Sound*         m_soundMultiJoinInternet;
    DirectX::Sound*         m_soundMultiHost;
    DirectX::Sound*         m_soundStartingServer;
    DirectX::Sound*         m_soundSearching;
    DirectX::Sound*         m_soundConnectFailed;
    DirectX::Sound*         m_soundRaceInProgress;
    DirectX::Sound*         m_soundFailedToStart;
    DirectX::Sound*         m_soundServerFound;
    DirectX::Sound*         m_soundServersFound;
    DirectX::Sound*         m_soundServer;
    DirectX::Sound*         m_soundSelectServer;
    DirectX::Sound*         m_soundStopServer;
    DirectX::Sound*         m_soundDisconnectFromServer;
    DirectX::Sound*         m_soundConnected;
    DirectX::Sound*         m_soundYouAre;
    DirectX::Sound*         m_soundPlayer;
    DirectX::Sound*         m_soundServerStarted;
    DirectX::Sound*         m_soundQuit;
    DirectX::Sound*         m_soundMainMenu;
    DirectX::Sound*         m_soundOptions;
    DirectX::Sound*         m_soundLanguage;
    DirectX::Sound*         m_soundRandomCustomTracks;
    DirectX::Sound*         m_soundRandomCustomVehicles;
    DirectX::Sound*         m_soundSaved;
    DirectX::Sound*         m_soundDefaultsRestored;
    DirectX::Sound*         m_soundControls;
    DirectX::Sound*         m_soundSelectDevice;
    DirectX::Sound*         m_soundForceFeedback;
    DirectX::Sound*         m_soundAssignKeyboard;
    DirectX::Sound*         m_soundAssignJoystick;
    DirectX::Sound*         m_soundOff;
    DirectX::Sound*         m_soundLapsOnly;
    DirectX::Sound*         m_soundOn;
    DirectX::Sound*         m_soundYes;
    DirectX::Sound*         m_soundNo;
    DirectX::Sound*         m_soundKeyboard;
    DirectX::Sound*         m_soundJoystick;
    DirectX::Sound*         m_soundGameSettings;
    DirectX::Sound*         m_soundRaceSettings;
    DirectX::Sound*         m_soundCopilot;
    DirectX::Sound*         m_soundAutomaticInfo;
    DirectX::Sound*         m_soundCurvesOnly;
    DirectX::Sound*         m_soundAll;
    DirectX::Sound*         m_soundCurveAnnouncement;
    DirectX::Sound*         m_soundCurveFixed;
    DirectX::Sound*         m_soundCurveSpeed;
    DirectX::Sound*         m_soundNrOfLaps;
    DirectX::Sound*         m_soundAssignControls;
    DirectX::Sound*         m_soundNrOfComputers;
    DirectX::Sound*         m_soundDifficulty;
    DirectX::Sound*         m_soundDifficultyEasy;
    DirectX::Sound*         m_soundDifficultyNormal;
    DirectX::Sound*         m_soundDifficultyHard;
//    DirectX::Sound*         m_soundSingleCustomVehicles;
    DirectX::Sound*         m_soundThreeD;
    DirectX::Sound*         m_soundHardwareAcceleration;
    DirectX::Sound*         m_soundReverseStereo;
    DirectX::Sound*         m_soundRestoreDefaults;
    DirectX::Sound*         m_soundMinute;
    DirectX::Sound*         m_soundMinutes;
    DirectX::Sound*         m_soundSecond;
    DirectX::Sound*         m_soundSeconds;
    DirectX::Sound*         m_soundPoint;
    // calibrating sounds
    DirectX::Sound*         m_soundFollowInstructions;
    DirectX::Sound*         m_soundCenterJoystick;
    DirectX::Sound*         m_soundSteerLeft;
    DirectX::Sound*         m_soundSteerRight;
    DirectX::Sound*         m_soundThrottle;
    DirectX::Sound*         m_soundBrake;
    DirectX::Sound*         m_soundGearUp;
    DirectX::Sound*         m_soundGearDown;
    DirectX::Sound*         m_soundHorn;
    DirectX::Sound*         m_soundRequestInfo;
    DirectX::Sound*         m_soundCurrentGear;
    DirectX::Sound*         m_soundCurrentLapNr;
    DirectX::Sound*         m_soundCurrentRacePerc;
    DirectX::Sound*         m_soundCurrentLapPerc;
    DirectX::Sound*         m_soundCurrentRaceTime;
    // variable sounds
    DirectX::Sound*         m_soundNSessions;

    // miscellaneous sounds
    DirectX::Sound*         m_soundLeft;
    

    Item                    m_mainMenu[6];
    Item                    m_timeTrial[5];
    Item                    m_singleRace[5];
    Item                    m_timeTrialCircuitTrack[19];
    Item*                   m_timeTrialCircuitVehicle;
    Item                    m_timeTrialCircuitTrans[3];
    Item                    m_timeTrialAdventureTrack[9];
    Item*                   m_timeTrialAdventureVehicle;
    Item                    m_timeTrialAdventureTrans[3];
    Item                    m_singleRaceCircuitTrack[19];
    Item*                   m_singleRaceCircuitVehicle;
    Item                    m_singleRaceCircuitTrans[3];
    Item                    m_singleRaceAdventureTrack[9];
    Item*                   m_singleRaceAdventureVehicle;
    Item                    m_singleRaceAdventureTrans[3];
    Item*                   m_timeTrialCustomTrackVehicle;
    Item                    m_timeTrialCustomTrackTrans[3];
    Item*                   m_timeTrialRandomVehicle;
    Item                    m_timeTrialRandomTrans[3];
    Item*                   m_singleRaceCustomTrackVehicle;
    Item                    m_singleRaceCustomTrackTrans[3];
    Item*                   m_singleRaceRandomVehicle;
    Item                    m_singleRaceRandomTrans[3];
    Item                    m_multiplayer[4];
    Item                    m_multiHost[5];
    Item                    m_multiHostCircuitTrack[19];
    Item                    m_multiHostAdventureTrack[9];
    Item*                   m_multiHostCustomTrackTrack;
    Item                    m_multiHostCircuitVehicle[NVEHICLES+2];
    Item                    m_multiHostAdventureVehicle[NVEHICLES+2];
    Item                    m_multiHostCustomTrackVehicle[NVEHICLES+2];
    Item                    m_multiHostCircuitTrans[3];
    Item                    m_multiHostAdventureTrans[3];
    Item                    m_multiHostCustomTrackTrans[3];
    Item                    m_multiHostRandomVehicle[NVEHICLES+2];
    Item                    m_multiHostRandomTrans[3];
    Item                    m_multiJoinVehicle[NVEHICLES+2];
    Item                    m_multiJoinTrans[3];
    Item                    m_options[5];
    Item                    m_optionsControls[5];
    Item                    m_optionsForceFeedback[3];
    Item                    m_optionsDevice[3];
    Item                    m_optionsGameSettings[7];
    Item                    m_optionsRaceSettings[7];
    Item                    m_optionsCopilot[4];
    Item                    m_optionsCurves[3];
    Item                    m_optionsRequestInfo[4];
    Item                    m_optionsNrOfLaps[17];
    Item                    m_optionsNrOfComputers[8];
    Item*                   m_optionsLanguage;
    Item                    m_optionsRandomCustomTracks[3];
    Item                    m_optionsRandomCustomVehicles[3];
    Item                    m_optionsDifficulty[4];
//    Item                    m_optionsSingleCustomVehicles[3];
    Item                    m_optionsThreeD[3];
    Item                    m_optionsHardwareAcceleration[3];
    Item                    m_optionsReverseStereo[3];
    Item                    m_optionsRestore[2];
    Item*                   m_currentMenu;
    Item*                   m_listServers;
    Item*                   m_timeTrialCustomTrackTrack;
    Item*                   m_singleRaceCustomTrackTrack;
    UInt                    m_nCustomTracks;
    UInt                    m_nVehicles;
    UInt                    m_currentMenuItem;
    UInt                    m_currentMenuSize;
    Char                    m_languageFiles[64][64];
    Char                    m_customTrackFiles[MAXCUSTOMTRACKS][64];
    Char                    m_vehicleFiles[MAXCUSTOMVEHICLES][64];
    UInt                    m_nSessions;
    Char                    m_nSessionsSound[64];

    static Boolean          g_firstRun;
    DirectX::Sound*         m_soundLogo1;

    UInt                    m_nLanguages;
    Goto                    m_goto;
};


#endif // __RACING_MENU_H__
