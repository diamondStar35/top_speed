/**
* Top Speed 3
* Copyright 2003-2013 Playing in the Dark (http://playinginthedark.net)
* Code contributors: Davy Kager, Davy Loots and Leonard de Ruijter
* This program is distributed under the terms of the GNU General Public License version 3.
*/
#include "Menu.h"
#include "Common/If/Algorithm.h"
#include "RaceInput.h"
#include "RaceClient.h"
#include "RaceServer.h"
#include "resource.h"

Boolean Menu::g_firstRun = true;

enum _menuItemActions
{
    a_back,
    a_quickStart,
    a_timeTrial,
    a_timeTrialCircuit,
    a_timeTrialAdventure,
    a_timeTrialCustomTrack,
    a_timeTrialRandom,
    a_timeCircuitChoose,
    a_timeCircuitChooseRandom,
    a_timeCircuitCustomVehicle,
    a_timeCircuitOfficialVehicle,
    a_timeCircuitRandom,
    a_timeAutomatic,
    a_timeManual,
    a_timeAdventureChoose,
    a_timeAdventureChooseRandom,
    a_timeAdventureCustomVehicle,
    a_timeAdventureOfficialVehicle,
    a_timeAdventureRandom,
    a_timeCustomTrackChoose,
    a_timeCustomTrackChooseRandom,
    a_timeCustomTrackCustomVehicle,
    a_timeCustomTrackOfficialVehicle,
    a_timeCustomTrackRandom,
    a_timeRandomCustomVehicle,
    a_timeRandomOfficialVehicle,
    a_timeRandomRandom,
    a_singleRace,
    a_singleRaceCircuit,
    a_singleRaceAdventure,
    a_singleRaceCustomTrack,
    a_singleRaceRandom,
    a_singleCircuitChoose,
    a_singleCircuitChooseRandom,
    a_singleCircuitCustomVehicle,
    a_singleCircuitOfficialVehicle,
    a_singleCircuitRandom,
    a_singleAutomatic,
    a_singleManual,
    a_singleAdventureChoose,
    a_singleAdventureChooseRandom,
    a_singleAdventureCustomVehicle,
    a_singleAdventureOfficialVehicle,
    a_singleAdventureRandom,
    a_singleCustomTrackChoose,
    a_singleCustomTrackChooseRandom,
    a_singleCustomTrackCustomVehicle,
    a_singleCustomTrackOfficialVehicle,
    a_singleCustomTrackRandom,
    a_singleRandomCustomVehicle,
    a_singleRandomOfficialVehicle,
    a_singleRandomRandom,
    a_multiplayer,
    a_multiJoin,
    a_multiJoinInternet,
    a_multiHost,
    a_multiSelectServer,
    a_multiHostCircuit,
    a_multiHostAdventure,
    a_multiHostCustomTrack,
    a_multiHostRandom,
    a_multiHostOptions,
    a_multiHostOptionsNrOfLaps,
    a_multiHostOptionsNrOfLaps1,
    a_multiHostOptionsNrOfLaps2,
    a_multiHostOptionsNrOfLaps3,
    a_multiHostOptionsNrOfLaps4,
    a_multiHostOptionsNrOfLaps5,
    a_multiHostOptionsNrOfLaps6,
    a_multiHostOptionsNrOfLaps7,
    a_multiHostOptionsNrOfLaps8,
    a_multiHostOptionsNrOfLaps9,
    a_multiHostOptionsNrOfLaps10,
    a_multiHostOptionsNrOfLaps11,
    a_multiHostOptionsNrOfLaps12,
    a_multiHostOptionsNrOfLaps13,
    a_multiHostOptionsNrOfLaps14,
    a_multiHostOptionsNrOfLaps15,
    a_multiHostOptionsNrOfLaps16,
    a_multiHostCircuitChoose,
    a_multiHostCircuitChooseRandom,
    a_multiHostAdventureChoose,
    a_multiHostAdventureChooseRandom,
    a_multiHostCustomTrackChoose,
    a_multiHostCustomTrackChooseRandom,
    a_multiHostStopServer,
    a_multiHostCircuitOfficialVehicle,
    a_multiHostCircuitRandom,
    a_multiHostAutomatic,
    a_multiHostManual,
    a_multiHostAdventureOfficialVehicle,
    a_multiHostAdventureRandom,
    a_multiHostCustomTrackOfficialVehicle,
    a_multiHostCustomTrackRandom,
    a_multiHostRandomOfficialVehicle,
    a_multiHostRandomRandom,
    a_multiJoinOfficialVehicle,
    a_multiJoinRandom,
    a_multiJoinDisconnect,
    a_multiJoinAutomatic,
    a_multiJoinManual,
    a_options,
    a_optionsControls,
    a_optionsControlsDevice,
    a_optionsControlsFF,
    a_optionsControlsAssignKeyboard,
    a_optionsControlsAssignJoystick,
    a_optionsControlsDeviceKeyboard,
    a_optionsControlsDeviceJoystick,
    a_optionsControlsFFOn,
    a_optionsControlsFFOff,
    a_optionsGameSettings,
    a_optionsGameLanguage,
    a_optionsGameRandomCustomTracks,
    a_optionsRandomCustomTracksOn,
    a_optionsRandomCustomTracksOff,
    a_optionsGameRandomCustomVehicles,
    a_optionsRandomCustomVehiclesOn,
    a_optionsRandomCustomVehiclesOff,
    a_optionsGameThreeD,
    a_optionsGameHardwareAcceleration,
    a_optionsGameReverseStereo,
    a_optionsLanguageChoose,
    a_optionsCopilotOff,
    a_optionsCopilotCurves,
    a_optionsCopilotAll,
    a_optionsInfoOff,
    a_optionsInfoLapsOnly,
    a_optionsInfoOn,
    a_optionsNrOfLaps1,
    a_optionsNrOfLaps2,
    a_optionsNrOfLaps3,
    a_optionsNrOfLaps4,
    a_optionsNrOfLaps5,
    a_optionsNrOfLaps6,
    a_optionsNrOfLaps7,
    a_optionsNrOfLaps8,
    a_optionsNrOfLaps9,
    a_optionsNrOfLaps10,
    a_optionsNrOfLaps11,
    a_optionsNrOfLaps12,
    a_optionsNrOfLaps13,
    a_optionsNrOfLaps14,
    a_optionsNrOfLaps15,
    a_optionsNrOfLaps16,
    a_optionsNrOfComputers1,
    a_optionsNrOfComputers2,
    a_optionsNrOfComputers3,
    a_optionsNrOfComputers4,
    a_optionsNrOfComputers5,
    a_optionsNrOfComputers6,
    a_optionsNrOfComputers7,
    a_optionsDifficultyEasy,
    a_optionsDifficultyNormal,
    a_optionsDifficultyHard,
    a_optionsThreeDOff,
    a_optionsThreeDOn,
    a_optionsHardwareAccelerationOff,
    a_optionsHardwareAccelerationOn,
    a_optionsReverseStereoOff,
    a_optionsReverseStereoOn,
    a_optionsRaceSettings,
    a_optionsRaceCopilot,
    a_optionsRaceCurves,
    a_optionsCurveFixed,
    a_optionsCurveSpeed,
    a_optionsRaceInfo,
    a_optionsRaceNrOfLaps,
    a_optionsRaceNrOfComputers,
    a_optionsRaceDifficulty,
//    a_optionsRaceSingleCustomVehicles,
//    a_optionsSingleCustomVehiclesOn,
//    a_optionsSingleCustomVehiclesOff,
    a_optionsRestoreDefaults,
    a_optionsRestoreYes,
    a_quit
};

Menu::Menu(Game* game) : 
    m_listServers(0),
    m_game(game),
    m_soundManager(game->soundManager( )),
    m_nSessions(0),
    m_sayTimeLength(0),
    m_goto(none),
    m_soundNSessions(0)
{
    RACE("(+) Menu");
}


Menu::~Menu( )
{
    RACE("(-) Menu");
}


void
Menu::initialize(Goto nextGoto)
{
    RACE("Menu::initialize");
    m_elapsedTotal = 0;
    m_b1released = false;
    m_nextReleased = false;
    m_prevReleased = false;
    m_acceptInput = false;
    m_multiAcceptingPlayers = true;
    m_calibratingJoystick = false;
    m_calibratingKeyboard = false;
    m_calibratingStep = 0;
    m_goto = nextGoto;

	RACE("Menu::initialize : creating sounds...");
    // m_soundButton               = m_soundManager->create(IDR_BUTTON);
//	RACE("Menu::initialize : loading theme song...");
    m_soundTheme1                = m_game->loadLanguageSound("music\\theme1");
    m_soundTheme2                = m_game->loadLanguageSound("music\\theme2");
    m_soundTheme3                = m_game->loadLanguageSound("music\\theme3");
    m_soundTheme1->volume(0);
    m_soundTheme2->volume(0);
    m_soundTheme3->volume(0);
    switch (m_goto)
    {
        case quickStart:
            m_activeTheme = m_soundTheme1;
            break;
        case timeTrial:
        case singleRace:
        case multiplayer:
        case multiHost:
        case multiJoin:
            m_activeTheme = m_soundTheme3;
            break;
        case multiHostOptions:
        case options:
            m_activeTheme = m_soundTheme2;
            break;
        default:
            m_activeTheme = m_soundTheme1;
            break;
    }
//    RACE("Menu::initialize : loading menu sounds...");    
	m_soundIntro                = m_game->loadLanguageSound("menu\\usearrowkeys");
    m_soundBack                 = m_game->loadLanguageSound("menu\\goback");
    m_soundQuickstart           = m_game->loadLanguageSound("menu\\quickstart");
    m_soundTimeTrial            = m_game->loadLanguageSound("menu\\timetrial");
    m_soundSingleRace           = m_game->loadLanguageSound("menu\\singlerace");
    m_soundPickTrackType          = m_game->loadLanguageSound("menu\\selecttracktype");
    m_soundNoHighScores          = m_game->loadLanguageSound("menu\\noscoresfound");
    m_soundLaps          = m_game->loadLanguageSound("menu\\laps");
    m_soundRaceTrack            = m_game->loadLanguageSound("menu\\racetrack");
    m_soundAdventure            = m_game->loadLanguageSound("menu\\streetadventure");
    m_soundCustomTrack            = m_game->loadLanguageSound("menu\\customtrack");
    m_soundPickCircuit          = m_game->loadLanguageSound("menu\\selectatrack");
    m_soundAmerica              = m_game->loadLanguageSound("tracks\\america");
    m_soundAustria              = m_game->loadLanguageSound("tracks\\austria");
    m_soundBelgium              = m_game->loadLanguageSound("tracks\\belgium");
    m_soundBrazil              = m_game->loadLanguageSound("tracks\\brazil");
    m_soundChina              = m_game->loadLanguageSound("tracks\\china");
    m_soundEngland              = m_game->loadLanguageSound("tracks\\england");
    m_soundFinland              = m_game->loadLanguageSound("tracks\\finland");
    m_soundFrance              = m_game->loadLanguageSound("tracks\\france");
    m_soundGermany              = m_game->loadLanguageSound("tracks\\germany");
    m_soundIreland                = m_game->loadLanguageSound("tracks\\ireland");
    m_soundItaly                = m_game->loadLanguageSound("tracks\\italy");
    m_soundNetherlands          = m_game->loadLanguageSound("tracks\\netherlands");
    m_soundPortugal                = m_game->loadLanguageSound("tracks\\portugal");
    m_soundRussia              = m_game->loadLanguageSound("tracks\\russia");
    m_soundSpain                = m_game->loadLanguageSound("tracks\\spain");
    m_soundSweden                = m_game->loadLanguageSound("tracks\\sweden");
    m_soundSwitserland                = m_game->loadLanguageSound("tracks\\switserland");
////	RACE("Menu::initialize : loading some more menu sounds...");    
    m_soundPickVehicle              = m_game->loadLanguageSound("menu\\selectavehicle");
    Char vehicleSound[19];
    for (UInt i = 0; i < NVEHICLES; ++i)
    {
        sprintf(vehicleSound, "vehicles\\vehicle%d", i+1);
        m_soundVehicles[i] = m_game->loadLanguageSound(vehicleSound);
    }
    m_soundRandom               = m_game->loadLanguageSound("menu\\random");
    m_soundPickAdventure        = m_game->loadLanguageSound("menu\\selectadventure");
    m_soundHills                = m_game->loadLanguageSound("tracks\\rallyhills");
    m_soundCoast                = m_game->loadLanguageSound("tracks\\frenchcoast");
    m_soundCountry              = m_game->loadLanguageSound("tracks\\englishcountry");
    m_soundAirport              = m_game->loadLanguageSound("tracks\\rideairport");
    m_soundDesert              = m_game->loadLanguageSound("tracks\\rallydesert");
    m_soundRush              = m_game->loadLanguageSound("tracks\\rushhour");
    m_soundEscape              = m_game->loadLanguageSound("tracks\\polarescape");
    m_soundChoose               = m_game->loadLanguageSound("menu\\makeaselection");
    m_soundChangeOption               = m_game->loadLanguageSound("menu\\changeoptionto");
    m_soundConfirm               = m_game->loadLanguageSound("menu\\areyousure");
    m_soundSelectTrans          = m_game->loadLanguageSound("menu\\selecttransmission");
    m_soundAutomatic            = m_game->loadLanguageSound("menu\\automatictransmission");
    m_soundManual               = m_game->loadLanguageSound("menu\\manualtransmission");
    m_soundMultiplayer          = m_game->loadLanguageSound("menu\\multiplayergame");
    m_soundMultiJoin            = m_game->loadLanguageSound("menu\\joinagame");
    m_soundMultiJoinInternet    = m_game->loadLanguageSound("menu\\joininternetgame");
    m_soundMultiHost            = m_game->loadLanguageSound("menu\\hostagame");
    m_soundStartingServer       = m_game->loadLanguageSound("menu\\startingserver");
    m_soundSearching            = m_game->loadLanguageSound("menu\\searchingforgames");
    m_soundConnectFailed            = m_game->loadLanguageSound("menu\\couldnotconnect");
    m_soundRaceInProgress            = m_game->loadLanguageSound("menu\\raceinprogress");
    m_soundRaceAborted = m_game->loadLanguageSound("race\\multiplayer\\raceaborted");
    m_soundWaitingForPlayers    = m_game->loadLanguageSound("race\\multiplayer\\waitingforplayers");
    m_soundFailedToStart            = m_game->loadLanguageSound("menu\\failedtostartserver");
//	RACE("Menu::initialize : loading still some more menu sounds...");    
    m_soundServerFound          = m_game->loadLanguageSound("menu\\serverfound");
    m_soundServersFound         = m_game->loadLanguageSound("menu\\serversfound");
    m_soundServer               = m_game->loadLanguageSound("menu\\server");
    m_soundSelectServer         = m_game->loadLanguageSound("menu\\selectaserver");
    m_soundConnected            = m_game->loadLanguageSound("menu\\connectedtoserver");
    m_soundYouAre    = m_game->loadLanguageSound("race\\youare");
    m_soundPlayer    = m_game->loadLanguageSound("race\\player");
m_soundServerStarted            = m_game->loadLanguageSound("menu\\serverstarted");
    m_soundStopServer           = m_game->loadLanguageSound("menu\\stopserver");
    m_soundDisconnectFromServer = m_game->loadLanguageSound("menu\\disconnectfromserver");
    m_soundQuit                 = m_game->loadLanguageSound("menu\\exitgame");
    m_soundMainMenu             = m_game->loadLanguageSound("menu\\mainmenu");
    m_soundOptions              = m_game->loadLanguageSound("menu\\options");
    m_soundLanguage              = m_game->loadLanguageSound("menu\\language");
    m_soundRandomCustomTracks              = m_game->loadLanguageSound("menu\\randomcustomtracks");
    m_soundRandomCustomVehicles              = m_game->loadLanguageSound("menu\\randomcustomvehicles");
    m_soundSaved              = m_game->loadLanguageSound("menu\\saved");
    m_soundDefaultsRestored              = m_game->loadLanguageSound("menu\\defaultsrestored");
    m_soundControls             = m_game->loadLanguageSound("menu\\controls");
    m_soundSelectDevice         = m_game->loadLanguageSound("menu\\selectdevice");
    m_soundForceFeedback        = m_game->loadLanguageSound("menu\\forcefeedback");
    m_soundAssignKeyboard       = m_game->loadLanguageSound("menu\\assignkeyboard");
    m_soundAssignJoystick       = m_game->loadLanguageSound("menu\\assignjoystick");
    m_soundOff                  = m_game->loadLanguageSound("menu\\off");
    m_soundLapsOnly                   = m_game->loadLanguageSound("menu\\lapsonly");
    m_soundOn                   = m_game->loadLanguageSound("menu\\on");
    m_soundYes                   = m_game->loadLanguageSound("menu\\yes");
    m_soundNo                   = m_game->loadLanguageSound("menu\\no");
    m_soundKeyboard             = m_game->loadLanguageSound("menu\\keyboard");
    m_soundJoystick             = m_game->loadLanguageSound("menu\\joystickorwheel");
    m_soundCopilot              = m_game->loadLanguageSound("menu\\copilot");
//	RACE("Menu::initialize : loading menu sounds (they keep coming)...");    
    m_soundAutomaticInfo        = m_game->loadLanguageSound("menu\\automaticinformation");
    m_soundCurvesOnly           = m_game->loadLanguageSound("menu\\curvesonly");
    m_soundAll                  = m_game->loadLanguageSound("menu\\all");
    m_soundCurveAnnouncement    = m_game->loadLanguageSound("menu\\curveannouncement");
    m_soundCurveFixed           = m_game->loadLanguageSound("menu\\fixeddistance");
    m_soundCurveSpeed           = m_game->loadLanguageSound("menu\\speeddependant");
    m_soundNrOfLaps             = m_game->loadLanguageSound("menu\\nroflaps");
    m_soundNrOfComputers           = m_game->loadLanguageSound("menu\\nrofcomputers");
    m_soundDifficulty           = m_game->loadLanguageSound("menu\\difficulty");
    m_soundDifficultyEasy       = m_game->loadLanguageSound("menu\\easy");
    m_soundDifficultyNormal     = m_game->loadLanguageSound("menu\\normal");
    m_soundDifficultyHard       = m_game->loadLanguageSound("menu\\hard");
//    m_soundSingleCustomVehicles              = m_game->loadLanguageSound("menu\\singleracecustomvehicles");
    m_soundThreeD           = m_game->loadLanguageSound("menu\\threed");
    m_soundHardwareAcceleration           = m_game->loadLanguageSound("menu\\hardwareacceleration");
    m_soundReverseStereo           = m_game->loadLanguageSound("menu\\reversestereo");
    m_soundRestoreDefaults      = m_game->loadLanguageSound("menu\\restoredefaults");
    m_soundGameSettings = m_game->loadLanguageSound("menu\\gamesettings");
    m_soundRaceSettings = m_game->loadLanguageSound("menu\\racesettings");

//	RACE("Menu::initialize : setting the volume of the themesong.");    
//    m_soundTheme->volume(90);
    // Initialize misc. sounds
//	RACE("Menu::initialize : loading miscellaneous sounds...");    
    m_soundMinute = m_game->loadLanguageSound("race\\time\\minute");
    m_soundMinutes = m_game->loadLanguageSound("race\\time\\minutes");
    m_soundSecond = m_game->loadLanguageSound("race\\time\\second");
    m_soundSeconds = m_game->loadLanguageSound("race\\time\\seconds");
    m_soundPoint = m_game->loadLanguageSound("race\\time\\point");
	m_soundLeft                 = m_game->loadLanguageSound("race\\copilot\\left1");
    m_soundLeft->pan(-100);

	RACE("Menu::initialize : building the menus.");    
    m_currentMenu     = 0;
    m_currentMenuItem = 0;
    // Initialize main menu
    m_mainMenu[0].sound  = m_soundQuickstart;
    m_mainMenu[0].action = a_quickStart;
    m_mainMenu[1].sound  = m_soundTimeTrial;
    m_mainMenu[1].action = a_timeTrial;
    m_mainMenu[2].sound  = m_soundSingleRace;
    m_mainMenu[2].action = a_singleRace;
    m_mainMenu[3].sound  = m_soundMultiplayer;
    m_mainMenu[3].action = a_multiplayer;
    m_mainMenu[4].sound  = m_soundOptions;
    m_mainMenu[4].action = a_options;
    m_mainMenu[5].sound  = m_soundQuit;
    m_mainMenu[5].action = a_quit;

    // Initialize time trial
    m_timeTrial[0].sound  = m_soundRaceTrack;
    m_timeTrial[0].action = a_timeTrialCircuit;
    m_timeTrial[1].sound  = m_soundAdventure;
    m_timeTrial[1].action = a_timeTrialAdventure;
    m_timeTrial[2].sound  = m_soundCustomTrack;
    m_timeTrial[2].action = a_timeTrialCustomTrack;
    m_timeTrial[3].sound  = m_soundRandom;
    m_timeTrial[3].action = a_timeTrialRandom;
    m_timeTrial[4].sound      = m_soundBack;
    m_timeTrial[4].action     = a_back;

    // Initialize single race
    m_singleRace[0].sound  = m_soundRaceTrack;
    m_singleRace[0].action = a_singleRaceCircuit;
    m_singleRace[1].sound  = m_soundAdventure;
    m_singleRace[1].action = a_singleRaceAdventure;
    m_singleRace[2].sound  = m_soundCustomTrack;
    m_singleRace[2].action = a_singleRaceCustomTrack;
    m_singleRace[3].sound  = m_soundRandom;
    m_singleRace[3].action = a_singleRaceRandom;
    m_singleRace[4].sound      = m_soundBack;
    m_singleRace[4].action     = a_back;

    // Initialize time trial circuit trackmenu
    m_timeTrialCircuitTrack[0].sound  = m_soundAmerica;
    m_timeTrialCircuitTrack[0].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[0].name = "america";
    m_timeTrialCircuitTrack[1].sound  = m_soundAustria;
    m_timeTrialCircuitTrack[1].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[1].name = "austria";
    m_timeTrialCircuitTrack[2].sound  = m_soundBelgium;
    m_timeTrialCircuitTrack[2].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[2].name = "belgium";
    m_timeTrialCircuitTrack[3].sound  = m_soundBrazil;
    m_timeTrialCircuitTrack[3].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[3].name = "brazil";
    m_timeTrialCircuitTrack[4].sound  = m_soundChina;
    m_timeTrialCircuitTrack[4].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[4].name = "china";
    m_timeTrialCircuitTrack[5].sound  = m_soundEngland;
    m_timeTrialCircuitTrack[5].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[5].name = "england";
    m_timeTrialCircuitTrack[6].sound  = m_soundFinland;
    m_timeTrialCircuitTrack[6].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[6].name = "finland";
    m_timeTrialCircuitTrack[7].sound  = m_soundFrance;
    m_timeTrialCircuitTrack[7].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[7].name = "france";
    m_timeTrialCircuitTrack[8].sound  = m_soundGermany;
    m_timeTrialCircuitTrack[8].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[8].name = "germany";
    m_timeTrialCircuitTrack[9].sound  = m_soundIreland;
    m_timeTrialCircuitTrack[9].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[9].name = "ireland";
    m_timeTrialCircuitTrack[10].sound  = m_soundItaly;
    m_timeTrialCircuitTrack[10].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[10].name = "italy";
    m_timeTrialCircuitTrack[11].sound  = m_soundNetherlands;
    m_timeTrialCircuitTrack[11].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[11].name = "netherlands";
    m_timeTrialCircuitTrack[12].sound  = m_soundPortugal;
    m_timeTrialCircuitTrack[12].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[12].name = "portugal";
    m_timeTrialCircuitTrack[13].sound  = m_soundRussia;
    m_timeTrialCircuitTrack[13].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[13].name = "russia";
    m_timeTrialCircuitTrack[14].sound  = m_soundSpain;
    m_timeTrialCircuitTrack[14].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[14].name = "spain";
    m_timeTrialCircuitTrack[15].sound  = m_soundSweden;
    m_timeTrialCircuitTrack[15].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[15].name = "sweden";
    m_timeTrialCircuitTrack[16].sound  = m_soundSwitserland;
    m_timeTrialCircuitTrack[16].action = a_timeCircuitChoose;
    m_timeTrialCircuitTrack[16].name = "switserland";
    m_timeTrialCircuitTrack[17].sound  = m_soundRandom;
    m_timeTrialCircuitTrack[17].action = a_timeCircuitChooseRandom;
    m_timeTrialCircuitTrack[18].sound      = m_soundBack;
    m_timeTrialCircuitTrack[18].action     = a_back;
    // Initialize time trial transmission menu
    m_timeTrialCircuitTrans[0].sound      = m_soundAutomatic;
    m_timeTrialCircuitTrans[0].action     = a_timeAutomatic;
    m_timeTrialCircuitTrans[1].sound      = m_soundManual;
    m_timeTrialCircuitTrans[1].action     = a_timeManual;
    m_timeTrialCircuitTrans[2].sound      = m_soundBack;
    m_timeTrialCircuitTrans[2].action     = a_back;

    // Initialize adventure menu
    m_timeTrialAdventureTrack[0].sound   = m_soundHills;
    m_timeTrialAdventureTrack[0].action  = a_timeAdventureChoose;
    m_timeTrialAdventureTrack[0].name = "advHills";
    m_timeTrialAdventureTrack[1].sound   = m_soundCoast;
    m_timeTrialAdventureTrack[1].action  = a_timeAdventureChoose;
    m_timeTrialAdventureTrack[1].name = "advCoast";
    m_timeTrialAdventureTrack[2].sound   = m_soundCountry;
    m_timeTrialAdventureTrack[2].action  = a_timeAdventureChoose;
    m_timeTrialAdventureTrack[2].name = "advCountry";
    m_timeTrialAdventureTrack[3].sound   = m_soundAirport;
    m_timeTrialAdventureTrack[3].action  = a_timeAdventureChoose;
    m_timeTrialAdventureTrack[3].name = "advAirport";
    m_timeTrialAdventureTrack[4].sound   = m_soundDesert;
    m_timeTrialAdventureTrack[4].action  = a_timeAdventureChoose;
    m_timeTrialAdventureTrack[4].name = "advDesert";
    m_timeTrialAdventureTrack[5].sound   = m_soundRush;
    m_timeTrialAdventureTrack[5].action  = a_timeAdventureChoose;
    m_timeTrialAdventureTrack[5].name = "advRush";
    m_timeTrialAdventureTrack[6].sound   = m_soundEscape;
    m_timeTrialAdventureTrack[6].action  = a_timeAdventureChoose;
    m_timeTrialAdventureTrack[6].name = "advEscape";
    m_timeTrialAdventureTrack[7].sound   = m_soundRandom;
    m_timeTrialAdventureTrack[7].action  = a_timeAdventureChooseRandom;
    m_timeTrialAdventureTrack[8].sound   = m_soundBack;
    m_timeTrialAdventureTrack[8].action  = a_back;

    // Initialize adventure transmission menu
    m_timeTrialAdventureTrans[0].sound  = m_soundAutomatic;
    m_timeTrialAdventureTrans[0].action = a_timeAutomatic;
    m_timeTrialAdventureTrans[1].sound  = m_soundManual;
    m_timeTrialAdventureTrans[1].action = a_timeManual;
    m_timeTrialAdventureTrans[2].sound  = m_soundBack;
    m_timeTrialAdventureTrans[2].action = a_back;

    // Initialize time trial transmission menu
    m_timeTrialCustomTrackTrans[0].sound      = m_soundAutomatic;
    m_timeTrialCustomTrackTrans[0].action     = a_timeAutomatic;
    m_timeTrialCustomTrackTrans[1].sound      = m_soundManual;
    m_timeTrialCustomTrackTrans[1].action     = a_timeManual;
    m_timeTrialCustomTrackTrans[2].sound      = m_soundBack;
    m_timeTrialCustomTrackTrans[2].action     = a_back;

    // Initialize time trial transmission menu
    m_timeTrialRandomTrans[0].sound      = m_soundAutomatic;
    m_timeTrialRandomTrans[0].action     = a_timeAutomatic;
    m_timeTrialRandomTrans[1].sound      = m_soundManual;
    m_timeTrialRandomTrans[1].action     = a_timeManual;
    m_timeTrialRandomTrans[2].sound      = m_soundBack;
    m_timeTrialRandomTrans[2].action     = a_back;

    // Initialize single race transmission menu
    m_singleRaceCircuitTrans[0].sound      = m_soundAutomatic;
    m_singleRaceCircuitTrans[0].action     = a_singleAutomatic;
    m_singleRaceCircuitTrans[1].sound      = m_soundManual;
    m_singleRaceCircuitTrans[1].action     = a_singleManual;
    m_singleRaceCircuitTrans[2].sound      = m_soundBack;
    m_singleRaceCircuitTrans[2].action     = a_back;

    // Initialize single race circuit trackmenu
    m_singleRaceCircuitTrack[0].sound  = m_soundAmerica;
    m_singleRaceCircuitTrack[0].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[0].name = "america";
    m_singleRaceCircuitTrack[1].sound  = m_soundAustria;
    m_singleRaceCircuitTrack[1].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[1].name = "austria";
    m_singleRaceCircuitTrack[2].sound  = m_soundBelgium;
    m_singleRaceCircuitTrack[2].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[2].name = "belgium";
    m_singleRaceCircuitTrack[3].sound  = m_soundBrazil;
    m_singleRaceCircuitTrack[3].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[3].name = "brazil";
    m_singleRaceCircuitTrack[4].sound  = m_soundChina;
    m_singleRaceCircuitTrack[4].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[4].name = "china";
    m_singleRaceCircuitTrack[5].sound  = m_soundEngland;
    m_singleRaceCircuitTrack[5].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[5].name = "england";
    m_singleRaceCircuitTrack[6].sound  = m_soundFinland;
    m_singleRaceCircuitTrack[6].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[6].name = "finland";
    m_singleRaceCircuitTrack[7].sound  = m_soundFrance;
    m_singleRaceCircuitTrack[7].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[7].name = "france";
    m_singleRaceCircuitTrack[8].sound  = m_soundGermany;
    m_singleRaceCircuitTrack[8].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[8].name = "germany";
    m_singleRaceCircuitTrack[9].sound  = m_soundIreland;
    m_singleRaceCircuitTrack[9].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[9].name = "ireland";
    m_singleRaceCircuitTrack[10].sound  = m_soundItaly;
    m_singleRaceCircuitTrack[10].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[10].name = "italy";
    m_singleRaceCircuitTrack[11].sound  = m_soundNetherlands;
    m_singleRaceCircuitTrack[11].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[11].name = "netherlands";
    m_singleRaceCircuitTrack[12].sound  = m_soundPortugal;
    m_singleRaceCircuitTrack[12].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[12].name = "portugal";
    m_singleRaceCircuitTrack[13].sound  = m_soundRussia;
    m_singleRaceCircuitTrack[13].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[13].name = "russia";
    m_singleRaceCircuitTrack[14].sound  = m_soundSpain;
    m_singleRaceCircuitTrack[14].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[14].name = "spain";
    m_singleRaceCircuitTrack[15].sound  = m_soundSweden;
    m_singleRaceCircuitTrack[15].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[15].name = "sweden";
    m_singleRaceCircuitTrack[16].sound  = m_soundSwitserland;
    m_singleRaceCircuitTrack[16].action = a_singleCircuitChoose;
    m_singleRaceCircuitTrack[16].name = "switserland";
    m_singleRaceCircuitTrack[17].sound  = m_soundRandom;
    m_singleRaceCircuitTrack[17].action = a_singleCircuitChooseRandom;
    m_singleRaceCircuitTrack[18].sound      = m_soundBack;
    m_singleRaceCircuitTrack[18].action     = a_back;
    // Initialize adventure menu
    m_singleRaceAdventureTrack[0].sound   = m_soundHills;
    m_singleRaceAdventureTrack[0].action  = a_singleAdventureChoose;
    m_singleRaceAdventureTrack[0].name = "advHills";
    m_singleRaceAdventureTrack[1].sound   = m_soundCoast;
    m_singleRaceAdventureTrack[1].action  = a_singleAdventureChoose;
    m_singleRaceAdventureTrack[1].name = "advCoast";
    m_singleRaceAdventureTrack[2].sound   = m_soundCountry;
    m_singleRaceAdventureTrack[2].action  = a_singleAdventureChoose;
    m_singleRaceAdventureTrack[2].name = "advCountry";
    m_singleRaceAdventureTrack[3].sound   = m_soundAirport;
    m_singleRaceAdventureTrack[3].action  = a_singleAdventureChoose;
    m_singleRaceAdventureTrack[3].name = "advAirport";
    m_singleRaceAdventureTrack[4].sound   = m_soundDesert;
    m_singleRaceAdventureTrack[4].action  = a_singleAdventureChoose;
    m_singleRaceAdventureTrack[4].name = "advDesert";
    m_singleRaceAdventureTrack[5].sound   = m_soundRush;
    m_singleRaceAdventureTrack[5].action  = a_singleAdventureChoose;
    m_singleRaceAdventureTrack[5].name = "advRush";
    m_singleRaceAdventureTrack[6].sound   = m_soundEscape;
    m_singleRaceAdventureTrack[6].action  = a_singleAdventureChoose;
    m_singleRaceAdventureTrack[6].name = "advEscape";
    m_singleRaceAdventureTrack[7].sound   = m_soundRandom;
    m_singleRaceAdventureTrack[7].action  = a_singleAdventureChooseRandom;
    m_singleRaceAdventureTrack[8].sound   = m_soundBack;
    m_singleRaceAdventureTrack[8].action  = a_back;

    // Initialize adventure transmission menu
    m_singleRaceAdventureTrans[0].sound  = m_soundAutomatic;
    m_singleRaceAdventureTrans[0].action = a_singleAutomatic;
    m_singleRaceAdventureTrans[1].sound  = m_soundManual;
    m_singleRaceAdventureTrans[1].action = a_singleManual;
    m_singleRaceAdventureTrans[2].sound  = m_soundBack;
    m_singleRaceAdventureTrans[2].action = a_back;

    // Initialize single race transmission menu
    m_singleRaceCustomTrackTrans[0].sound      = m_soundAutomatic;
    m_singleRaceCustomTrackTrans[0].action     = a_singleAutomatic;
    m_singleRaceCustomTrackTrans[1].sound      = m_soundManual;
    m_singleRaceCustomTrackTrans[1].action     = a_singleManual;
    m_singleRaceCustomTrackTrans[2].sound      = m_soundBack;
    m_singleRaceCustomTrackTrans[2].action     = a_back;

    // Initialize single race transmission menu
    m_singleRaceRandomTrans[0].sound      = m_soundAutomatic;
    m_singleRaceRandomTrans[0].action     = a_singleAutomatic;
    m_singleRaceRandomTrans[1].sound      = m_soundManual;
    m_singleRaceRandomTrans[1].action     = a_singleManual;
    m_singleRaceRandomTrans[2].sound      = m_soundBack;
    m_singleRaceRandomTrans[2].action     = a_back;

    // Initialize multiplayer join/host menu
    m_multiplayer[0].sound  = m_soundMultiJoin;
    m_multiplayer[0].action = a_multiJoin;
    m_multiplayer[1].sound  = m_soundMultiJoinInternet;
    m_multiplayer[1].action = a_multiJoinInternet;
    m_multiplayer[2].sound  = m_soundMultiHost;
    m_multiplayer[2].action = a_multiHost;
    m_multiplayer[3].sound  = m_soundBack;
    m_multiplayer[3].action = a_back;

    // Initialize multiplayer host menu
    m_multiHost[0].sound  = m_soundRaceTrack;
    m_multiHost[0].action = a_multiHostCircuit;
    m_multiHost[1].sound  = m_soundAdventure;
    m_multiHost[1].action = a_multiHostAdventure;
    m_multiHost[2].sound  = m_soundCustomTrack;
    m_multiHost[2].action = a_multiHostCustomTrack;
    m_multiHost[3].sound  = m_soundRandom;
    m_multiHost[3].action = a_multiHostRandom;
    m_multiHost[4].sound      = m_soundOptions;
    m_multiHost[4].action     = a_options;
    m_multiHost[5].sound      = m_soundStopServer;
    m_multiHost[5].action     = a_multiHostStopServer;

    // Initialize multiplayer host track menu
    m_multiHostCircuitTrack[0].sound  = m_soundAmerica;
    m_multiHostCircuitTrack[0].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[0].name = "america";
    m_multiHostCircuitTrack[1].sound  = m_soundAustria;
    m_multiHostCircuitTrack[1].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[1].name = "austria";
    m_multiHostCircuitTrack[2].sound  = m_soundBelgium;
    m_multiHostCircuitTrack[2].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[2].name = "belgium";
    m_multiHostCircuitTrack[3].sound  = m_soundBrazil;
    m_multiHostCircuitTrack[3].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[3].name = "brazil";
    m_multiHostCircuitTrack[4].sound  = m_soundChina;
    m_multiHostCircuitTrack[4].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[4].name = "china";
    m_multiHostCircuitTrack[5].sound  = m_soundEngland;
    m_multiHostCircuitTrack[5].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[5].name = "england";
    m_multiHostCircuitTrack[6].sound  = m_soundFinland;
    m_multiHostCircuitTrack[6].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[6].name = "finland";
    m_multiHostCircuitTrack[7].sound  = m_soundFrance;
    m_multiHostCircuitTrack[7].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[7].name = "france";
    m_multiHostCircuitTrack[8].sound  = m_soundGermany;
    m_multiHostCircuitTrack[8].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[8].name = "germany";
    m_multiHostCircuitTrack[9].sound  = m_soundIreland;
    m_multiHostCircuitTrack[9].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[9].name = "ireland";
    m_multiHostCircuitTrack[10].sound  = m_soundItaly;
    m_multiHostCircuitTrack[10].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[10].name = "italy";
    m_multiHostCircuitTrack[11].sound  = m_soundNetherlands;
    m_multiHostCircuitTrack[11].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[11].name = "netherlands";
    m_multiHostCircuitTrack[12].sound  = m_soundPortugal;
    m_multiHostCircuitTrack[12].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[12].name = "portugal";
    m_multiHostCircuitTrack[13].sound  = m_soundRussia;
    m_multiHostCircuitTrack[13].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[13].name = "russia";
    m_multiHostCircuitTrack[14].sound  = m_soundSpain;
    m_multiHostCircuitTrack[14].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[14].name = "spain";
    m_multiHostCircuitTrack[15].sound  = m_soundSweden;
    m_multiHostCircuitTrack[15].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[15].name = "sweden";
    m_multiHostCircuitTrack[16].sound  = m_soundSwitserland;
    m_multiHostCircuitTrack[16].action = a_multiHostCircuitChoose;
    m_multiHostCircuitTrack[16].name = "switserland";
    m_multiHostCircuitTrack[17].sound  = m_soundRandom;
    m_multiHostCircuitTrack[17].action = a_multiHostCircuitChooseRandom;
    m_multiHostCircuitTrack[18].sound      = m_soundBack;
    m_multiHostCircuitTrack[18].action     = a_back;
    // Initialize multiplayer host circuit vehicle menu
    m_multiHostCircuitVehicle[NVEHICLES].sound     = m_soundRandom;
    m_multiHostCircuitVehicle[NVEHICLES].action    = a_multiHostCircuitRandom;
    m_multiHostCircuitVehicle[NVEHICLES+1].sound     = m_soundBack;
    m_multiHostCircuitVehicle[NVEHICLES+1].action    = a_back;

    // Initialize multiplayer host transmission menu
    m_multiHostCircuitTrans[0].sound       = m_soundAutomatic;
    m_multiHostCircuitTrans[0].action      = a_multiHostAutomatic;
    m_multiHostCircuitTrans[1].sound       = m_soundManual;
    m_multiHostCircuitTrans[1].action      = a_multiHostManual;
    m_multiHostCircuitTrans[2].sound       = m_soundBack;
    m_multiHostCircuitTrans[2].action      = a_back;

    // Initialize multiplayer host Adventure track menu
    m_multiHostAdventureTrack[0].sound  = m_soundHills;
    m_multiHostAdventureTrack[0].action = a_multiHostAdventureChoose;
    m_multiHostAdventureTrack[0].name = "advHills";
    m_multiHostAdventureTrack[1].sound  = m_soundCoast;
    m_multiHostAdventureTrack[1].action = a_multiHostAdventureChoose;
    m_multiHostAdventureTrack[1].name = "advCoast";
    m_multiHostAdventureTrack[2].sound  = m_soundCountry;
    m_multiHostAdventureTrack[2].action = a_multiHostAdventureChoose;
    m_multiHostAdventureTrack[2].name = "advCountry";
    m_multiHostAdventureTrack[3].sound  = m_soundAirport;
    m_multiHostAdventureTrack[3].action = a_multiHostAdventureChoose;
    m_multiHostAdventureTrack[3].name = "advAirport";
    m_multiHostAdventureTrack[4].sound  = m_soundDesert;
    m_multiHostAdventureTrack[4].action = a_multiHostAdventureChoose;
    m_multiHostAdventureTrack[4].name = "advDesert";
    m_multiHostAdventureTrack[5].sound  = m_soundRush;
    m_multiHostAdventureTrack[5].action = a_multiHostAdventureChoose;
    m_multiHostAdventureTrack[5].name = "advRush";
    m_multiHostAdventureTrack[6].sound  = m_soundEscape;
    m_multiHostAdventureTrack[6].action = a_multiHostAdventureChoose;
    m_multiHostAdventureTrack[6].name = "advEscape";
    m_multiHostAdventureTrack[7].sound  = m_soundRandom;
    m_multiHostAdventureTrack[7].action = a_multiHostAdventureChooseRandom;
    m_multiHostAdventureTrack[8].sound      = m_soundBack;
    m_multiHostAdventureTrack[8].action     = a_back;

    // Initialize multiplayer host Adventure vehicle menu
    m_multiHostAdventureVehicle[NVEHICLES].sound     = m_soundRandom;
    m_multiHostAdventureVehicle[NVEHICLES].action    = a_multiHostAdventureRandom;
    m_multiHostAdventureVehicle[NVEHICLES+1].sound     = m_soundBack;
    m_multiHostAdventureVehicle[NVEHICLES+1].action    = a_back;

    // Initialize multiplayer host Adventure transmission menu
    m_multiHostAdventureTrans[0].sound       = m_soundAutomatic;
    m_multiHostAdventureTrans[0].action      = a_multiHostAutomatic;
    m_multiHostAdventureTrans[1].sound       = m_soundManual;
    m_multiHostAdventureTrans[1].action      = a_multiHostManual;
    m_multiHostAdventureTrans[2].sound       = m_soundBack;
    m_multiHostAdventureTrans[2].action      = a_back;

    // Initialize multiplayer host vehicle menu
    m_multiHostCustomTrackVehicle[NVEHICLES].sound     = m_soundRandom;
    m_multiHostCustomTrackVehicle[NVEHICLES].action    = a_multiHostCustomTrackRandom;
    m_multiHostCustomTrackVehicle[NVEHICLES+1].sound     = m_soundBack;
    m_multiHostCustomTrackVehicle[NVEHICLES+1].action    = a_back;

    // Initialize multiplayer host transmission menu
    m_multiHostCustomTrackTrans[0].sound       = m_soundAutomatic;
    m_multiHostCustomTrackTrans[0].action      = a_multiHostAutomatic;
    m_multiHostCustomTrackTrans[1].sound       = m_soundManual;
    m_multiHostCustomTrackTrans[1].action      = a_multiHostManual;
    m_multiHostCustomTrackTrans[2].sound       = m_soundBack;
    m_multiHostCustomTrackTrans[2].action      = a_back;

    // Initialize multiplayer host vehicle menu
    m_multiHostRandomVehicle[NVEHICLES].sound     = m_soundRandom;
    m_multiHostRandomVehicle[NVEHICLES].action    = a_multiHostRandomRandom;
    m_multiHostRandomVehicle[NVEHICLES+1].sound     = m_soundBack;
    m_multiHostRandomVehicle[NVEHICLES+1].action    = a_back;

    // Initialize multiplayer host transmission menu
    m_multiHostRandomTrans[0].sound       = m_soundAutomatic;
    m_multiHostRandomTrans[0].action      = a_multiHostAutomatic;
    m_multiHostRandomTrans[1].sound       = m_soundManual;
    m_multiHostRandomTrans[1].action      = a_multiHostManual;
    m_multiHostRandomTrans[2].sound       = m_soundBack;
    m_multiHostRandomTrans[2].action      = a_back;

    // Initialize the multiHostOptions menu
    m_multiHostOptions[0].sound      = m_soundNrOfLaps;
    m_multiHostOptions[0].action     = a_multiHostOptionsNrOfLaps;
    m_multiHostOptions[1].sound      = m_soundBack;
    m_multiHostOptions[1].action     = a_back;
    // Initialize the multiHostOptions nroflaps menu
    m_multiHostOptionsNrOfLaps[0].sound      = m_game->m_soundNumbers[1];
    m_multiHostOptionsNrOfLaps[0].action     = a_multiHostOptionsNrOfLaps1;
    m_multiHostOptionsNrOfLaps[1].sound      = m_game->m_soundNumbers[2];
    m_multiHostOptionsNrOfLaps[1].action     = a_multiHostOptionsNrOfLaps2;
    m_multiHostOptionsNrOfLaps[2].sound      = m_game->m_soundNumbers[3];
    m_multiHostOptionsNrOfLaps[2].action     = a_multiHostOptionsNrOfLaps3;
    m_multiHostOptionsNrOfLaps[3].sound      = m_game->m_soundNumbers[4];
    m_multiHostOptionsNrOfLaps[3].action     = a_multiHostOptionsNrOfLaps4;
    m_multiHostOptionsNrOfLaps[4].sound      = m_game->m_soundNumbers[5];
    m_multiHostOptionsNrOfLaps[4].action     = a_multiHostOptionsNrOfLaps5;
    m_multiHostOptionsNrOfLaps[5].sound      = m_game->m_soundNumbers[6];
    m_multiHostOptionsNrOfLaps[5].action     = a_multiHostOptionsNrOfLaps6;
    m_multiHostOptionsNrOfLaps[6].sound      = m_game->m_soundNumbers[7];
    m_multiHostOptionsNrOfLaps[6].action     = a_multiHostOptionsNrOfLaps7;
    m_multiHostOptionsNrOfLaps[7].sound      = m_game->m_soundNumbers[8];
    m_multiHostOptionsNrOfLaps[7].action     = a_multiHostOptionsNrOfLaps8;
    m_multiHostOptionsNrOfLaps[8].sound      = m_game->m_soundNumbers[9];
    m_multiHostOptionsNrOfLaps[8].action     = a_multiHostOptionsNrOfLaps9;
    m_multiHostOptionsNrOfLaps[9].sound      = m_game->m_soundNumbers[10];
    m_multiHostOptionsNrOfLaps[9].action     = a_multiHostOptionsNrOfLaps10;
    m_multiHostOptionsNrOfLaps[10].sound      = m_game->m_soundNumbers[11];
    m_multiHostOptionsNrOfLaps[10].action     = a_multiHostOptionsNrOfLaps11;
    m_multiHostOptionsNrOfLaps[11].sound      = m_game->m_soundNumbers[12];
    m_multiHostOptionsNrOfLaps[11].action     = a_multiHostOptionsNrOfLaps12;
    m_multiHostOptionsNrOfLaps[12].sound      = m_game->m_soundNumbers[13];
    m_multiHostOptionsNrOfLaps[12].action     = a_multiHostOptionsNrOfLaps13;
    m_multiHostOptionsNrOfLaps[13].sound      = m_game->m_soundNumbers[14];
    m_multiHostOptionsNrOfLaps[13].action     = a_multiHostOptionsNrOfLaps14;
    m_multiHostOptionsNrOfLaps[14].sound      = m_game->m_soundNumbers[15];
    m_multiHostOptionsNrOfLaps[14].action     = a_multiHostOptionsNrOfLaps15;
    m_multiHostOptionsNrOfLaps[15].sound      = m_game->m_soundNumbers[16];
    m_multiHostOptionsNrOfLaps[15].action     = a_multiHostOptionsNrOfLaps16;
    m_multiHostOptionsNrOfLaps[16].sound      = m_soundBack;
    m_multiHostOptionsNrOfLaps[16].action     = a_back;

    // Initialize multiplayer join vehicle menu
    m_multiJoinVehicle[NVEHICLES].sound     = m_soundRandom;
    m_multiJoinVehicle[NVEHICLES].action    = a_multiJoinRandom;
    m_multiJoinVehicle[NVEHICLES+1].sound     = m_soundOptions;
    m_multiJoinVehicle[NVEHICLES+1].action    = a_options;
    m_multiJoinVehicle[NVEHICLES+2].sound     = m_soundDisconnectFromServer;
    m_multiJoinVehicle[NVEHICLES+2].action    = a_multiJoinDisconnect;

    // Initialize multiplayer join transmission menu
    m_multiJoinTrans[0].sound       = m_soundAutomatic;
    m_multiJoinTrans[0].action      = a_multiJoinAutomatic;
    m_multiJoinTrans[1].sound       = m_soundManual;
    m_multiJoinTrans[1].action      = a_multiJoinManual;
    m_multiJoinTrans[2].sound       = m_soundBack;
    m_multiJoinTrans[2].action      = a_back;

    // Initialize the options menu
    m_options[0].sound              = m_soundGameSettings;
    m_options[0].action             = a_optionsGameSettings;
    m_options[1].sound              = m_soundControls;
    m_options[1].action             = a_optionsControls;
    m_options[2].sound              = m_soundRaceSettings;
    m_options[2].action             = a_optionsRaceSettings;
    m_options[3].sound              = m_soundRestoreDefaults;
    m_options[3].action             = a_optionsRestoreDefaults;
    m_options[4].sound              = m_soundBack;
    m_options[4].action             = a_back;

    // Initialize the options joystick menu
    m_optionsControls[0].sound      = m_soundSelectDevice;
    m_optionsControls[0].action     = a_optionsControlsDevice;
    m_optionsControls[1].sound      = m_soundForceFeedback;
    m_optionsControls[1].action     = a_optionsControlsFF;
    m_optionsControls[2].sound      = m_soundAssignKeyboard;
    m_optionsControls[2].action     = a_optionsControlsAssignKeyboard;
    m_optionsControls[3].sound      = m_soundAssignJoystick;
    m_optionsControls[3].action     = a_optionsControlsAssignJoystick;
    m_optionsControls[4].sound      = m_soundBack;
    m_optionsControls[4].action     = a_back;

    // Initialize the options Force Feedback menu
    m_optionsForceFeedback[0].sound     = m_soundOff;
    m_optionsForceFeedback[0].action    = a_optionsControlsFFOff;
    m_optionsForceFeedback[1].sound     = m_soundOn;
    m_optionsForceFeedback[1].action    = a_optionsControlsFFOn;
    m_optionsForceFeedback[2].sound     = m_soundBack;
    m_optionsForceFeedback[2].action    = a_back;

    // Initialize the options controls device menu
    m_optionsDevice[0].sound        = m_soundKeyboard;
    m_optionsDevice[0].action       = a_optionsControlsDeviceKeyboard;
    m_optionsDevice[1].sound        = m_soundJoystick;
    m_optionsDevice[1].action       = a_optionsControlsDeviceJoystick;
    m_optionsDevice[2].sound        = m_soundBack;
    m_optionsDevice[2].action       = a_back;

    // Initialize the options gamesettings menu
    m_optionsGameSettings[0].sound  = m_soundLanguage;
    m_optionsGameSettings[0].action = a_optionsGameLanguage;
    m_optionsGameSettings[1].sound  = m_soundRandomCustomTracks;
    m_optionsGameSettings[1].action = a_optionsGameRandomCustomTracks;
    m_optionsGameSettings[2].sound  = m_soundRandomCustomVehicles;
    m_optionsGameSettings[2].action = a_optionsGameRandomCustomVehicles;
    m_optionsGameSettings[3].sound  = m_soundThreeD;
    m_optionsGameSettings[3].action = a_optionsGameThreeD;
    m_optionsGameSettings[4].sound  = m_soundHardwareAcceleration;
    m_optionsGameSettings[4].action = a_optionsGameHardwareAcceleration;
    m_optionsGameSettings[5].sound  = m_soundReverseStereo;
    m_optionsGameSettings[5].action = a_optionsGameReverseStereo;
    m_optionsGameSettings[6].sound  = m_soundBack;
    m_optionsGameSettings[6].action = a_back;

    // Initialize the options gamesettings custom tracks in randomization menu
    m_optionsRandomCustomTracks[0].sound   = m_soundOff;
    m_optionsRandomCustomTracks[0].action  = a_optionsRandomCustomTracksOff;
    m_optionsRandomCustomTracks[1].sound   = m_soundOn;
    m_optionsRandomCustomTracks[1].action  = a_optionsRandomCustomTracksOn;
    m_optionsRandomCustomTracks[2].sound   = m_soundBack;
    m_optionsRandomCustomTracks[2].action  = a_back;

    // Initialize the options gamesettings custom vehicles in randomization menu
    m_optionsRandomCustomVehicles[0].sound   = m_soundOff;
    m_optionsRandomCustomVehicles[0].action  = a_optionsRandomCustomVehiclesOff;
    m_optionsRandomCustomVehicles[1].sound   = m_soundOn;
    m_optionsRandomCustomVehicles[1].action  = a_optionsRandomCustomVehiclesOn;
    m_optionsRandomCustomVehicles[2].sound   = m_soundBack;
    m_optionsRandomCustomVehicles[2].action  = a_back;

    // Initialize the options gamesettings copilot menu
    m_optionsCopilot[0].sound       = m_soundOff;
    m_optionsCopilot[0].action      = a_optionsCopilotOff;
    m_optionsCopilot[1].sound       = m_soundCurvesOnly;
    m_optionsCopilot[1].action      = a_optionsCopilotCurves;
    m_optionsCopilot[2].sound       = m_soundAll;
    m_optionsCopilot[2].action      = a_optionsCopilotAll;
    m_optionsCopilot[3].sound       = m_soundBack;
    m_optionsCopilot[3].action      = a_back;

    // Initialize the options gamesettings curve announcement menu
    m_optionsCurves[0].sound        = m_soundCurveFixed;
    m_optionsCurves[0].action       = a_optionsCurveFixed;
    m_optionsCurves[1].sound        = m_soundCurveSpeed;
    m_optionsCurves[1].action       = a_optionsCurveSpeed;
    m_optionsCurves[2].sound        = m_soundBack;
    m_optionsCurves[2].action       = a_back;

    // Initialize the options gamesettings raceinfo menu
    m_optionsRequestInfo[0].sound   = m_soundOff;
    m_optionsRequestInfo[0].action  = a_optionsInfoOff;
    m_optionsRequestInfo[1].sound   = m_soundLapsOnly;
    m_optionsRequestInfo[1].action  = a_optionsInfoLapsOnly;
    m_optionsRequestInfo[2].sound   = m_soundOn;
    m_optionsRequestInfo[2].action  = a_optionsInfoOn;
    m_optionsRequestInfo[3].sound   = m_soundBack;
    m_optionsRequestInfo[3].action  = a_back;

    // Initialize the options gamesettings nroflaps menu
    m_optionsNrOfLaps[0].sound      = m_game->m_soundNumbers[1];
    m_optionsNrOfLaps[0].action     = a_optionsNrOfLaps1;
    m_optionsNrOfLaps[1].sound      = m_game->m_soundNumbers[2];
    m_optionsNrOfLaps[1].action     = a_optionsNrOfLaps2;
    m_optionsNrOfLaps[2].sound      = m_game->m_soundNumbers[3];
    m_optionsNrOfLaps[2].action     = a_optionsNrOfLaps3;
    m_optionsNrOfLaps[3].sound      = m_game->m_soundNumbers[4];
    m_optionsNrOfLaps[3].action     = a_optionsNrOfLaps4;
    m_optionsNrOfLaps[4].sound      = m_game->m_soundNumbers[5];
    m_optionsNrOfLaps[4].action     = a_optionsNrOfLaps5;
    m_optionsNrOfLaps[5].sound      = m_game->m_soundNumbers[6];
    m_optionsNrOfLaps[5].action     = a_optionsNrOfLaps6;
    m_optionsNrOfLaps[6].sound      = m_game->m_soundNumbers[7];
    m_optionsNrOfLaps[6].action     = a_optionsNrOfLaps7;
    m_optionsNrOfLaps[7].sound      = m_game->m_soundNumbers[8];
    m_optionsNrOfLaps[7].action     = a_optionsNrOfLaps8;
    m_optionsNrOfLaps[8].sound      = m_game->m_soundNumbers[9];
    m_optionsNrOfLaps[8].action     = a_optionsNrOfLaps9;
    m_optionsNrOfLaps[9].sound      = m_game->m_soundNumbers[10];
    m_optionsNrOfLaps[9].action     = a_optionsNrOfLaps10;
    m_optionsNrOfLaps[10].sound      = m_game->m_soundNumbers[11];
    m_optionsNrOfLaps[10].action     = a_optionsNrOfLaps11;
    m_optionsNrOfLaps[11].sound      = m_game->m_soundNumbers[12];
    m_optionsNrOfLaps[11].action     = a_optionsNrOfLaps12;
    m_optionsNrOfLaps[12].sound      = m_game->m_soundNumbers[13];
    m_optionsNrOfLaps[12].action     = a_optionsNrOfLaps13;
    m_optionsNrOfLaps[13].sound      = m_game->m_soundNumbers[14];
    m_optionsNrOfLaps[13].action     = a_optionsNrOfLaps14;
    m_optionsNrOfLaps[14].sound      = m_game->m_soundNumbers[15];
    m_optionsNrOfLaps[14].action     = a_optionsNrOfLaps15;
    m_optionsNrOfLaps[15].sound      = m_game->m_soundNumbers[16];
    m_optionsNrOfLaps[15].action     = a_optionsNrOfLaps16;
    m_optionsNrOfLaps[16].sound      = m_soundBack;
    m_optionsNrOfLaps[16].action     = a_back;

    // Initialize the options gamesettings nrOfComputers menu
    m_optionsNrOfComputers[0].sound      = m_game->m_soundNumbers[1];
    m_optionsNrOfComputers[0].action     = a_optionsNrOfComputers1;
    m_optionsNrOfComputers[1].sound      = m_game->m_soundNumbers[2];
    m_optionsNrOfComputers[1].action     = a_optionsNrOfComputers2;
    m_optionsNrOfComputers[2].sound      = m_game->m_soundNumbers[3];
    m_optionsNrOfComputers[2].action     = a_optionsNrOfComputers3;
    m_optionsNrOfComputers[3].sound      = m_game->m_soundNumbers[4];
    m_optionsNrOfComputers[3].action     = a_optionsNrOfComputers4;
    m_optionsNrOfComputers[4].sound      = m_game->m_soundNumbers[5];
    m_optionsNrOfComputers[4].action     = a_optionsNrOfComputers5;
    m_optionsNrOfComputers[5].sound      = m_game->m_soundNumbers[6];
    m_optionsNrOfComputers[5].action     = a_optionsNrOfComputers6;
    m_optionsNrOfComputers[6].sound      = m_game->m_soundNumbers[7];
    m_optionsNrOfComputers[6].action     = a_optionsNrOfComputers7;
    m_optionsNrOfComputers[7].sound      = m_soundBack;
    m_optionsNrOfComputers[7].action     = a_back;

    // Initialize the options gamesettings difficulty menu
    m_optionsDifficulty[0].sound    = m_soundDifficultyEasy;
    m_optionsDifficulty[0].action   = a_optionsDifficultyEasy;
    m_optionsDifficulty[1].sound    = m_soundDifficultyNormal;
    m_optionsDifficulty[1].action   = a_optionsDifficultyNormal;
    m_optionsDifficulty[2].sound    = m_soundDifficultyHard;
    m_optionsDifficulty[2].action   = a_optionsDifficultyHard;
    m_optionsDifficulty[3].sound    = m_soundBack;
    m_optionsDifficulty[3].action   = a_back;
/*
    // Initialize the options racesettings custom vehicles in single race menu
    m_optionsSingleCustomVehicles[0].sound   = m_soundOff;
    m_optionsSingleCustomVehicles[0].action  = a_optionsSingleCustomVehiclesOff;
    m_optionsSingleCustomVehicles[1].sound   = m_soundOn;
    m_optionsSingleCustomVehicles[1].action  = a_optionsSingleCustomVehiclesOn;
    m_optionsSingleCustomVehicles[2].sound   = m_soundBack;
    m_optionsSingleCustomVehicles[2].action  = a_back;
*/
    // Initialize the options gamesettings 3Dmenu
    m_optionsThreeD[0].sound   = m_soundOff;
    m_optionsThreeD[0].action  = a_optionsThreeDOff;
    m_optionsThreeD[1].sound   = m_soundOn;
    m_optionsThreeD[1].action  = a_optionsThreeDOn;
    m_optionsThreeD[2].action  = a_back;
    m_optionsThreeD[2].sound   = m_soundBack;


    // Initialize the options gamesettings hardware acceleration menu
    m_optionsHardwareAcceleration[0].sound   = m_soundOff;
    m_optionsHardwareAcceleration[0].action  = a_optionsHardwareAccelerationOff;
    m_optionsHardwareAcceleration[1].sound   = m_soundOn;
    m_optionsHardwareAcceleration[1].action  = a_optionsHardwareAccelerationOn;
    m_optionsHardwareAcceleration[2].action  = a_back;
    m_optionsHardwareAcceleration[2].sound   = m_soundBack;


    // Initialize the options gamesettings reverse stereo menu
    m_optionsReverseStereo[0].sound   = m_soundOff;
    m_optionsReverseStereo[0].action  = a_optionsReverseStereoOff;
    m_optionsReverseStereo[1].sound   = m_soundOn;
    m_optionsReverseStereo[1].action  = a_optionsReverseStereoOn;
    m_optionsReverseStereo[2].sound   = m_soundBack;
    m_optionsReverseStereo[2].action  = a_back;

    // Initialize the options racesettings menu
    m_optionsRaceSettings[0].sound  = m_soundCopilot;
    m_optionsRaceSettings[0].action = a_optionsRaceCopilot;
    m_optionsRaceSettings[1].sound  = m_soundCurveAnnouncement;
    m_optionsRaceSettings[1].action = a_optionsRaceCurves;
    m_optionsRaceSettings[2].sound  = m_soundAutomaticInfo;
    m_optionsRaceSettings[2].action = a_optionsRaceInfo;
    m_optionsRaceSettings[3].sound  = m_soundNrOfLaps;
    m_optionsRaceSettings[3].action = a_optionsRaceNrOfLaps;
    m_optionsRaceSettings[4].sound  = m_soundNrOfComputers;
    m_optionsRaceSettings[4].action = a_optionsRaceNrOfComputers;
    m_optionsRaceSettings[5].sound  = m_soundDifficulty;
    m_optionsRaceSettings[5].action = a_optionsRaceDifficulty;
//    m_optionsRaceSettings[6].sound  = m_soundSingleCustomVehicles;
//    m_optionsRaceSettings[6].action = a_optionsRaceSingleCustomVehicles;
    m_optionsRaceSettings[6].sound  = m_soundBack;
    m_optionsRaceSettings[6].action = a_back;

    // Initialize the options restore defaults menu
    m_optionsRestore[0].sound   = m_soundYes;
    m_optionsRestore[0].action  = a_optionsRestoreYes;
    m_optionsRestore[1].sound   = m_soundNo;
    m_optionsRestore[1].action  = a_back;

    m_currentMenu     = m_mainMenu;
    m_currentMenuSize = sizeof(m_mainMenu);
    m_listServers     = 0;

//    RACE("Menu::initialize : ready to initialize the language menu.");    
    initializeLanguageMenu( );
//    RACE("Menu::initialize : ready to initialize the track menu.");    
    initializeTrackMenu( );
//    RACE("Menu::initialize : ready to initialize the vehicle menu.");    
    initializeVehicleMenu( );
    for (UInt i = 0; i < NVEHICLES; ++i)
    {
        m_timeTrialCircuitVehicle[i].sound = m_soundVehicles[i];
        m_timeTrialCircuitVehicle[i].action = a_timeCircuitOfficialVehicle;
        m_timeTrialAdventureVehicle[i].sound = m_soundVehicles[i];
        m_timeTrialAdventureVehicle[i].action = a_timeAdventureOfficialVehicle;
        m_timeTrialCustomTrackVehicle[i].sound = m_soundVehicles[i];
        m_timeTrialCustomTrackVehicle[i].action = a_timeCustomTrackOfficialVehicle;
        m_timeTrialRandomVehicle[i].sound = m_soundVehicles[i];
        m_timeTrialRandomVehicle[i].action = a_timeRandomOfficialVehicle;
        m_singleRaceCircuitVehicle[i].sound = m_soundVehicles[i];
        m_singleRaceCircuitVehicle[i].action = a_singleCircuitOfficialVehicle;
        m_singleRaceAdventureVehicle[i].sound = m_soundVehicles[i];
        m_singleRaceAdventureVehicle[i].action = a_singleAdventureOfficialVehicle;
        m_singleRaceCustomTrackVehicle[i].sound = m_soundVehicles[i];
        m_singleRaceCustomTrackVehicle[i].action = a_singleCustomTrackOfficialVehicle;
        m_singleRaceRandomVehicle[i].sound = m_soundVehicles[i];
        m_singleRaceRandomVehicle[i].action = a_singleRandomOfficialVehicle;
        m_multiHostCircuitVehicle[i].sound = m_soundVehicles[i];
        m_multiHostCircuitVehicle[i].action = a_multiHostCircuitOfficialVehicle;
        m_multiHostAdventureVehicle[i].sound = m_soundVehicles[i];
        m_multiHostAdventureVehicle[i].action = a_multiHostAdventureOfficialVehicle;
        m_multiHostCustomTrackVehicle[i].sound = m_soundVehicles[i];
        m_multiHostCustomTrackVehicle[i].action = a_multiHostCustomTrackOfficialVehicle;
        m_multiHostRandomVehicle[i].sound = m_soundVehicles[i];
        m_multiHostRandomVehicle[i].action = a_multiHostRandomOfficialVehicle;
        m_multiJoinVehicle[i].sound = m_soundVehicles[i];
        m_multiJoinVehicle[i].action = a_multiJoinOfficialVehicle;
    }
    if (g_firstRun)
    {
//		RACE("Menu::initialize : creating the logo...");    
        m_soundLogo1 = m_soundManager->create(IDR_PITD_LOGO);
        g_firstRun = false;
    }
    else
        m_soundLogo1 = 0;
	RACE("Menu::initialize : done (at last).");
}

void
Menu::initializeLanguageMenu( )
{
    WIN32_FIND_DATA findFileData;
    HANDLE          findHandle = INVALID_HANDLE_VALUE;
    DWORD           error;
    
    DirectX::Sound* languageSounds[64];
    UInt            nLanguages = 0;

    findHandle = ::FindFirstFile("Sounds\\*.ogg", &findFileData);

    if (findHandle == INVALID_HANDLE_VALUE) 
    {
//        RACE("Menu::initializeLanguageMenu : FindFirstFile failed with error 0x%x", GetLastError());
    } 
    else 
    {
        Char soundFile[64];
        ::sprintf(soundFile, "Sounds\\%s", findFileData.cFileName);
        languageSounds[nLanguages] = m_game->soundManager()->createVorbis(soundFile);
        Int length = ::strlen(findFileData.cFileName);
        ::strncpy(m_languageFiles[nLanguages], findFileData.cFileName, length-4);
        ++nLanguages;
        while ((nLanguages < 64) && (::FindNextFile(findHandle, &findFileData) != 0)) 
        {
            ::sprintf(soundFile, "Sounds\\%s", findFileData.cFileName);
            languageSounds[nLanguages] = m_game->soundManager()->createVorbis(soundFile);
            length = ::strlen(findFileData.cFileName);
            ::strncpy(m_languageFiles[nLanguages], findFileData.cFileName, length-4);
            ++nLanguages;
        }
        error = GetLastError();
        ::FindClose(findHandle);
        if (error != ERROR_NO_MORE_FILES) 
        {
//            RACE("Menu::initializeLanguageMenu : FindNextFile failed with error 0x%x", error);
        }
    }
    RACE("Menu::initializeLanguageMenu : %d languages found", nLanguages);
    m_nLanguages = nLanguages + 1;
    // initialize the menus
    m_optionsLanguage = new Item[m_nLanguages];
    for (UInt i = 0; i < nLanguages; ++i)
    {
        m_optionsLanguage[i].sound  = languageSounds[i];
        m_optionsLanguage[i].action = a_optionsLanguageChoose;
        m_optionsLanguage[i].name  = m_languageFiles[i];
    }
    m_optionsLanguage[m_nLanguages-1].sound  = m_soundBack;
    m_optionsLanguage[m_nLanguages-1].action = a_back;
}

void
Menu::initializeTrackMenu( )
{
    WIN32_FIND_DATA findFileData;
    HANDLE          findHandle = INVALID_HANDLE_VALUE;
    DWORD           error;
    
    DirectX::Sound* trackSounds[MAXCUSTOMTRACKS];
    UInt            nTracks = 0;
    findHandle = ::FindFirstFile("Tracks\\*.trk", &findFileData);
    if (findHandle == INVALID_HANDLE_VALUE) 
    {
//        RACE("Menu::initializeTrackMenu : FindFirstFile failed with error 0x%x", GetLastError());
    } 
    else 
    {
        ::sprintf(m_customTrackFiles[nTracks], "Tracks\\%s", findFileData.cFileName);
        Int length = ::strlen(m_customTrackFiles[nTracks]);
        Char soundFile[64];
        ::strncpy(soundFile, m_customTrackFiles[nTracks], length-4);
        soundFile[length-4] = '\0';
        sprintf(soundFile, "%s.wav", soundFile);
        trackSounds[nTracks] = m_game->soundManager()->create(soundFile);
        if (trackSounds[nTracks] != 0)
        {
            // trackfile and soundfile exist
            // RACE("Menu::initializeTrackMenu : added %s", m_customTrackFiles[nTracks]);
            ++nTracks;
        }
        else
        {
            RACE("Menu::initializeTrackMenu : no soundfile for track %s found!", m_customTrackFiles[nTracks]);
        }
        while ((nTracks < MAXCUSTOMTRACKS) && (::FindNextFile(findHandle, &findFileData) != 0)) 
        {
            ::sprintf(m_customTrackFiles[nTracks], "Tracks\\%s", findFileData.cFileName);
            length = ::strlen(m_customTrackFiles[nTracks]);
            soundFile[64];
            ::strncpy(soundFile, m_customTrackFiles[nTracks], length-4);
            soundFile[length-4] = '\0';
            sprintf(soundFile, "%s.wav", soundFile);
            trackSounds[nTracks] = m_game->soundManager()->create(soundFile);
            if (trackSounds[nTracks] != 0)
            {
                // trackfile and soundfile exist
//                RACE("Menu::initializeTrackMenu : added %s", m_customTrackFiles[nTracks]);
                ++nTracks;
            }
            else
            {
                RACE("Menu::initializeTrackMenu : no soundfile for track %s found!", m_customTrackFiles[nTracks]);
            }
        }
        error = GetLastError();
        ::FindClose(findHandle);
        if (error != ERROR_NO_MORE_FILES) 
        {
//            RACE("Menu::initializeTrackMenu : FindNextFile failed with error 0x%x", error);
        }
    }
    RACE("Menu::initializeTrackMenu : %d user defined tracks found", nTracks);
    m_nCustomTracks = nTracks + 2;
    // initialize the menus
    m_timeTrialCustomTrackTrack = new Item[m_nCustomTracks];
    m_singleRaceCustomTrackTrack = new Item[m_nCustomTracks];
    m_multiHostCustomTrackTrack = new Item[m_nCustomTracks];
    for (UInt i = 0; i < nTracks; ++i)
    {
        m_timeTrialCustomTrackTrack[i].sound  = trackSounds[i];
        m_timeTrialCustomTrackTrack[i].action = a_timeCustomTrackChoose;
        // m_timeTrialCustomTrackTrack[i].param  = i;
        m_timeTrialCustomTrackTrack[i].name  = m_customTrackFiles[i];
        m_singleRaceCustomTrackTrack[i].sound  = trackSounds[i];
        m_singleRaceCustomTrackTrack[i].action = a_singleCustomTrackChoose;
        // m_singleRaceCustomTrackTrack[i].param  = i;
        m_singleRaceCustomTrackTrack[i].name  = m_customTrackFiles[i];
        m_multiHostCustomTrackTrack[i].sound  = trackSounds[i];
        m_multiHostCustomTrackTrack[i].action = a_multiHostCustomTrackChoose;
        // m_multiHostCustomTrackTrack[i].param  = i;
        m_multiHostCustomTrackTrack[i].name  = m_customTrackFiles[i];
    }
    m_timeTrialCustomTrackTrack[m_nCustomTracks-2].sound  = m_soundRandom;
    m_timeTrialCustomTrackTrack[m_nCustomTracks-2].action = a_timeCustomTrackChooseRandom;
    m_timeTrialCustomTrackTrack[m_nCustomTracks-1].sound  = m_soundBack;
    m_timeTrialCustomTrackTrack[m_nCustomTracks-1].action = a_back;
    m_singleRaceCustomTrackTrack[m_nCustomTracks-2].sound  = m_soundRandom;
    m_singleRaceCustomTrackTrack[m_nCustomTracks-2].action = a_singleCustomTrackChooseRandom;
    m_singleRaceCustomTrackTrack[m_nCustomTracks-1].sound  = m_soundBack;
    m_singleRaceCustomTrackTrack[m_nCustomTracks-1].action = a_back;
    m_multiHostCustomTrackTrack[m_nCustomTracks-2].sound  = m_soundRandom;
    m_multiHostCustomTrackTrack[m_nCustomTracks-2].action = a_multiHostCustomTrackChooseRandom;
    m_multiHostCustomTrackTrack[m_nCustomTracks-1].sound  = m_soundBack;
    m_multiHostCustomTrackTrack[m_nCustomTracks-1].action = a_back;
}


void
Menu::initializeVehicleMenu( )
{
    WIN32_FIND_DATA findFileData;
    HANDLE          findHandle = INVALID_HANDLE_VALUE;
    DWORD           error;
    
    DirectX::Sound* vehicleSounds[MAXCUSTOMVEHICLES];
    UInt            nVehicles = 0;
    findHandle = ::FindFirstFile("Vehicles\\*.vhc", &findFileData);
    if (findHandle == INVALID_HANDLE_VALUE) 
    {
//        RACE("Menu::initializeVehicleMenu : FindFirstFile failed with error 0x%x", GetLastError());
    } 
    else 
    {
        ::sprintf(m_vehicleFiles[nVehicles], "Vehicles\\%s", findFileData.cFileName);
        Int length = ::strlen(m_vehicleFiles[nVehicles]);
        Char soundFile[64];
        ::strncpy(soundFile, m_vehicleFiles[nVehicles], length-4);
        soundFile[length-4] = '\0';
        sprintf(soundFile, "%s.wav", soundFile);
        vehicleSounds[nVehicles] = m_soundManager->create(soundFile);
        if (vehicleSounds[nVehicles] != 0)
        {
            // vehiclefile and soundfile exist
//            RACE("Menu::initializeVehicleMenu : added %s", m_vehicleFiles[nVehicles]);
            ++nVehicles;
        }
        else
        {
            RACE("Menu::initializeVehicleMenu : no soundfile for vehicle %s found!", m_vehicleFiles[nVehicles]);
        }
        while ((nVehicles < MAXCUSTOMVEHICLES) && (::FindNextFile(findHandle, &findFileData) != 0)) 
        {
            ::sprintf(m_vehicleFiles[nVehicles], "Vehicles\\%s", findFileData.cFileName);
            length = ::strlen(m_vehicleFiles[nVehicles]);
            soundFile[64];
            ::strncpy(soundFile, m_vehicleFiles[nVehicles], length-4);
            soundFile[length-4] = '\0';
            sprintf(soundFile, "%s.wav", soundFile);
            vehicleSounds[nVehicles] = m_soundManager->create(soundFile);
            if (vehicleSounds[nVehicles] != 0)
            {
                // vehiclefile and soundfile exist
//                RACE("Menu::initializeVehicleMenu : added %s", m_vehicleFiles[nVehicles]);
                ++nVehicles;
            }
            else
            {
                RACE("Menu::initializeVehicleMenu : no soundfile for vehicle %s found!", m_vehicleFiles[nVehicles]);
            }
        }
        error = GetLastError();
        ::FindClose(findHandle);
        if (error != ERROR_NO_MORE_FILES) 
        {
//            RACE("Menu::initializeVehicleMenu : FindNextFile failed with error 0x%x", error);
        }
    }
    RACE("Menu::initializeVehicleMenu : %d user defined vehicles found", nVehicles);
    m_nVehicles = nVehicles + NVEHICLES + 2;
    // initialize the actual menus

    // Time trial
    m_timeTrialCircuitVehicle = new Item[m_nVehicles];
    for (UInt i = 0; i < nVehicles; ++i)
    {
        m_timeTrialCircuitVehicle[NVEHICLES + i].sound  = vehicleSounds[i];
        m_timeTrialCircuitVehicle[NVEHICLES + i].action = a_timeCircuitCustomVehicle;
//        m_timeTrialCircuitVehicle[NVEHICLES + i].param  = i;
        m_timeTrialCircuitVehicle[NVEHICLES + i].name  = m_vehicleFiles[i];
    }
    m_timeTrialCircuitVehicle[m_nVehicles-2].sound  = m_soundRandom;
    m_timeTrialCircuitVehicle[m_nVehicles-2].action = a_timeCircuitRandom;
    m_timeTrialCircuitVehicle[m_nVehicles-1].sound  = m_soundBack;
    m_timeTrialCircuitVehicle[m_nVehicles-1].action = a_back;
    // Street adventure
    m_timeTrialAdventureVehicle = new Item[m_nVehicles];
    for (UInt i = 0; i < nVehicles; ++i)
    {
        m_timeTrialAdventureVehicle[NVEHICLES + i].sound  = vehicleSounds[i];
        m_timeTrialAdventureVehicle[NVEHICLES + i].action = a_timeAdventureCustomVehicle;
//        m_timeTrialAdventureVehicle[NVEHICLES + i].param  = i;
        m_timeTrialAdventureVehicle[NVEHICLES + i].name  = m_vehicleFiles[i];
    }
    m_timeTrialAdventureVehicle[m_nVehicles-2].sound  = m_soundRandom;
    m_timeTrialAdventureVehicle[m_nVehicles-2].action = a_timeAdventureRandom;
    m_timeTrialAdventureVehicle[m_nVehicles-1].sound  = m_soundBack;
    m_timeTrialAdventureVehicle[m_nVehicles-1].action = a_back;

    m_timeTrialCustomTrackVehicle = new Item[m_nVehicles];
    for (UInt i = 0; i < nVehicles; ++i)
    {
        m_timeTrialCustomTrackVehicle[NVEHICLES + i].sound  = vehicleSounds[i];
        m_timeTrialCustomTrackVehicle[NVEHICLES + i].action = a_timeCustomTrackCustomVehicle;
//        m_timeTrialCustomTrackVehicle[NVEHICLES + i].param  = i;
        m_timeTrialCustomTrackVehicle[NVEHICLES + i].name  = m_vehicleFiles[i];
    }
    m_timeTrialCustomTrackVehicle[m_nVehicles-2].sound  = m_soundRandom;
    m_timeTrialCustomTrackVehicle[m_nVehicles-2].action = a_timeCustomTrackRandom;
    m_timeTrialCustomTrackVehicle[m_nVehicles-1].sound  = m_soundBack;
    m_timeTrialCustomTrackVehicle[m_nVehicles-1].action = a_back;

    m_timeTrialRandomVehicle = new Item[m_nVehicles];
    for (UInt i = 0; i < nVehicles; ++i)
    {
        m_timeTrialRandomVehicle[NVEHICLES + i].sound  = vehicleSounds[i];
        m_timeTrialRandomVehicle[NVEHICLES + i].action = a_timeRandomCustomVehicle;
//        m_timeTrialRandomVehicle[NVEHICLES + i].param  = i;
        m_timeTrialRandomVehicle[NVEHICLES + i].name  = m_vehicleFiles[i];
    }
    m_timeTrialRandomVehicle[m_nVehicles-2].sound  = m_soundRandom;
    m_timeTrialRandomVehicle[m_nVehicles-2].action = a_timeRandomRandom;
    m_timeTrialRandomVehicle[m_nVehicles-1].sound  = m_soundBack;
    m_timeTrialRandomVehicle[m_nVehicles-1].action = a_back;
    // Single race
    m_singleRaceCircuitVehicle = new Item[m_nVehicles];
    for (UInt i = 0; i < nVehicles; ++i)
    {
        m_singleRaceCircuitVehicle[NVEHICLES + i].sound  = vehicleSounds[i];
        m_singleRaceCircuitVehicle[NVEHICLES + i].action = a_singleCircuitCustomVehicle;
//        m_singleRaceCircuitVehicle[NVEHICLES + i].param  = i;
        m_singleRaceCircuitVehicle[NVEHICLES + i].name  = m_vehicleFiles[i];
    }
    m_singleRaceCircuitVehicle[m_nVehicles-2].sound  = m_soundRandom;
    m_singleRaceCircuitVehicle[m_nVehicles-2].action = a_singleCircuitRandom;
    m_singleRaceCircuitVehicle[m_nVehicles-1].sound  = m_soundBack;
    m_singleRaceCircuitVehicle[m_nVehicles-1].action = a_back;
    // Street adventure
    m_singleRaceAdventureVehicle = new Item[m_nVehicles];
    for (UInt i = 0; i < nVehicles; ++i)
    {
        m_singleRaceAdventureVehicle[NVEHICLES + i].sound  = vehicleSounds[i];
        m_singleRaceAdventureVehicle[NVEHICLES + i].action = a_singleAdventureCustomVehicle;
//        m_singleRaceAdventureVehicle[NVEHICLES + i].param  = i;
        m_singleRaceAdventureVehicle[NVEHICLES + i].name  = m_vehicleFiles[i];
    }
    m_singleRaceAdventureVehicle[m_nVehicles-2].sound  = m_soundRandom;
    m_singleRaceAdventureVehicle[m_nVehicles-2].action = a_singleAdventureRandom;
    m_singleRaceAdventureVehicle[m_nVehicles-1].sound  = m_soundBack;
    m_singleRaceAdventureVehicle[m_nVehicles-1].action = a_back;
    m_singleRaceCustomTrackVehicle = new Item[m_nVehicles];
    for (UInt i = 0; i < nVehicles; ++i)
    {
        m_singleRaceCustomTrackVehicle[NVEHICLES + i].sound  = vehicleSounds[i];
        m_singleRaceCustomTrackVehicle[NVEHICLES + i].action = a_singleCustomTrackCustomVehicle;
//        m_singleRaceCustomTrackVehicle[NVEHICLES + i].param  = i;
        m_singleRaceCustomTrackVehicle[NVEHICLES + i].name  = m_vehicleFiles[i];
    }
    m_singleRaceCustomTrackVehicle[m_nVehicles-2].sound  = m_soundRandom;
    m_singleRaceCustomTrackVehicle[m_nVehicles-2].action = a_singleCustomTrackRandom;
    m_singleRaceCustomTrackVehicle[m_nVehicles-1].sound  = m_soundBack;
    m_singleRaceCustomTrackVehicle[m_nVehicles-1].action = a_back;
    m_singleRaceRandomVehicle = new Item[m_nVehicles];
    for (UInt i = 0; i < nVehicles; ++i)
    {
        m_singleRaceRandomVehicle[NVEHICLES + i].sound  = vehicleSounds[i];
        m_singleRaceRandomVehicle[NVEHICLES + i].action = a_singleRandomCustomVehicle;
//        m_singleRaceRandomVehicle[NVEHICLES + i].param  = i;
        m_singleRaceRandomVehicle[NVEHICLES + i].name  = m_vehicleFiles[i];
    }
    m_singleRaceRandomVehicle[m_nVehicles-2].sound  = m_soundRandom;
    m_singleRaceRandomVehicle[m_nVehicles-2].action = a_singleRandomRandom;
    m_singleRaceRandomVehicle[m_nVehicles-1].sound  = m_soundBack;
    m_singleRaceRandomVehicle[m_nVehicles-1].action = a_back;
}



void
Menu::start( )
{
    // play logo
    Float introSkip = 0.0f;
    if (m_soundLogo1)
    {
        m_soundLogo1->play( );
        introSkip = m_soundLogo1->length();
    }
}


void
Menu::finalize( )
{
    RACE("Menu::finalize");
    fadeOut( );
    m_soundTheme1->stop( );
    m_soundTheme2->stop( );
    m_soundTheme3->stop( );
    // SAFE_DELETE(m_soundButton);
    SAFE_DELETE(m_soundTheme1);
    SAFE_DELETE(m_soundTheme2);
    SAFE_DELETE(m_soundTheme3);
    SAFE_DELETE(m_soundIntro);
    SAFE_DELETE(m_soundBack);
    SAFE_DELETE(m_soundQuickstart);
    SAFE_DELETE(m_soundTimeTrial);
    SAFE_DELETE(m_soundSingleRace);
    SAFE_DELETE(m_soundPickTrackType);
    SAFE_DELETE(m_soundRaceTrack);
    SAFE_DELETE(m_soundAdventure);
    SAFE_DELETE(m_soundCustomTrack);
    SAFE_DELETE(m_soundPickCircuit);
    SAFE_DELETE(m_soundAmerica);
    SAFE_DELETE(m_soundAustria);
    SAFE_DELETE(m_soundBelgium);
    SAFE_DELETE(m_soundBrazil);
    SAFE_DELETE(m_soundChina);
    SAFE_DELETE(m_soundEngland);
    SAFE_DELETE(m_soundFinland);
    SAFE_DELETE(m_soundFrance);
    SAFE_DELETE(m_soundGermany);
    SAFE_DELETE(m_soundIreland);
    SAFE_DELETE(m_soundItaly);
    SAFE_DELETE(m_soundNetherlands);
    SAFE_DELETE(m_soundPortugal);
    SAFE_DELETE(m_soundRussia);
    SAFE_DELETE(m_soundSpain);
    SAFE_DELETE(m_soundSweden);
    SAFE_DELETE(m_soundSwitserland);
    SAFE_DELETE(m_soundPickVehicle);
    for (UInt i = 0; i < NVEHICLES; ++i)
        SAFE_DELETE(m_soundVehicles[i]);
    SAFE_DELETE(m_soundRandom);
    SAFE_DELETE(m_soundPickAdventure);
    SAFE_DELETE(m_soundHills);
    SAFE_DELETE(m_soundCoast);
    SAFE_DELETE(m_soundCountry);
    SAFE_DELETE(m_soundAirport);
    SAFE_DELETE(m_soundDesert);
    SAFE_DELETE(m_soundRush);
    SAFE_DELETE(m_soundEscape);
    SAFE_DELETE(m_soundChoose);
    SAFE_DELETE(m_soundConfirm);
    SAFE_DELETE(m_soundAutomatic);
    SAFE_DELETE(m_soundManual);
    SAFE_DELETE(m_soundMultiplayer);
    SAFE_DELETE(m_soundMultiJoin);
    SAFE_DELETE(m_soundMultiJoinInternet);
    SAFE_DELETE(m_soundMultiHost);
    SAFE_DELETE(m_soundStartingServer);
    SAFE_DELETE(m_soundSearching);
    SAFE_DELETE(m_soundConnectFailed);
    SAFE_DELETE(m_soundRaceInProgress);
    SAFE_DELETE(m_soundRaceAborted);
    SAFE_DELETE(m_soundWaitingForPlayers);
    SAFE_DELETE(m_soundFailedToStart);
    SAFE_DELETE(m_soundServerFound);
    SAFE_DELETE(m_soundServersFound);
    SAFE_DELETE(m_soundServer);
    SAFE_DELETE(m_soundSelectServer);
    SAFE_DELETE(m_soundStopServer);
    SAFE_DELETE(m_soundDisconnectFromServer);
    SAFE_DELETE(m_soundConnected);
    SAFE_DELETE(m_soundServerStarted);
    SAFE_DELETE(m_soundQuit);
    SAFE_DELETE(m_soundMainMenu);
    SAFE_DELETE(m_soundOptions);
    SAFE_DELETE(m_soundSaved);
    SAFE_DELETE(m_soundDefaultsRestored);
    SAFE_DELETE(m_soundControls);
    SAFE_DELETE(m_soundSelectDevice);
    SAFE_DELETE(m_soundForceFeedback);
    SAFE_DELETE(m_soundAssignKeyboard);
    SAFE_DELETE(m_soundAssignJoystick);
    SAFE_DELETE(m_soundOn);
    SAFE_DELETE(m_soundOff);
    SAFE_DELETE(m_soundYes);
    SAFE_DELETE(m_soundNo);
    SAFE_DELETE(m_soundKeyboard);
    SAFE_DELETE(m_soundJoystick);
    SAFE_DELETE(m_soundGameSettings);
    SAFE_DELETE(m_soundCopilot);
    SAFE_DELETE(m_soundAutomaticInfo);
    SAFE_DELETE(m_soundCurvesOnly);
    SAFE_DELETE(m_soundAll);
    SAFE_DELETE(m_soundCurveAnnouncement);
    SAFE_DELETE(m_soundCurveFixed);
    SAFE_DELETE(m_soundCurveSpeed);
    SAFE_DELETE(m_soundNrOfLaps);
    SAFE_DELETE(m_soundNrOfComputers);
    SAFE_DELETE(m_soundDifficulty);
    SAFE_DELETE(m_soundDifficultyEasy);
    SAFE_DELETE(m_soundDifficultyNormal);
    SAFE_DELETE(m_soundDifficultyHard);
    SAFE_DELETE(m_soundThreeD);
    SAFE_DELETE(m_soundHardwareAcceleration);
    SAFE_DELETE(m_soundReverseStereo);
    SAFE_DELETE(m_soundRestoreDefaults);
    SAFE_DELETE(m_soundLeft);
    SAFE_DELETE_ARRAY(m_optionsLanguage);
    for (UInt i = 0; i < m_nCustomTracks - 2; ++i)
        SAFE_DELETE(m_timeTrialCustomTrackTrack[i].sound);
//    RACE("Menu::finalize : single race tracksounds deleted, time to delete arrays");
    SAFE_DELETE_ARRAY(m_timeTrialCustomTrackTrack);
    SAFE_DELETE_ARRAY(m_singleRaceCustomTrackTrack);
    SAFE_DELETE_ARRAY(m_multiHostCustomTrackTrack);
//    RACE("Menu::finalize : single race trackarrays deleted");
    for (UInt i = NVEHICLES; i < m_nVehicles - 2; ++i)
        SAFE_DELETE(m_timeTrialCircuitVehicle[i].sound);
    SAFE_DELETE_ARRAY(m_timeTrialCircuitVehicle);
    SAFE_DELETE_ARRAY(m_timeTrialAdventureVehicle);
    SAFE_DELETE_ARRAY(m_singleRaceCircuitVehicle);
    SAFE_DELETE_ARRAY(m_singleRaceAdventureVehicle);
    if (m_soundLogo1)
    {
        SAFE_DELETE(m_soundLogo1);
    }
    if (m_soundNSessions)
    {
        SAFE_DELETE(m_soundNSessions);
    }
    if (m_listServers)
    {
        SAFE_DELETE_ARRAY(m_listServers);
    }
    if ((m_calibratingJoystick) || (m_calibratingKeyboard))
    {
        stopCalibrating( );
    }
}


void
Menu::run(Float elapsed)
{
    DirectX::Input::State& input = m_game->input( );
    if (!m_soundTheme1->playing())
    {
        if ((m_soundLogo1) && (!m_soundLogo1->playing()))
        {
            m_soundTheme1->play(0, true);
            m_soundTheme2->play(0, true);
            m_soundTheme3->play(0, true);
            fadeIn();
            pushEvent(Event::playSound, 1.0f, m_soundIntro);
            pushEvent(Event::playSound, 1.5f + m_soundIntro->length(), m_soundMainMenu);
            pushEvent(Event::playCurrentItem, 1.5f + m_soundIntro->length() + m_soundMainMenu->length());
            pushEvent(Event::acceptInput, 1.5f + m_soundIntro->length() + m_soundMainMenu->length());
        }
        else if (!m_soundLogo1)
        {
            m_soundTheme1->play(0, true);
            m_soundTheme2->play(0, true);
            m_soundTheme3->play(0, true);
            fadeIn();
            if ((m_currentMenu == m_mainMenu) && (m_game->state() != Game::menu))
            {
                pushEvent(Event::playSound, 1.0f, m_soundIntro);
                pushEvent(Event::playSound, 1.5f + m_soundIntro->length(), m_soundMainMenu);
                pushEvent(Event::playCurrentItem, 1.5f + m_soundIntro->length() + m_soundMainMenu->length());
                pushEvent(Event::acceptInput, 1.5f + m_soundIntro->length() + m_soundMainMenu->length());
            }
        }
        else if ((m_soundLogo1) && (m_soundLogo1->playing()))
        {
            if (input.b1 == true)
                m_soundLogo1->stop();
        }
        switch (m_goto)
        {
            case quickStart:
                gotoMainMenu( );
                break;
            case timeTrial:
                gotoTimeTrial( );
                break;
            case singleRace:
                gotoSingleRace( );
                break;
            case multiplayer:
                gotoMultiplayer( );
                break;
            case multiHost:
                gotoMultiHost( );
                break;
            case multiJoin:
                gotoMultiJoinVehicle( );
                break;
            case options:
                gotoOptions( );
                break;
            case optionsGameSettings:
                gotoOptionsGameSettings( );
                break;
            default:
                break;
        }
    }
    if ((!m_calibratingJoystick) && (!m_calibratingKeyboard) && (m_game->raceClient()->sessionLost( )) && (!m_game->serverStarted( )))
    {
        flushEvents( );
        DirectX::Sound* serverLost = m_game->loadLanguageSound("race\\multiplayer\\serverlost");
        if (serverLost)
        {
            serverLost->play( );
            ::Sleep(DWORD(serverLost->length() * 1000.0f));
            m_game->resetTimer( );
            SAFE_DELETE(serverLost);
        }
        m_game->raceClient()->sessionLost(false);
        gotoMultiplayer( );
    }
    if ((!m_calibratingJoystick) && (!m_calibratingKeyboard) && (m_game->raceClient()->forceDisconnected( )) && ((m_currentMenu == m_multiplayer) || (m_currentMenu == m_listServers)))
    {
        flushEvents( );
        DirectX::Sound* serverFull = m_game->loadLanguageSound("menu\\serverfull");
        if (serverFull)
        {
            serverFull->play( );
            ::Sleep(DWORD(serverFull->length( ) * 1000.0f));
            m_game->resetTimer( );
           SAFE_DELETE(serverFull);
        }
        m_game->raceClient()->forceDisconnected(false);
        gotoMultiplayer( );
    }
    if ((!m_calibratingJoystick) && (!m_calibratingKeyboard) && (m_game->raceClient()->raceAborted( )) && (!m_game->serverStarted( )))
    {
        flushEvents( );
        m_soundRaceAborted->play( );
        ::Sleep(DWORD(m_soundRaceAborted->length( ) * 1000.0f));
        m_game->resetTimer( );
    }
    if ((!m_calibratingJoystick) && (!m_calibratingKeyboard) && (m_game->raceClient()->connected( )) && (!m_game->serverStarted( )))
    {
        if (!m_game->raceClient()->startRace( ))
        {
            if (!m_multiAcceptingPlayers)
            {
                m_soundWaitingForPlayers->play( );
                ::Sleep(DWORD(m_soundWaitingForPlayers->length( ) * 1000.0f));
                m_game->resetTimer( );
                m_multiAcceptingPlayers = true;
            }
        }
        else
        {
            m_multiAcceptingPlayers = false;
        }
    }
    if ((!m_calibratingJoystick) && (!m_calibratingKeyboard) && (m_acceptInput))
    {
        if ((!input.b1) && (!input.keys[DIK_BACK]) && (!input.keys[DIK_ESCAPE]) && (!input.pov2) && (!input.pov4) && (!input.pov6) && (!input.pov8))
            m_b1released = true;
        if (((input.b1) || (input.pov2) || (input.pov6)) && (m_b1released))
        {
            m_b1released = false;
            // m_soundButton->play( );
            if (handleMenuItem(m_currentMenu[m_currentMenuItem]))
                return;
        }
        if (m_currentMenu == m_mainMenu)
        {
            if ((input.keys[DIK_Q]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[0]))
                    return;
            }
            if ((input.keys[DIK_T]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[1]))
                    return;
            }
            if ((input.keys[DIK_S]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[2]))
                    return;
            }
            if ((input.keys[DIK_M]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[3]))
                    return;
            }
            if ((input.keys[DIK_O]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[4]))
                    return;
            }
            if ((input.keys[DIK_E]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[5]))
                    return;
            }
        }
        else if (m_currentMenu == m_multiplayer)
        {
            if ((input.keys[DIK_L]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[0]))
                    return;
            }
            if ((input.keys[DIK_I]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[1]))
                    return;
            }
            if ((input.keys[DIK_H]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[2]))
                    return;
            }
            if (((input.keys[DIK_G]) || (input.keys[DIK_B])) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[3]))
                    return;
            }
        }
        else if ((m_currentMenu == m_timeTrial) || (m_currentMenu == m_singleRace) || (m_currentMenu == m_multiHost))
        {
            if ((input.keys[DIK_R]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[0]))
                    return;
            }
            if ((input.keys[DIK_S]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[1]))
                    return;
            }
            if ((input.keys[DIK_C]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[2]))
                    return;
            }
            if ((input.keys[DIK_R]) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[3]))
                    return;
            }
            if (((input.keys[DIK_G]) || (input.keys[DIK_B])) && (m_b1released))
            {
                m_b1released = false;
                // m_soundButton->play( );
                if (handleMenuItem(m_currentMenu[4]))
                    return;
            }
        }
        if (((input.keys[DIK_BACK]) || (input.keys[DIK_ESCAPE]) || (input.pov4) || (input.pov8)) && (m_b1released))
        {
//            if (m_currentMenu != m_mainMenu)
//            {
                if (handleMenuItem(m_currentMenu[m_currentMenuSize / sizeof(Item) - 1]))
                    return;
//            }
        }
        if (input.keys[DIK_F1])
            m_soundLeft->play( );
        if ((input.keys[DIK_SLASH]) 
&& (m_currentMenu[m_currentMenuItem].action != a_back) 
&& (m_currentMenu[m_currentMenuItem].action != a_timeCircuitChooseRandom) 
&& (m_currentMenu[m_currentMenuItem].action != a_timeAdventureChooseRandom)
&& (m_currentMenu[m_currentMenuItem].action != a_timeCustomTrackChooseRandom) 
&& (m_currentMenu[m_currentMenuItem].action != a_singleCircuitChooseRandom) 
&& (m_currentMenu[m_currentMenuItem].action != a_singleAdventureChooseRandom) 
&& (m_currentMenu[m_currentMenuItem].action != a_singleCustomTrackChooseRandom) 
&& (m_currentMenu[m_currentMenuItem].action != a_multiHostCircuitChooseRandom) 
&& (m_currentMenu[m_currentMenuItem].action != a_multiHostAdventureChooseRandom) 
&& (m_currentMenu[m_currentMenuItem].action != a_multiHostCustomTrackChooseRandom))
        {
            if ((m_currentMenu == m_timeTrialCircuitTrack) || (m_currentMenu == m_timeTrialAdventureTrack) || (m_currentMenu == m_singleRaceCircuitTrack) || (m_currentMenu == m_singleRaceAdventureTrack) || (m_currentMenu == m_multiHostCircuitTrack) || (m_currentMenu == m_multiHostAdventureTrack) || (m_currentMenu == m_timeTrialCustomTrackTrack) || (m_currentMenu == m_singleRaceCustomTrackTrack) || (m_currentMenu == m_multiHostCustomTrackTrack))
            {
                stopCurrentMenuItem( );
                m_acceptInput = false;
                sayHighScores(m_currentMenu[m_currentMenuItem].name);
            }
        }
        if ((input.x > -50) && (input.y < 50) && (!input.pov1) && (!input.pov5))
            m_prevReleased = true;
        if (((input.x < -50) || (input.y > 50) || (input.pov1) || (input.pov5)) && (m_prevReleased))
        {
            m_prevReleased = false;
            stopCurrentMenuItem( );
            m_currentMenuItem = (m_currentMenuItem - 1 + (m_currentMenuSize/sizeof(Item))) % (m_currentMenuSize / sizeof(Item));        
            playCurrentMenuItem( );
        }
        if ((input.x < 50) && (input.y > -50) && (!input.pov3) && (!input.pov7))
            m_nextReleased = true;
        if (((input.x > 50) || (input.y < -50) || (input.pov3) || (input.pov7)) && (m_nextReleased))
        {
            m_nextReleased = false;
            stopCurrentMenuItem( );
            m_currentMenuItem = (m_currentMenuItem + 1) % (m_currentMenuSize / sizeof(Item));
            playCurrentMenuItem( );
        }
    }
    else if ((!m_calibratingJoystick) && (!m_calibratingKeyboard) && (m_elapsedTotal > 1.0f) && (input.b1) && (m_soundIntro->playing( )))
    {
        m_soundIntro->stop( );
        m_elapsedTotal += m_soundIntro->length( ) + 0.5f;
        flushEvents( );
        m_b1released = false;
        m_soundMainMenu->play();
//        pushEvent(Event::playSound, 0.0f, m_soundMainMenu);
        pushEvent(Event::playCurrentItem, m_soundMainMenu->length());
        pushEvent(Event::acceptInput, m_soundMainMenu->length());
        return;
    }
    else if (m_calibratingJoystick)
    {
        calibrateJoystick(input);
        // return;
    }
    else if (m_calibratingKeyboard)
    {
        calibrateKeyboard(input);
        // return;
    }
    // Handle events
    Event* e = 0;
    while (e = m_eventList.next(e))
    {
        if (e->time <= m_elapsedTotal)
        {
            switch (e->type)
            {
            case Event::playSound:
                // ugly hack, please fix properly
                /* if (e->sound == m_soundMainMenu)
                {
                    m_acceptInput = false;
                    stopCurrentMenuItem( );
                    pushEvent(Event::playCurrentItem, m_soundMainMenu->length());
                    pushEvent(Event::acceptInput, m_soundMainMenu->length());
                } */
                // play a sound
                if (e->sound)
                {
                    e->sound->reset( );
                    e->sound->play( );
                }
                break;
            case Event::acceptInput:
                // start accepting input
                m_acceptInput = true;
                break;
            case Event::stopSessionEnum:
                // give session enumerating result
                m_game->stopEnumSessions( );
                m_nSessions = m_game->raceClient()->nSessions( );
                if (m_nSessions > 59)
                    m_nSessions = 59;
                    sprintf(m_nSessionsSound, "numbers\\%d", m_nSessions);
                if (m_soundNSessions)
                    SAFE_DELETE(m_soundNSessions)
                m_soundNSessions = m_game->loadLanguageSound(m_nSessionsSound);
                m_soundNSessions->play( );
                ::Sleep(DWORD(m_soundNSessions->length() * 1000.0f));
                m_game->resetTimer();
                if (m_nSessions == 0)
                {
                    m_soundServersFound->play();
                    ::Sleep(DWORD(m_soundServersFound->length() * 1000.0f));
                    m_game->resetTimer();
                    gotoMultiplayer();
                }
                else if (m_nSessions == 1)
                {
                    m_soundServerFound->play();
                    ::Sleep(DWORD(m_soundServerFound->length() * 1000.0f));
                    m_game->resetTimer();
                    gotoMultiplayerListServers();
                }
                else
                {
                    m_soundServersFound->play();
                    ::Sleep(DWORD(m_soundServersFound->length() * 1000.0f));
                    m_game->resetTimer();
                    gotoMultiplayerListServers();
                }
                break;
            case Event::stopSessionEnumAndJoin:
                m_game->stopEnumSessions( );
                if (m_game->raceClient()->nSessions( ) > 0)
                {
                    RACE("Menu::run : server joining own game...");
                    m_game->joinSession(0);
                    gotoMultiHost( );            
                }
                else
                {
                    RACE("(!) Menu::run : server not successfully started...");
                    gotoMultiplayer( );
                }
                break;
            case Event::playCurrentItem:
                playCurrentMenuItem( );
                break;
            default:
                break;
            }
            m_eventList.purge(e);
            delete e;
            e = 0;
        }
    }
    m_elapsedTotal += elapsed;
}


void 
Menu::pushEvent(Event::Type type, Float time, DirectX::Sound* sound)
{
    Event* e = new Event;
    e->type = type;
    e->time = m_elapsedTotal + time;
    e->sound = sound;
    m_eventList.push(e);
}

void 
Menu::flushEvents()
{
    Event* e = 0;
    while (e = m_eventList.next(e))
    {
        m_eventList.purge(e);
        delete e;
        e = 0;
    }
}

void
Menu::playCurrentMenuItem( )
{
    m_currentMenu[m_currentMenuItem].sound->play( );
    if (m_currentMenu[m_currentMenuItem].action == a_multiSelectServer)
    {
        UInt serverId  = m_currentMenu[m_currentMenuItem].param;
        UInt thousands = (serverId/1000)%10;
        UInt hundreds  = (serverId/100)%10;
        UInt tens      = (serverId/10)%10;
        UInt units     = (serverId)%10;
//RACE("Server ID = %d", serverId);
        pushEvent(Event::playSound, m_soundServer->length(), m_game->m_soundNumbers[thousands]);
        pushEvent(Event::playSound, m_soundServer->length() + m_game->m_soundNumbers[thousands]->length(),
                  m_game->m_soundNumbers[hundreds]);
        pushEvent(Event::playSound, m_soundServer->length() + m_game->m_soundNumbers[thousands]->length() +
                  m_game->m_soundNumbers[hundreds]->length(), m_game->m_soundNumbers[tens]);
        pushEvent(Event::playSound, m_soundServer->length() + m_game->m_soundNumbers[thousands]->length() +
                  m_game->m_soundNumbers[hundreds]->length() + m_game->m_soundNumbers[tens]->length(),
                  m_game->m_soundNumbers[units]);
    }
}

void
Menu::stopCurrentMenuItem( )
{
    if (m_currentMenu[m_currentMenuItem].sound->playing( ))
    {
        m_currentMenu[m_currentMenuItem].sound->stop( );
        m_currentMenu[m_currentMenuItem].sound->reset( );
    }
    if (m_currentMenu[m_currentMenuItem].action == a_multiSelectServer)
    {
        // pushEvent(Event::flushEvents, 0.0f);
        flushEvents( );
    }
}

Boolean
Menu::handleMenuItem(Item& item)
{
    Boolean shouldReturn = false;
    switch (item.action)
    {
    case a_quickStart:
        randomTrack( );
        randomVehicle(m_game->raceSettings().randomCustomVehicles);
        m_game->nextAutomaticTransmission(true);
        m_game->state(Game::quickStart);
        shouldReturn = true;
        break;
    case a_timeTrial:
        gotoTimeTrial( );
        break;
    case a_singleRace:
        gotoSingleRace( );
        break;
    case a_options:
        gotoOptions( );
        break;
    case a_quit:
        fadeOut();
        ::exit(0);
        break;
    case a_back:
        if (m_currentMenu == m_timeTrial)
            gotoMainMenu();
        else if (m_currentMenu == m_singleRace)
            gotoMainMenu();
        else if (m_currentMenu == m_timeTrialCircuitTrack)
            gotoTimeTrial( );
        else if (m_currentMenu == m_timeTrialCircuitVehicle)
            gotoTimeTrialCircuitTrack( );
        else if (m_currentMenu == m_timeTrialCircuitTrans)
            gotoTimeTrialCircuitVehicle( );
        else if (m_currentMenu == m_timeTrialAdventureTrack)
            gotoTimeTrial( );
        else if (m_currentMenu == m_timeTrialAdventureVehicle)
            gotoTimeTrialAdventureTrack( );
        else if (m_currentMenu == m_timeTrialAdventureTrans)
            gotoTimeTrialAdventureVehicle( );
        else if (m_currentMenu == m_timeTrialCustomTrackTrack)
            gotoTimeTrial( );
        else if (m_currentMenu == m_timeTrialCustomTrackVehicle)
            gotoTimeTrialCustomTrackTrack( );
        else if (m_currentMenu == m_timeTrialCustomTrackTrans)
            gotoTimeTrialCustomTrackVehicle( );
        else if (m_currentMenu == m_timeTrialRandomVehicle)
            gotoTimeTrial( );
        else if (m_currentMenu == m_timeTrialRandomTrans)
            gotoTimeTrialRandomVehicle( );
        else if (m_currentMenu == m_singleRaceCircuitTrack)
            gotoSingleRace( );
        else if (m_currentMenu == m_singleRaceCircuitVehicle)
            gotoSingleRaceCircuitTrack( );
        else if (m_currentMenu == m_singleRaceCircuitTrans)
            gotoSingleRaceCircuitVehicle( );
        else if (m_currentMenu == m_singleRaceAdventureTrack)
            gotoSingleRace( );
        else if (m_currentMenu == m_singleRaceAdventureVehicle)
            gotoSingleRaceAdventureTrack( );
        else if (m_currentMenu == m_singleRaceAdventureTrans)
            gotoSingleRaceAdventureVehicle( );
        else if (m_currentMenu == m_singleRaceCustomTrackTrack)
            gotoSingleRace( );
        else if (m_currentMenu == m_singleRaceCustomTrackVehicle)
            gotoSingleRaceCustomTrackTrack( );
        else if (m_currentMenu == m_singleRaceCustomTrackTrans)
            gotoSingleRaceCustomTrackVehicle( );
        else if (m_currentMenu == m_singleRaceRandomVehicle)
            gotoSingleRace( );
        else if (m_currentMenu == m_singleRaceRandomTrans)
            gotoSingleRaceRandomVehicle( );
        else if (m_currentMenu == m_multiplayer)
            gotoMainMenu();
        else if (m_currentMenu == m_listServers)
            gotoMultiplayer( );
        else if (m_currentMenu == m_multiHostCircuitTrack)
            gotoMultiHost( );
        else if (m_currentMenu == m_multiHostCircuitVehicle)
            gotoMultiHostCircuitTrack( );
        else if (m_currentMenu == m_multiHostCircuitTrans)
            gotoMultiHostCircuitVehicle( );
        else if (m_currentMenu == m_multiHostAdventureTrack)
            gotoMultiHost( );
        else if (m_currentMenu == m_multiHostAdventureVehicle)
            gotoMultiHostAdventureTrack( );
        else if (m_currentMenu == m_multiHostAdventureTrans)
            gotoMultiHostAdventureVehicle( );
        else if (m_currentMenu == m_multiHostCustomTrackTrack)
            gotoMultiHost( );
        else if (m_currentMenu == m_multiHostCustomTrackVehicle)
            gotoMultiHostCustomTrackTrack( );
        else if (m_currentMenu == m_multiHostCustomTrackTrans)
            gotoMultiHostCustomTrackVehicle( );
        else if (m_currentMenu == m_multiHostRandomVehicle)
            gotoMultiHost( );
        else if (m_currentMenu == m_multiHostRandomTrans)
            gotoMultiHostRandomVehicle( );
        else if (m_currentMenu == m_multiHostOptions)
            gotoMultiHost( );
        else if (m_currentMenu == m_multiHostOptionsNrOfLaps)
            gotoMultiHostOptions( );
        else if (m_currentMenu == m_multiJoinTrans)
            gotoMultiJoinVehicle( );
        else if (m_currentMenu == m_options)
            if (m_game->serverStarted( ))
                gotoMultiHost( );
            else if (m_game->raceClient()->connected( ))
                gotoMultiJoinVehicle( );
            else
                gotoMainMenu( );
        else if (m_currentMenu == m_optionsControls)
            gotoOptions( );
        else if (m_currentMenu == m_optionsDevice)
            gotoOptionsControls( );
        else if (m_currentMenu == m_optionsForceFeedback)
            gotoOptionsControls( );
        else if (m_currentMenu == m_optionsGameSettings)
            gotoOptions( );
        else if (m_currentMenu == m_optionsRaceSettings)
            gotoOptions( );
        else if (m_currentMenu == m_optionsCopilot)
            gotoOptionsRaceSettings( );
        else if (m_currentMenu == m_optionsCurves)
            gotoOptionsRaceSettings( );
        else if (m_currentMenu == m_optionsRequestInfo)
            gotoOptionsRaceSettings( );
        else if (m_currentMenu == m_optionsNrOfLaps)
            gotoOptionsRaceSettings( );
        else if (m_currentMenu == m_optionsNrOfComputers)
            gotoOptionsRaceSettings( );
        else if (m_currentMenu == m_optionsLanguage)
            gotoOptionsGameSettings( );
        else if (m_currentMenu == m_optionsRandomCustomTracks)
            gotoOptionsGameSettings( );
        else if (m_currentMenu == m_optionsRandomCustomVehicles)
            gotoOptionsGameSettings( );
        else if (m_currentMenu == m_optionsDifficulty)
            gotoOptionsRaceSettings( );
//        else if (m_currentMenu == m_optionsSingleCustomVehicles)
//            gotoOptionsRaceSettings( );
        else if (m_currentMenu == m_optionsThreeD)
            gotoOptionsGameSettings( );
        else if (m_currentMenu == m_optionsHardwareAcceleration)
            gotoOptionsGameSettings( );
        else if (m_currentMenu == m_optionsReverseStereo)
            gotoOptionsGameSettings( );
        else if (m_currentMenu == m_optionsRestore)
            gotoOptions( );
        break;
    // Time trial
    case a_timeTrialCircuit:
        gotoTimeTrialCircuitTrack( );
        break;
    case a_timeTrialAdventure:
        gotoTimeTrialAdventureTrack( );
        break;
    case a_timeTrialCustomTrack:
        gotoTimeTrialCustomTrackTrack( );
        break;
    case a_timeTrialRandom:
        randomTrack( );
        gotoTimeTrialRandomVehicle( );
        break;
    case a_timeCircuitChoose:
        m_game->nextTrack(m_currentMenu[m_currentMenuItem].name);
        gotoTimeTrialCircuitVehicle( );
        break;
    case a_timeCircuitChooseRandom:
        randomCircuit( );
        gotoTimeTrialCircuitVehicle( );
        break;
    case a_timeCircuitCustomVehicle:
        m_game->nextVehicle(0, m_currentMenu[m_currentMenuItem].name);
        gotoTimeTrialCircuitTrans( );
        break;
    case a_timeCircuitOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoTimeTrialCircuitTrans( );
        break;
    case a_timeCircuitRandom:
        randomVehicle(m_game->raceSettings().randomCustomVehicles);
        gotoTimeTrialCircuitTrans( );
        break;
    case a_timeAutomatic:
        m_game->nextAutomaticTransmission(true);
        m_game->state(Game::timeTrial);
        shouldReturn = true;
        break;
    case a_timeManual:
        m_game->nextAutomaticTransmission(false);
        m_game->state(Game::timeTrial);
        shouldReturn = true;
        break;
    case a_timeAdventureChoose:
        m_game->nextTrack(m_currentMenu[m_currentMenuItem].name);
        gotoTimeTrialAdventureVehicle( );
        break;
    case a_timeAdventureChooseRandom:
        randomAdventure( );
        gotoTimeTrialAdventureVehicle( );
        break;
    case a_timeAdventureCustomVehicle:
        m_game->nextVehicle(0, m_currentMenu[m_currentMenuItem].name);
        gotoTimeTrialAdventureTrans( );
        break;
    case a_timeAdventureOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoTimeTrialAdventureTrans( );
        break;
    case a_timeAdventureRandom:
        randomVehicle(m_game->raceSettings().randomCustomVehicles);
        gotoTimeTrialAdventureTrans( );
        break;
    case a_timeCustomTrackChoose:
        m_game->nextTrack(m_currentMenu[m_currentMenuItem].name);
        gotoTimeTrialCustomTrackVehicle( );
        break;
    case a_timeCustomTrackChooseRandom:
        randomCustomTrack( );
        gotoTimeTrialCustomTrackVehicle( );
        break;
    case a_timeCustomTrackCustomVehicle:
        m_game->nextVehicle(0, m_currentMenu[m_currentMenuItem].name);
        gotoTimeTrialCustomTrackTrans( );
        break;
    case a_timeCustomTrackOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoTimeTrialCustomTrackTrans( );
        break;
    case a_timeCustomTrackRandom:
        randomVehicle(m_game->raceSettings().randomCustomVehicles);
        gotoTimeTrialCustomTrackTrans( );
        break;
    case a_timeRandomCustomVehicle:
        m_game->nextVehicle(0, m_currentMenu[m_currentMenuItem].name);
        gotoTimeTrialRandomTrans( );
        break;
    case a_timeRandomOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoTimeTrialRandomTrans( );
        break;
    case a_timeRandomRandom:
        randomVehicle(m_game->raceSettings().randomCustomVehicles);
        gotoTimeTrialRandomTrans( );
        break;
    // Single Race
    case a_singleRaceCircuit:
        gotoSingleRaceCircuitTrack( );
        break;
    case a_singleRaceAdventure:
        gotoSingleRaceAdventureTrack( );
        break;
    case a_singleRaceCustomTrack:
        gotoSingleRaceCustomTrackTrack( );
        break;
    case a_singleRaceRandom:
        randomTrack( );
        gotoSingleRaceRandomVehicle( );
        break;
    case a_singleCircuitChoose:
        m_game->nextTrack(m_currentMenu[m_currentMenuItem].name);
        gotoSingleRaceCircuitVehicle( );
        break;
    case a_singleCircuitChooseRandom:
        randomCircuit( );
        gotoSingleRaceCircuitVehicle( );
        break;
    case a_singleCircuitCustomVehicle:
        m_game->nextVehicle(0, m_currentMenu[m_currentMenuItem].name);
        gotoSingleRaceCircuitTrans( );
        break;
    case a_singleCircuitOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoSingleRaceCircuitTrans( );
        break;
    case a_singleCircuitRandom:
        randomVehicle(m_game->raceSettings().randomCustomVehicles);
        gotoSingleRaceCircuitTrans( );
        break;
    case a_singleAutomatic:
        m_game->nextAutomaticTransmission(true);
        m_game->state(Game::singleRace);
        shouldReturn = true;
        break;
    case a_singleManual:
        m_game->nextAutomaticTransmission(false);
        m_game->state(Game::singleRace);
        shouldReturn = true;
        break;
    case a_singleAdventureChoose:
        m_game->nextTrack(m_currentMenu[m_currentMenuItem].name);
        gotoSingleRaceAdventureVehicle( );
        break;
    case a_singleAdventureChooseRandom:
        randomAdventure( );
        gotoSingleRaceAdventureVehicle( );
        break;
    case a_singleAdventureCustomVehicle:
        m_game->nextVehicle(0, m_currentMenu[m_currentMenuItem].name);
        gotoSingleRaceAdventureTrans( );
        break;
    case a_singleAdventureOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoSingleRaceAdventureTrans( );
        break;
    case a_singleAdventureRandom:
        randomVehicle(m_game->raceSettings().randomCustomVehicles);
        gotoSingleRaceAdventureTrans( );
        break;
    case a_singleCustomTrackChoose:
        m_game->nextTrack(m_currentMenu[m_currentMenuItem].name);
        gotoSingleRaceCustomTrackVehicle( );
        break;
    case a_singleCustomTrackChooseRandom:
        randomCustomTrack( );
        gotoSingleRaceCustomTrackVehicle( );
        break;
    case a_singleCustomTrackCustomVehicle:
        m_game->nextVehicle(0, m_currentMenu[m_currentMenuItem].name);
        gotoSingleRaceCustomTrackTrans( );
        break;
    case a_singleCustomTrackOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoSingleRaceCustomTrackTrans( );
        break;
    case a_singleCustomTrackRandom:
        randomVehicle(m_game->raceSettings().randomCustomVehicles);
        gotoSingleRaceCustomTrackTrans( );
        break;
    case a_singleRandomCustomVehicle:
        m_game->nextVehicle(0, m_currentMenu[m_currentMenuItem].name);
        gotoSingleRaceRandomTrans( );
        break;
    case a_singleRandomOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoSingleRaceRandomTrans( );
        break;
    case a_singleRandomRandom:
        randomVehicle(m_game->raceSettings().randomCustomVehicles);
        gotoSingleRaceRandomTrans( );
        break;
    case a_multiplayer:
        //m_game->state(Game::multiplayer);
        //shouldReturn = true;
        gotoMultiplayer( );
        break;
    case a_multiJoin:
        stopCurrentMenuItem();
        m_acceptInput = false;
        m_game->startClient( );
        m_game->startEnumSessions( );
        m_soundSearching->play( );
        pushEvent(Event::stopSessionEnum, 5.0f);
        break;
    case a_multiJoinInternet:
        {
            stopCurrentMenuItem();
            File* internetFile = new File("TopSpeed.cfg", File::read);
            Char buffer[256];
            sprintf(buffer, "127.0.0.1");
            if (internetFile->opened( ))
            {
                internetFile->readString("Multiplayer", buffer, 256, "127.0.0.1");
            }
            else
            {
                SAFE_DELETE(internetFile);
                internetFile = new File("TopSpeed.cfg", File::create | File::write);
                fprintf(internetFile->getStream( ), "[Settings]\n");
                internetFile->writeKeyInt("EnableTracing", 0);
                internetFile->writeKeyString("Multiplayer", buffer);
            }
            SAFE_DELETE(internetFile);
            m_acceptInput = false;
            m_game->startClient( );
            m_soundSearching->play( );
            if (m_game->joinSessionAt(buffer) == dxSuccess)
            {
                m_soundSearching->stop( );
                m_soundSearching->reset( );
                while (m_game->state( ) == Game::menu)
                {
                    if (m_game->raceClient()->forceDisconnected( ))
                        break;
                    if (m_game->raceClient()->connected( ))
                    {
                        m_soundConnected->play( );
                        ::Sleep(DWORD(m_soundConnected->length( ) * 1000.0f));
                        m_soundYouAre->play( );
                        ::Sleep(DWORD(m_soundYouAre->length( ) * 1000.0f));
                        m_soundPlayer->play( );
                        ::Sleep(DWORD(m_soundPlayer->length( ) * 1000.0f));
                        UInt playerNr = m_game->raceClient()->playerNumber();
                        m_game->m_soundNumbers[playerNr+1]->play( );
                        ::Sleep(DWORD(m_game->m_soundNumbers[playerNr+1]->length( ) * 1000.0f));
                        m_game->resetTimer( );
                        gotoMultiJoinVehicle( );
                        break;
                    }
                    ::Sleep(10);
                }
            }
            else
            {
                m_soundSearching->stop( );
                m_soundSearching->reset( );
                m_soundConnectFailed->play( );
                ::Sleep(DWORD(m_soundConnectFailed->length( ) * 1000.0f));
                m_game->resetTimer( );
                gotoMultiplayer( );
            }
        }
        break;
    case a_multiHost:
        stopCurrentMenuItem();
        m_soundStartingServer->play( );
        m_game->startServer( );
        m_game->startClient( );
        //m_game->startEnumSessions("127.0.0.1");
        m_acceptInput = false;
        if (m_game->joinSessionAt("127.0.0.1") == dxSuccess)
        {
            m_soundStartingServer->stop( );
            m_soundStartingServer->reset( );
            m_soundServerStarted->play( );
            ::Sleep(DWORD(m_soundServerStarted->length( ) * 1000.0f));
            if (m_game->raceClient()->connected( ))
            {
                UInt playerNr = m_game->raceClient()->playerNumber();
                m_soundYouAre->play( );
                ::Sleep(DWORD(m_soundYouAre->length( ) * 1000.0f));
                m_soundPlayer->play( );
                ::Sleep(DWORD(m_soundPlayer->length( ) * 1000.0f));
                m_game->m_soundNumbers[playerNr+1]->play( );
                ::Sleep(DWORD(m_game->m_soundNumbers[playerNr+1]->length( ) * 1000.0f));
                m_game->resetTimer( );
                gotoMultiHost( ); 
            }
        }
        else
        {
            m_soundStartingServer->stop( );
            m_soundStartingServer->reset( );
            m_soundFailedToStart->play( );
            ::Sleep(DWORD(m_soundFailedToStart->length( ) * 1000.0f));
            m_game->resetTimer( );
            gotoMultiplayer( );
        }
        //pushEvent(Event::acceptInput, 5.0f);
        //pushEvent(Event::stopSessionEnumAndJoin, 5.0f);
        break;
    case a_multiSelectServer:
        stopCurrentMenuItem( );
        if (m_game->joinSession(m_currentMenuItem) != dxSuccess)
        {
            m_soundConnectFailed->play( );
            ::Sleep(DWORD(m_soundConnectFailed->length( ) * 1000.0f));
            m_game->resetTimer( );
            gotoMultiplayer( );
        }
        else 
        {
            while (m_game->state( ) == Game::menu)
            {
    if (m_game->raceClient()->forceDisconnected( ))
        break;
                if (m_game->raceClient()->connected( ))
                {
                    m_soundConnected->play( );
                    ::Sleep(DWORD(m_soundConnected->length( ) * 1000.0f));
                    m_soundYouAre->play( );
                    ::Sleep(DWORD(m_soundYouAre->length( ) * 1000.0f));
                    m_soundPlayer->play( );
                    ::Sleep(DWORD(m_soundPlayer->length( ) * 1000.0f));
                    UInt playerNr = m_game->raceClient()->playerNumber();
                    m_game->m_soundNumbers[playerNr+1]->play( );
                    ::Sleep(DWORD(m_game->m_soundNumbers[playerNr+1]->length( ) * 1000.0f));
                    m_game->resetTimer( );
                    gotoMultiJoinVehicle( );
                    break;
                }
                ::Sleep(10);
            }
        }
        break;
    case a_multiHostCircuit:
        gotoMultiHostCircuitTrack( );
        break;
    case a_multiHostAdventure:
        gotoMultiHostAdventureTrack( );
        break;
    case a_multiHostCustomTrack:
        gotoMultiHostCustomTrackTrack( );
        break;
    case a_multiHostRandom:
        randomTrack( );
        gotoMultiHostRandomVehicle( );
        break;
    case a_multiHostOptions:
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps:
        gotoMultiHostOptionsNrOfLaps( );
        break;
    case a_multiHostOptionsNrOfLaps1:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 1;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps2:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 2;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps3:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 3;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps4:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 4;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps5:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 5;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps6:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 6;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps7:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 7;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps8:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 8;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps9:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 9;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps10:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 10;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps11:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 11;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps12:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 12;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps13:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 13;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps14:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 14;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps15:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 15;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostOptionsNrOfLaps16:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 16;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoMultiHostOptions( );
        break;
    case a_multiHostCircuitChoose:
        m_game->nextTrack(m_currentMenu[m_currentMenuItem].name);
        gotoMultiHostCircuitVehicle( );
        break;
    case a_multiHostCircuitChooseRandom:
        randomCircuit( );
        gotoMultiHostCircuitVehicle( );
        break;
    case a_multiHostAdventureChoose:
        m_game->nextTrack(m_currentMenu[m_currentMenuItem].name);
        gotoMultiHostAdventureVehicle( );
        break;
    case a_multiHostAdventureChooseRandom:
        randomAdventure( );
        gotoMultiHostAdventureVehicle( );
        break;
    case a_multiHostCustomTrackChoose:
        m_game->nextTrack(m_currentMenu[m_currentMenuItem].name);
        gotoMultiHostCustomTrackVehicle( );
        break;
    case a_multiHostCustomTrackChooseRandom:
        randomCustomTrack( );
        gotoMultiHostCustomTrackVehicle( );
        break;
    case a_multiHostStopServer:
        gotoMultiplayer( );
        break;
    case a_multiHostCircuitOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoMultiHostCircuitTrans( );
        break;
    case a_multiHostCircuitRandom:
        randomVehicle(false);
        gotoMultiHostCircuitTrans( );
        break;
    case a_multiHostAutomatic:
        m_game->nextAutomaticTransmission(true);
        m_game->state(Game::awaitingGame);
        shouldReturn = true;
        break;
    case a_multiHostManual:
        m_game->nextAutomaticTransmission(false);
        m_game->state(Game::awaitingGame);
        shouldReturn = true;
        break;
    case a_multiHostAdventureOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoMultiHostAdventureTrans( );
        break;
    case a_multiHostAdventureRandom:
        randomVehicle(false);
        gotoMultiHostAdventureTrans( );
        break;
    case a_multiHostCustomTrackOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoMultiHostCustomTrackTrans( );
        break;
    case a_multiHostCustomTrackRandom:
        randomVehicle(false);
        gotoMultiHostCustomTrackTrans( );
        break;
    case a_multiHostRandomOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoMultiHostRandomTrans( );
        break;
    case a_multiHostRandomRandom:
        randomVehicle(false);
        gotoMultiHostRandomTrans( );
        break;
    case a_multiJoinOfficialVehicle:
        m_game->nextVehicle(m_currentMenuItem);
        gotoMultiJoinTrans( );
        break;
    case a_multiJoinRandom:
        randomVehicle(false);
        gotoMultiJoinTrans( );
        break;
    case a_multiJoinDisconnect:
        gotoMultiplayer( );
        break;
    case a_multiJoinAutomatic:
        if (m_game->raceClient()->startRace( ))
        {
            m_acceptInput = false;
            if (m_soundAutomatic->playing( ))
            {
                m_soundAutomatic->stop( );
                m_soundAutomatic->reset( );
            }
            m_soundRaceInProgress->play( );
            // pushEvent(Event::playCurrentItem, m_soundRaceInProgress->length());
            pushEvent(Event::acceptInput, m_soundRaceInProgress->length());
        }
        else
        {
            m_game->nextAutomaticTransmission(true);
            m_game->state(Game::awaitingGame);
            shouldReturn = true;
        }
        break;
    case a_multiJoinManual:
        if (m_game->raceClient()->startRace( ))
        {
            m_acceptInput = false;
            if (m_soundManual->playing( ))
            {
                m_soundManual->stop( );
                m_soundManual->reset( );
            }
            m_soundRaceInProgress->play( );
            // pushEvent(Event::playCurrentItem, m_soundRaceInProgress->length());
            pushEvent(Event::acceptInput, m_soundRaceInProgress->length());
        }
        else
        {
            m_game->nextAutomaticTransmission(false);
            m_game->state(Game::awaitingGame);
            shouldReturn = true;
        }
        break;
    case a_optionsControls:
        gotoOptionsControls( );
        break;
    case a_optionsControlsDevice:
        gotoOptionsControlsDevice( );
        break;
    case a_optionsControlsFF:
        gotoOptionsForceFeedback( );
        break;
    case a_optionsControlsAssignKeyboard:
        startCalibrating(false);
        break;
    case a_optionsControlsAssignJoystick:
        startCalibrating(true);
        break;
    case a_optionsControlsDeviceKeyboard:
            stopCurrentMenuItem( );
        m_game->raceInput( )->setDevice(0);
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsControls( );
        break;
    case a_optionsControlsDeviceJoystick:
            stopCurrentMenuItem( );
        m_game->raceInput( )->setDevice(1);
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsControls( );
        break;
    case a_optionsControlsFFOn:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).forceFeedback = 1;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsControls( );
        break;
    case a_optionsControlsFFOff:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).forceFeedback = 0;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsControls( );
        break;
    case a_optionsGameSettings:
        gotoOptionsGameSettings( );
        break;
    case a_optionsRaceSettings:
        gotoOptionsRaceSettings( );
        break;
    case a_optionsGameLanguage:
        gotoOptionsLanguage( );
        break;
    case a_optionsGameRandomCustomTracks:
        gotoOptionsRandomCustomTracks( );
        break;
    case a_optionsGameRandomCustomVehicles:
        gotoOptionsRandomCustomVehicles( );
        break;
    case a_optionsRaceCopilot:
        gotoOptionsCopilot( );
        break;
    case a_optionsRaceCurves:
        gotoOptionsCurves( );
        break;
    case a_optionsRaceInfo:
        gotoOptionsRequestInfo( );
        break;
    case a_optionsRaceNrOfLaps:
        gotoOptionsNrOfLaps( );
        break;
    case a_optionsRaceNrOfComputers:
        gotoOptionsNrOfComputers( );
        break;
    case a_optionsRaceDifficulty:
        gotoOptionsDifficulty( );
        break;
/*
    case a_optionsRaceSingleCustomVehicles:
        gotoOptionsSingleCustomVehicles( );
        break;
*/
    case a_optionsGameThreeD:
        gotoOptionsThreeD( );
        break;
    case a_optionsGameHardwareAcceleration:
        gotoOptionsHardwareAcceleration( );
        break;
    case a_optionsGameReverseStereo:
        gotoOptionsReverseStereo( );
        break;
    case a_optionsLanguageChoose:
        stopCurrentMenuItem();
        strcpy(m_game->raceSettings( ).language, m_currentMenu[m_currentMenuItem].name);
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        m_game->language(m_game->raceSettings( ).language);
        finalize( );
        initialize(optionsGameSettings);
        m_game->resetTimer( );
//        gotoOptionsGameSettings( );
        break;
    case a_optionsRandomCustomTracksOn:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).randomCustomTracks = 1;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsGameSettings( );
        break;
    case a_optionsRandomCustomTracksOff:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).randomCustomTracks = 0;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsGameSettings( );
        break;
    case a_optionsRandomCustomVehiclesOn:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).randomCustomVehicles = 1;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsGameSettings( );
        break;
    case a_optionsRandomCustomVehiclesOff:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).randomCustomVehicles = 0;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsGameSettings( );
        break;
    case a_optionsCopilotOff:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).copilot = 0;
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsCopilotCurves:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).copilot = 1;
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsCopilotAll:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).copilot = 3;
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsCurveFixed:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).curveAnnouncement = 0;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsCurveSpeed:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).curveAnnouncement = 1;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsInfoOff:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).automaticInfo = 0;
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsInfoLapsOnly:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).automaticInfo = 1;
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsInfoOn:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).automaticInfo = 2;
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps1:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 1;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps2:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 2;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps3:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 3;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps4:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 4;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps5:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 5;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps6:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 6;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps7:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 7;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps8:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 8;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps9:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 9;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps10:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 10;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps11:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 11;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps12:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 12;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps13:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 13;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps14:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 14;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps15:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 15;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfLaps16:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfLaps = 16;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfComputers1:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfComputers = 1;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfComputers2:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfComputers = 2;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfComputers3:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfComputers = 3;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfComputers4:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfComputers = 4;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfComputers5:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfComputers = 5;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfComputers6:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfComputers = 6;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsNrOfComputers7:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).nrOfComputers = 7;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsDifficultyEasy:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).difficulty = 0;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsDifficultyNormal:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).difficulty = 1;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsDifficultyHard:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).difficulty = 2;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
/*
    case a_optionsSingleCustomVehiclesOn:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).singleRaceCustomVehicles = 1;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
    case a_optionsSingleCustomVehiclesOff:
            stopCurrentMenuItem( );
        m_game->raceSettings( ).singleRaceCustomVehicles = 0;
        m_game->raceSettings( ).write( );
                m_soundSaved->play( );
                ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
                m_game->resetTimer( );
        gotoOptionsRaceSettings( );
        break;
*/
    case a_optionsThreeDOn:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).threeD = 1;
        m_game->raceSettings( ).write( );
m_game->threeD(true);
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        finalize( );
        initialize(optionsGameSettings);
        m_game->resetTimer( );
//        gotoOptionsGameSettings( );
        break;
    case a_optionsThreeDOff:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).threeD = 0;
        m_game->raceSettings( ).write( );
m_game->threeD(false);
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        finalize( );
        initialize(optionsGameSettings);
        m_game->resetTimer( );
//        gotoOptionsGameSettings( );
        break;
    case a_optionsHardwareAccelerationOn:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).hardwareAcceleration = 1;
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
m_soundManager->playInSoftware(false);
        finalize( );
        initialize(optionsGameSettings);
        m_game->resetTimer( );
//        gotoOptionsGameSettings( );
        break;
    case a_optionsHardwareAccelerationOff:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).hardwareAcceleration = 0;
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        m_soundManager->playInSoftware(true);
        finalize( );
        initialize(optionsGameSettings);
        m_game->resetTimer( );
//        gotoOptionsGameSettings( );
        break;
    case a_optionsReverseStereoOn:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).reverseStereo = 1;
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        m_soundManager->reverseStereo(m_game->raceSettings( ).reverseStereo);
        finalize( );
        initialize(optionsGameSettings);
        m_game->resetTimer( );
//        gotoOptionsGameSettings( );
        break;
    case a_optionsReverseStereoOff:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).reverseStereo = 0;
        m_game->raceSettings( ).write( );
        m_soundSaved->play( );
        ::Sleep(DWORD(m_soundSaved->length( ) * 1000.0f));
        m_soundManager->reverseStereo(m_game->raceSettings( ).reverseStereo);
        finalize( );
        initialize(optionsGameSettings);
        m_game->resetTimer( );
//        gotoOptionsGameSettings( );
        break;
    case a_optionsRestoreDefaults:
        gotoOptionsRestore( );
        break;
    case a_optionsRestoreYes:
        stopCurrentMenuItem( );
        m_game->raceSettings( ).restoreDefaults( );
        m_game->raceSettings( ).write( );
        m_soundDefaultsRestored->play( );
        ::Sleep(DWORD(m_soundDefaultsRestored->length( ) * 1000.0f));
        m_game->language(m_game->raceSettings( ).language);
m_soundManager->playInSoftware(false);
        m_soundManager->reverseStereo((Boolean)m_game->raceSettings( ).reverseStereo);
        finalize( );
        m_game->raceInput( )->finalize( );
        m_game->raceInput( )->initialize( );
        initialize(options);
        m_game->resetTimer( );
//        gotoOptions( );
        break;
    default:
        break;
    }
    return shouldReturn;
}

void 
Menu::gotoTimeTrial( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrial;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_timeTrial);
    m_acceptInput = false;
    fadeTo(m_soundTheme3);
    m_soundPickTrackType->play();
    pushEvent(Event::playCurrentItem, m_soundPickTrackType->length( ));
    pushEvent(Event::acceptInput, m_soundPickTrackType->length( ));
}

void 
Menu::gotoSingleRace( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRace;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_singleRace);
    m_acceptInput = false;
    fadeTo(m_soundTheme3);
    m_soundPickTrackType->play();
    pushEvent(Event::playCurrentItem, m_soundPickTrackType->length( ));
    pushEvent(Event::acceptInput, m_soundPickTrackType->length( ));
}

void 
Menu::gotoTimeTrialCircuitVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialCircuitVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nVehicles*sizeof(Item);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}


void 
Menu::gotoSingleRaceCircuitVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceCircuitVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nVehicles*sizeof(Item);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}


void
Menu::gotoTimeTrialAdventureVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialAdventureVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nVehicles*sizeof(Item);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}
void
Menu::gotoSingleRaceAdventureVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceAdventureVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nVehicles*sizeof(Item);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}


void 
Menu::gotoTimeTrialCustomTrackVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialCustomTrackVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nVehicles*sizeof(Item);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}

void
Menu::gotoTimeTrialRandomVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialRandomVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nVehicles*sizeof(Item);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}


void
Menu::gotoTimeTrialRandomTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialRandomTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_timeTrialRandomTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}

void 
Menu::gotoSingleRaceCustomTrackVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceCustomTrackVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nVehicles*sizeof(Item);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}

void
Menu::gotoTimeTrialCircuitTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialCircuitTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_timeTrialCircuitTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}

void
Menu::gotoSingleRaceCircuitTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceCircuitTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_singleRaceCircuitTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}


void
Menu::gotoTimeTrialAdventureTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialAdventureTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_timeTrialAdventureTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}
void
Menu::gotoSingleRaceAdventureTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceAdventureTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_singleRaceAdventureTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}
void
Menu::gotoTimeTrialCustomTrackTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialCustomTrackTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_timeTrialCustomTrackTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}

void
Menu::gotoSingleRaceCustomTrackTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceCustomTrackTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_singleRaceCustomTrackTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}

void
Menu::gotoSingleRaceRandomVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceRandomVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nVehicles*sizeof(Item);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}

void
Menu::gotoSingleRaceRandomTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceRandomTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_singleRaceRandomTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}

void
Menu::gotoMainMenu()
{
    stopCurrentMenuItem();
    m_currentMenu = m_mainMenu;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_mainMenu);
    m_acceptInput = false;
    fadeTo(m_soundTheme1);
    m_soundMainMenu->play();
    pushEvent(Event::playCurrentItem, m_soundMainMenu->length( ));
    pushEvent(Event::acceptInput, m_soundMainMenu->length( ));
}

void 
Menu::gotoTimeTrialCircuitTrack( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialCircuitTrack;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_timeTrialCircuitTrack);
    m_acceptInput = false;
    m_soundPickCircuit->play();
    pushEvent(Event::playCurrentItem, m_soundPickCircuit->length( ));
    pushEvent(Event::acceptInput, m_soundPickCircuit->length( ));
}
void 
Menu::gotoSingleRaceCircuitTrack( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceCircuitTrack;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_singleRaceCircuitTrack);
    m_acceptInput = false;
    m_soundPickCircuit->play();
    pushEvent(Event::playCurrentItem, m_soundPickCircuit->length( ));
    pushEvent(Event::acceptInput, m_soundPickCircuit->length( ));
}
void
Menu::gotoTimeTrialCustomTrackTrack( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialCustomTrackTrack;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nCustomTracks*sizeof(Item);
    m_acceptInput = false;
    m_soundPickCircuit->play();
    pushEvent(Event::playCurrentItem, m_soundPickCircuit->length( ));
    pushEvent(Event::acceptInput, m_soundPickCircuit->length( ));    
}


void 
Menu::gotoSingleRaceCustomTrackTrack( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceCustomTrackTrack;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nCustomTracks*sizeof(Item);
    m_acceptInput = false;
    m_soundPickCircuit->play();
    pushEvent(Event::playCurrentItem, m_soundPickCircuit->length( ));
    pushEvent(Event::acceptInput, m_soundPickCircuit->length( ));    
}
    

void 
Menu::gotoTimeTrialAdventureTrack( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_timeTrialAdventureTrack;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_timeTrialAdventureTrack);
    m_acceptInput = false;
    m_soundPickAdventure->play();
    pushEvent(Event::playCurrentItem, m_soundPickAdventure->length( ));
    pushEvent(Event::acceptInput, m_soundPickAdventure->length( ));
}
void 
Menu::gotoSingleRaceAdventureTrack( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_singleRaceAdventureTrack;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_singleRaceAdventureTrack);
    m_acceptInput = false;
    m_soundPickAdventure->play();
    pushEvent(Event::playCurrentItem, m_soundPickAdventure->length( ));
    pushEvent(Event::acceptInput, m_soundPickAdventure->length( ));
}


void
Menu::gotoMultiplayer( )
{
    stopCurrentMenuItem();
    stopMultiplayer( );
    m_currentMenu = m_multiplayer;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiplayer);
    m_acceptInput = false;
    fadeTo(m_soundTheme2);
    m_soundChoose->play();
    pushEvent(Event::playCurrentItem, m_soundChoose->length( ));
    pushEvent(Event::acceptInput, m_soundChoose->length( ));
}
void 
Menu::gotoMultiHost( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHost;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHost);
    m_acceptInput = false;
    fadeTo(m_soundTheme3);
    m_soundPickTrackType->play();
    pushEvent(Event::playCurrentItem, m_soundPickTrackType->length( ));
    pushEvent(Event::acceptInput, m_soundPickTrackType->length( ));
}


void
Menu::gotoMultiHostCircuitTrack( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostCircuitTrack;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostCircuitTrack);
    m_acceptInput = false;
    m_soundPickCircuit->play();
    pushEvent(Event::playCurrentItem, m_soundPickCircuit->length( ));
    pushEvent(Event::acceptInput, m_soundPickCircuit->length( ));
}

void
Menu::gotoMultiHostCircuitVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostCircuitVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostCircuitVehicle);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}


void
Menu::gotoMultiHostCircuitTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostCircuitTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostCircuitTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}

void
Menu::gotoMultiHostAdventureTrack( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostAdventureTrack;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostAdventureTrack);
    m_acceptInput = false;
    m_soundPickAdventure->play();
    pushEvent(Event::playCurrentItem, m_soundPickAdventure->length( ));
    pushEvent(Event::acceptInput, m_soundPickAdventure->length( ));
}

void
Menu::gotoMultiHostAdventureVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostAdventureVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostAdventureVehicle);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}

void
Menu::gotoMultiHostAdventureTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostAdventureTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostAdventureTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}

void
Menu::gotoMultiHostCustomTrackTrack( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostCustomTrackTrack;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nCustomTracks*sizeof(Item);
    m_acceptInput = false;
    m_soundPickCircuit->play();
    pushEvent(Event::playCurrentItem, m_soundPickCircuit->length( ));
    pushEvent(Event::acceptInput, m_soundPickCircuit->length( ));    
}

void
Menu::gotoMultiHostOptions( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostOptions;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostOptions);
    m_acceptInput = false;
    fadeTo(m_soundTheme2);
    m_soundChoose->play();
    pushEvent(Event::playCurrentItem, m_soundChoose->length( ));
    pushEvent(Event::acceptInput, m_soundChoose->length( ));
}

void
Menu::gotoMultiHostOptionsNrOfLaps( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostOptionsNrOfLaps;
    m_currentMenuItem = m_game->raceSettings().nrOfLaps-1;
    m_currentMenuSize = sizeof(m_multiHostOptionsNrOfLaps);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}

void
Menu::gotoMultiHostCustomTrackVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostCustomTrackVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostCustomTrackVehicle);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}

void
Menu::gotoMultiHostCustomTrackTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostCustomTrackTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostCustomTrackTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}

void
Menu::gotoMultiHostRandomVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostRandomVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostRandomVehicle);
    m_acceptInput = false;
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}

void
Menu::gotoMultiHostRandomTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiHostRandomTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiHostRandomTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}

void
Menu::gotoMultiJoinVehicle( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiJoinVehicle;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiJoinVehicle);
    m_acceptInput = false;
    fadeTo(m_soundTheme3);
    m_soundPickVehicle->play();
    pushEvent(Event::playCurrentItem, m_soundPickVehicle->length( ));
    pushEvent(Event::acceptInput, m_soundPickVehicle->length( ));
}


void
Menu::gotoMultiJoinTrans( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_multiJoinTrans;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_multiJoinTrans);
    m_acceptInput = false;
    m_soundSelectTrans->play();
    pushEvent(Event::playCurrentItem, m_soundSelectTrans->length( ));
    pushEvent(Event::acceptInput, m_soundSelectTrans->length( ));
}


void
Menu::gotoMultiplayerListServers( )
{
    UInt nMenuItems = m_game->raceClient()->nSessions() + 1;
    m_listServers = new Item[nMenuItems];
    m_listServers[nMenuItems-1].sound   = m_soundBack;
    m_listServers[nMenuItems-1].action  = a_back;
    for (UInt i = 0; i < nMenuItems-1; ++i)
    {
        m_listServers[i].sound = m_soundServer;
        m_listServers[i].action = a_multiSelectServer;
        DirectX::Client::SessionInfo sessionInfo;
        if (m_game->raceClient()->session(i, sessionInfo) != dxSuccess)
        {
            RACE("(!) Menu::gotoMultiplayerListServers : illegal info on session %d", i);
            m_listServers[i].param = 0;
        }
        else
        {
            m_listServers[i].param = ::atoi(sessionInfo.sessionName);
        }
    }
    m_currentMenu = m_listServers;
    m_currentMenuItem = 0;
    m_currentMenuSize = nMenuItems*sizeof(Item);
    m_acceptInput = false;
    m_soundSelectServer->play();
    pushEvent(Event::playCurrentItem, m_soundSelectServer->length( ));
    pushEvent(Event::acceptInput, m_soundSelectServer->length( ));
}


void
Menu::gotoOptions( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_options;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_options);
    m_acceptInput = false;
    fadeTo(m_soundTheme2);
    m_soundChoose->play();
    pushEvent(Event::playCurrentItem, m_soundChoose->length( ));
    pushEvent(Event::acceptInput, m_soundChoose->length( ));
}


void
Menu::gotoOptionsGameSettings( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsGameSettings;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_optionsGameSettings);
    m_acceptInput = false;
    fadeTo(m_soundTheme2);
    m_soundChoose->play();
    pushEvent(Event::playCurrentItem, m_soundChoose->length( ));
    pushEvent(Event::acceptInput, m_soundChoose->length( ));
}

void
Menu::gotoOptionsRaceSettings( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsRaceSettings;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_optionsRaceSettings);
    m_acceptInput = false;
    m_soundChoose->play();
    pushEvent(Event::playCurrentItem, m_soundChoose->length( ));
    pushEvent(Event::acceptInput, m_soundChoose->length( ));
}

void
Menu::gotoOptionsControls( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsControls;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_optionsControls);
    m_acceptInput = false;
    m_soundChoose->play();
    pushEvent(Event::playCurrentItem, m_soundChoose->length( ));
    pushEvent(Event::acceptInput, m_soundChoose->length( ));
}


void
Menu::gotoOptionsControlsDevice( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsDevice;
    m_currentMenuItem = m_game->raceSettings().useJoystick;
    m_currentMenuSize = sizeof(m_optionsDevice);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}


void
Menu::gotoOptionsForceFeedback( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsForceFeedback;
    m_currentMenuItem = m_game->raceSettings().forceFeedback;
    m_currentMenuSize = sizeof(m_optionsForceFeedback);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}


void
Menu::gotoOptionsCopilot( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsCopilot;
    m_currentMenuItem = m_game->raceSettings().copilot;
    m_currentMenuSize = sizeof(m_optionsCopilot);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}


void
Menu::gotoOptionsCurves( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsCurves;
    m_currentMenuItem = m_game->raceSettings().curveAnnouncement;
    m_currentMenuSize = sizeof(m_optionsCurves);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}


void
Menu::gotoOptionsRequestInfo( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsRequestInfo;
    m_currentMenuItem = m_game->raceSettings().automaticInfo;
    m_currentMenuSize = sizeof(m_optionsRequestInfo);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}


void
Menu::gotoOptionsNrOfLaps( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsNrOfLaps;
    m_currentMenuItem = m_game->raceSettings().nrOfLaps-1;
    m_currentMenuSize = sizeof(m_optionsNrOfLaps);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}


void
Menu::gotoOptionsNrOfComputers( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsNrOfComputers;
    m_currentMenuItem = m_game->raceSettings().nrOfComputers-1;
    m_currentMenuSize = sizeof(m_optionsNrOfComputers);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}
void
Menu::gotoOptionsLanguage( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsLanguage;
    m_currentMenuItem = 0;
    m_currentMenuSize = m_nLanguages*sizeof(Item);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}

void
Menu::gotoOptionsRandomCustomTracks( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsRandomCustomTracks;
    m_currentMenuItem = m_game->raceSettings().randomCustomTracks;
    m_currentMenuSize = sizeof(m_optionsRandomCustomTracks);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}

void
Menu::gotoOptionsRandomCustomVehicles( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsRandomCustomVehicles;
    m_currentMenuItem = m_game->raceSettings().randomCustomVehicles;
    m_currentMenuSize = sizeof(m_optionsRandomCustomVehicles);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}

void
Menu::gotoOptionsDifficulty( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsDifficulty;
    m_currentMenuItem = m_game->raceSettings().difficulty;
    m_currentMenuSize = sizeof(m_optionsDifficulty);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}

void
Menu::gotoOptionsThreeD( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsThreeD;
    m_currentMenuItem = m_game->raceSettings().threeD;
    m_currentMenuSize = sizeof(m_optionsThreeD);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}

void
Menu::gotoOptionsHardwareAcceleration( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsHardwareAcceleration;
    m_currentMenuItem = m_game->raceSettings().hardwareAcceleration;
    m_currentMenuSize = sizeof(m_optionsHardwareAcceleration);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}
/*
void
Menu::gotoOptionsSingleCustomVehicles( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsSingleCustomVehicles;
    m_currentMenuItem = m_game->raceSettings().singleRaceCustomVehicles;
    m_currentMenuSize = sizeof(m_optionsSingleCustomVehicles);
    m_acceptInput = false;
    m_soundChoose->play();
    pushEvent(Event::playCurrentItem, m_soundChoose->length( ));
    pushEvent(Event::acceptInput, m_soundChoose->length( ));
}
*/
void 
Menu::gotoOptionsReverseStereo( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsReverseStereo;
    m_currentMenuItem = m_game->raceSettings().reverseStereo;
    m_currentMenuSize = sizeof(m_optionsReverseStereo);
    m_acceptInput = false;
    m_soundChangeOption->play();
    pushEvent(Event::playCurrentItem, m_soundChangeOption->length( ));
    pushEvent(Event::acceptInput, m_soundChangeOption->length( ));
}
void 
Menu::gotoOptionsRestore( )
{
    stopCurrentMenuItem();
    m_currentMenu = m_optionsRestore;
    m_currentMenuItem = 0;
    m_currentMenuSize = sizeof(m_optionsRestore);
    m_acceptInput = false;
    m_soundConfirm->play();
    pushEvent(Event::playCurrentItem, m_soundConfirm->length( ));
    pushEvent(Event::acceptInput, m_soundConfirm->length( ));
}

void
Menu::stopMultiplayer( )
{
    m_game->stopClient( );
    m_game->stopServer( );
}


void
Menu::calibrateJoystick(DirectX::Input::State& input)
{
    if (m_acceptInput)
    {
        if (input.keys[DIK_ESCAPE])
        {
            commitCalibrating( );
            stopCalibrating( );
            return;
        }
        JoystickAxisOrButton axisOrButton = foundAxisOrButtonTriggered(input);
        switch (m_calibratingStep)
        {
        case 0: // center
            if (input.b1)
            {
                m_joystickCenter = input;
                ++m_calibratingStep;
                m_soundSteerLeft->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundSteerLeft->length( ));
            }
            break;
        case 1: // left
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundSteerRight->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundSteerRight->length( ));
            }
            break;
        case 2: // right
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundThrottle->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundThrottle->length( ));
            }
            break;
        case 3: // throttle
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundBrake->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundBrake->length( ));
            }
            break;
        case 4: // brake
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundGearUp->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundGearUp->length( ));
            }
            break;
        case 5: // gearup
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundGearDown->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundGearDown->length( ));
            }
            break;
        case 6: // geardown
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundHorn->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundHorn->length( ));
            }
            break;
        case 7: // horn
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundRequestInfo->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundRequestInfo->length( ));
            }
            break;
        case 8: // request information
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundCurrentGear->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundCurrentGear->length( ));
            }
            break;
        case 9: // current gear
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundCurrentLapNr->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundCurrentLapNr->length( ));
            }
            break;
        case 10: // current lap nr
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundCurrentRacePerc->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundCurrentRacePerc->length( ));
            }
            break;
        case 11: // current race percentage
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundCurrentLapPerc->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundCurrentLapPerc->length( ));
            }
            break;
        case 12: // current lap percentage
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                ++m_calibratingStep;
                m_soundCurrentRaceTime->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundCurrentRaceTime->length( ));
            }
            break;
        case 13: // current race time
            if (axisOrButton != axisNone)
            {
                m_usedAxis[m_calibratingStep-1] = axisOrButton;
                commitCalibrating( );
                stopCalibrating( );
                // m_acceptInput = false;
                // pushEvent(Event::acceptInput, 1.0f);
            }
            break;
        default:
            break;
        }
    }
}


void
Menu::calibrateKeyboard(DirectX::Input::State& input)
{
    if (m_acceptInput)
    {
        if (input.keys[DIK_ESCAPE])
        {
//            commitCalibrating( );
            stopCalibrating( );
            return;
        }
        UByte key = foundKey(input);
        switch (m_calibratingStep)
        {
        case 1: // left
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundSteerRight->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundSteerRight->length( ));
            }
            break;
        case 2: // right
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundThrottle->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundThrottle->length( ));
            }
            break;
        case 3: // throttle
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundBrake->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundBrake->length( ));
            }
            break;
        case 4: // brake
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundGearUp->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundGearUp->length( ));
            }
            break;
        case 5: // gearup
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundGearDown->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundGearDown->length( ));
            }
            break;
        case 6: // geardown
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundHorn->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundHorn->length( ));
            }
            break;
        case 7: // horn
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundRequestInfo->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundRequestInfo->length( ));
            }
            break;
        case 8: // request information
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundCurrentGear->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundCurrentGear->length( ));
            }
            break;
        case 9: // current gear
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundCurrentLapNr->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundCurrentLapNr->length( ));
            }
            break;
        case 10: // current lap nr
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundCurrentRacePerc->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundCurrentRacePerc->length( ));
            }
            break;
        case 11: // current race percentage
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundCurrentLapPerc->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundCurrentLapPerc->length( ));
            }
            break;
        case 12: // current lap percentage
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                ++m_calibratingStep;
                m_soundCurrentRaceTime->play( );
                m_acceptInput = false;
                pushEvent(Event::acceptInput, m_soundCurrentRaceTime->length( ));
            }
            break;
        case 13: // current race time
            if (key)
            {
                m_usedKeys[m_calibratingStep-1] = key;
                commitCalibrating( );
                stopCalibrating( );
                // m_acceptInput = false;
                // pushEvent(Event::acceptInput, 1.0f);
            }
            break;
        default:
            break;
        }
    }
}


void
Menu::startCalibrating(Boolean joystick)
{
    stopCurrentMenuItem();
    RACE("Menu::startCalibrating");
    m_acceptInput = false;
    m_calibratingJoystick = joystick;
    m_calibratingKeyboard = !joystick;
    if (m_calibratingKeyboard)
        m_calibratingStep = 1; // skip the "center" part
    else
        m_calibratingStep = 0;
    for (UInt i = 0; i < 13; ++i)
    {
        m_usedAxis[i] = axisNone;
        m_usedKeys[i] = 0;
    }
    m_soundFollowInstructions = m_game->loadLanguageSound("menu\\followinstructions");
    if (m_calibratingJoystick)
        m_soundCenterJoystick = m_game->loadLanguageSound("menu\\centerjoystick");
    m_soundSteerLeft    = m_game->loadLanguageSound("menu\\steerleft");
    m_soundSteerRight   = m_game->loadLanguageSound("menu\\steerright");
    m_soundThrottle     = m_game->loadLanguageSound("menu\\throttle");
    m_soundBrake        = m_game->loadLanguageSound("menu\\brake");
    m_soundGearUp       = m_game->loadLanguageSound("menu\\shiftgearup");
    m_soundGearDown     = m_game->loadLanguageSound("menu\\shiftgeardown");
    m_soundHorn         = m_game->loadLanguageSound("menu\\usehorn");
    m_soundRequestInfo  = m_game->loadLanguageSound("menu\\requestinfo");
    m_soundCurrentGear  = m_game->loadLanguageSound("menu\\currentgear");
    m_soundCurrentLapNr  = m_game->loadLanguageSound("menu\\currentlapnr");
    m_soundCurrentRacePerc  = m_game->loadLanguageSound("menu\\currentracepercentage");
    m_soundCurrentLapPerc  = m_game->loadLanguageSound("menu\\currentlappercentage");
    m_soundCurrentRaceTime  = m_game->loadLanguageSound("menu\\currentracetime");
    m_soundFollowInstructions->play( );
    if (m_calibratingJoystick)
    {
        pushEvent(Event::playSound, m_soundFollowInstructions->length( ) + 0.5f, m_soundCenterJoystick);
        pushEvent(Event::acceptInput, m_soundFollowInstructions->length( ) + m_soundCenterJoystick->length( ) + 0.5f);
    }
    else 
    {
        pushEvent(Event::playSound, m_soundFollowInstructions->length( ) + 0.5f, m_soundSteerLeft);
        pushEvent(Event::acceptInput, m_soundFollowInstructions->length( ) + m_soundSteerLeft->length( ) + 0.5f);
    }
}

void
Menu::stopCalibrating( )
{
    RACE("Menu::stopCalibrating");
    m_calibratingStep = 0;
    SAFE_DELETE(m_soundFollowInstructions);
    if (m_calibratingJoystick)
        SAFE_DELETE(m_soundCenterJoystick);
    SAFE_DELETE(m_soundSteerLeft);
    SAFE_DELETE(m_soundSteerRight);
    SAFE_DELETE(m_soundThrottle);
    SAFE_DELETE(m_soundBrake);
    SAFE_DELETE(m_soundGearUp);
    SAFE_DELETE(m_soundGearDown);
    SAFE_DELETE(m_soundHorn);
    SAFE_DELETE(m_soundRequestInfo);
    SAFE_DELETE(m_soundCurrentGear);
    SAFE_DELETE(m_soundCurrentLapNr);
    SAFE_DELETE(m_soundCurrentRacePerc);
    SAFE_DELETE(m_soundCurrentLapPerc);
    SAFE_DELETE(m_soundCurrentRaceTime);
    m_calibratingJoystick = false;
    m_calibratingKeyboard = false;
    gotoOptionsControls( );
}

void
Menu::commitCalibrating( )
{
    RACE("Menu::commitCalibrating");
    if (m_calibratingJoystick)
    {
        if (m_calibratingStep != 0)
            m_game->raceInput( )->setCenter(m_joystickCenter);
        if (m_usedAxis[0] != axisNone)
            m_game->raceInput( )->setLeft(m_usedAxis[0]);
        if (m_usedAxis[1] != axisNone)
            m_game->raceInput( )->setRight(m_usedAxis[1]);
        if (m_usedAxis[2] != axisNone)
            m_game->raceInput( )->setThrottle(m_usedAxis[2]);
        if (m_usedAxis[3] != axisNone)
            m_game->raceInput( )->setBrake(m_usedAxis[3]);
        if (m_usedAxis[4] != axisNone)
            m_game->raceInput( )->setGearUp(m_usedAxis[4]);
        if (m_usedAxis[5] != axisNone)
            m_game->raceInput( )->setGearDown(m_usedAxis[5]);
        if (m_usedAxis[6] != axisNone)
            m_game->raceInput( )->setHorn(m_usedAxis[6]);
        if (m_usedAxis[7] != axisNone)
            m_game->raceInput( )->setRequestInfo(m_usedAxis[7]);
        if (m_usedAxis[8] != axisNone)
            m_game->raceInput( )->setCurrentGear(m_usedAxis[8]);
        if (m_usedAxis[9] != axisNone)
            m_game->raceInput( )->setCurrentLapNr(m_usedAxis[9]);
        if (m_usedAxis[10] != axisNone)
            m_game->raceInput( )->setCurrentRacePerc(m_usedAxis[10]);
        if (m_usedAxis[11] != axisNone)
            m_game->raceInput( )->setCurrentLapPerc(m_usedAxis[11]);
        if (m_usedAxis[12] != axisNone)
            m_game->raceInput( )->setCurrentRaceTime(m_usedAxis[12]);
    }
    if (m_calibratingKeyboard)
    {
        m_game->raceInput( )->setLeft(m_usedKeys[0]);
        m_game->raceInput( )->setRight(m_usedKeys[1]);
        m_game->raceInput( )->setThrottle(m_usedKeys[2]);
        m_game->raceInput( )->setBrake(m_usedKeys[3]);
        m_game->raceInput( )->setGearUp(m_usedKeys[4]);
        m_game->raceInput( )->setGearDown(m_usedKeys[5]);
        m_game->raceInput( )->setHorn(m_usedKeys[6]);
        m_game->raceInput( )->setRequestInfo(m_usedKeys[7]);
        m_game->raceInput( )->setCurrentGear(m_usedKeys[8]);
        m_game->raceInput( )->setCurrentLapNr(m_usedKeys[9]);
        m_game->raceInput( )->setCurrentRacePerc(m_usedKeys[10]);
        m_game->raceInput( )->setCurrentLapPerc(m_usedKeys[11]);
        m_game->raceInput( )->setCurrentRaceTime(m_usedKeys[12]);
    }
    m_game->raceSettings( ).write( );
}

Boolean
Menu::axisOrButtonTriggered(JoystickAxisOrButton a, DirectX::Input::State& input)
{
    DirectX::Input::State& center = m_joystickCenter; //m_game->raceInput()->getCenter( );
    switch (a)
    {
    case axisXneg:
        if (input.x < center.x - 50)
            return true;
        break;
    case axisXpos:
        if (input.x > center.x + 50)
            return true;
        break;
    case axisYneg:
        if (input.y < center.y - 50)
            return true;
        break;
    case axisYpos:
        if (input.y > center.y + 50)
            return true;
        break;
    case axisZneg:
        if (input.z < center.z - 50)
            return true;
        break;
    case axisZpos:
        if (input.z > center.z + 50)
            return true;
        break;
    case axisRXneg:
        if (input.rx < center.rx - 50)
            return true;
        break;
    case axisRXpos:
        if (input.rx > center.rx + 50)
            return true;
        break;
    case axisRYneg:
        if (input.ry < center.ry - 50)
            return true;
        break;
    case axisRYpos:
        if (input.ry > center.ry + 50)
            return true;
        break;
    case axisRZneg:
        if (input.rz < center.rz - 50)
            return true;
        break;
    case axisRZpos:
        if (input.rz > center.rz + 50)
            return true;
        break;
    case axisSlider1neg:
        if (input.slider1 < center.slider1 - 50)
            return true;
        break;
    case axisSlider1pos:
        if (input.slider1 > center.slider1 + 50)
            return true;
        break;
    case axisSlider2neg:
        if (input.slider2 < center.slider2 - 50)
            return true;
        break;
    case axisSlider2pos:
        if (input.slider2 > center.slider2 + 50)
            return true;
        break;
    case button1:
        if (input.b1)
            return true;
        break;
    case button2:
        if (input.b2)
            return true;
        break;
    case button3:
        if (input.b3)
            return true;
        break;
    case button4:
        if (input.b4)
            return true;
        break;
    case button5:
        if (input.b5)
            return true;
        break;
    case button6:
        if (input.b6)
            return true;
        break;
    case button7:
        if (input.b7)
            return true;
        break;
    case button8:
        if (input.b8)
            return true;
        break;
    case button9:
        if (input.b9)
            return true;
        break;
    case button10:
        if (input.b10)
            return true;
        break;
    case button11:
        if (input.b11)
            return true;
        break;
    case button12:
        if (input.b12)
            return true;
        break;
    case button13:
        if (input.b13)
            return true;
        break;
    case button14:
        if (input.b14)
            return true;
        break;
    case button15:
        if (input.b15)
            return true;
        break;
    case button16:
        if (input.b16)
            return true;
        break;
    case pov1:
        if (input.pov1)
            return true;
        break;
    case pov2:
        if (input.pov2)
            return true;
        break;
    case pov3:
        if (input.pov3)
            return true;
        break;
    case pov4:
        if (input.pov4)
            return true;
        break;
    case pov5:
        if (input.pov5)
            return true;
        break;
    case pov6:
        if (input.pov6)
            return true;
        break;
    case pov7:
        if (input.pov7)
            return true;
        break;
    case pov8:
        if (input.pov8)
            return true;
        break;
    default:
        break;
    }
    return false;
}


JoystickAxisOrButton
Menu::foundAxisOrButtonTriggered(DirectX::Input::State& input)
{
    JoystickAxisOrButton result = axisNone;
    for (Int a = (Int)axisXneg; a <= (Int)pov8; ++a)
    {
        Boolean found = false;
        if (axisOrButtonTriggered((JoystickAxisOrButton) a, input))
        {
            found = true;
            for (UInt i = 0; i < m_calibratingStep; ++i)
            {
                if (a == m_usedAxis[i])
                    found = false;
            }
        }
        if (found)
        {
            result = (JoystickAxisOrButton) a;
            break;
        }
    }
    return result;
}


UByte
Menu::foundKey(DirectX::Input::State& input)
{
    UByte result = 0;
    for (UByte a = 1; a < 0xFF; ++a)
    {
        Boolean found = false;
        if (input.keys[a])
        {
            found = true;
            for (UInt i = 0; i < m_calibratingStep; ++i)
            {
                if ((a == m_usedKeys[i]) || ((a >= DIK_F1) && (a <= DIK_F10)) || ((a >= DIK_F11) && (a <= DIK_F12)) || ((a >= DIK_1) && (a <= DIK_8)) || (a == DIK_LMENU))
                    found = false;
            }
        }
        if (found)
        {
            result = a;
            break;
        }
    }
    return result;
}


void
Menu::randomTrack( )
{
    UInt randomTracksNumber = 0;
    if (m_game->raceSettings().randomCustomTracks == 1)
        randomTracksNumber = random(NTRACKS+m_nCustomTracks-2);
    else
        randomTracksNumber = random(NTRACKS);
    if (randomTracksNumber < NCIRCUITS)
        m_game->nextTrack(m_timeTrialCircuitTrack[randomTracksNumber].name);
    else if (randomTracksNumber < NTRACKS)
        m_game->nextTrack(m_timeTrialAdventureTrack[randomTracksNumber-NCIRCUITS].name);
    else
        m_game->nextTrack(m_customTrackFiles[randomTracksNumber-NTRACKS]);
}

void
Menu::randomCircuit( )
{
    m_game->nextTrack(m_timeTrialCircuitTrack[random(NCIRCUITS)].name);
}

void
Menu::randomAdventure( )
{
    m_game->nextTrack(m_timeTrialAdventureTrack[random(NADVENTURES)].name);
}

void
Menu::randomCustomTrack( )
{
    m_game->nextTrack(m_customTrackFiles[random(m_nCustomTracks-2)]);
}

void
Menu::randomVehicle(Boolean randomCustomVehicles)
{
    if (randomCustomVehicles)
    {
        UInt randomVehiclesNumber = random(m_nVehicles-2);
        if (randomVehiclesNumber < NVEHICLES)
            m_game->nextVehicle(random(NVEHICLES));
        else
            m_game->nextVehicle(0, m_vehicleFiles[randomVehiclesNumber-NVEHICLES]);
    }
    else
    {
        m_game->nextVehicle(random(NVEHICLES));
    }
}

void
Menu::sayHighScores(Char* trackName)
{
    m_sayTimeLength = 0.0f;
    const Char highscoreFile[] = "highscore.cfg";
    UInt nrOfHighScores = 0;
    File file(highscoreFile, File::read);
    if (file.opened( ))
    {
        for (UInt i = 1; i <= 16; ++i)
        {
            Char trackLaps[64];
            sprintf(trackLaps, "%s;%d", trackName, i);
            Int val;
            file.readKeyInt(trackLaps, val, 0);
            if (val > 0)
            {
                sayTime(i, val);
                ++nrOfHighScores;
            }
        }
        if (nrOfHighScores == 0)
        {
            m_soundNoHighScores->play( );
            pushEvent(Event::acceptInput, m_soundNoHighScores->length( ));
        }
        else
        {
            pushEvent(Event::acceptInput, m_sayTimeLength);
        }
    }
    else
    {
        m_soundNoHighScores->play( );
        pushEvent(Event::acceptInput, m_soundNoHighScores->length( ));
    }
}

void
Menu::sayTime(Int nrOfLaps, Int raceTime)
{
    RACE("Menu::sayTime : racetime = %d", raceTime);
    // Get time
        pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[nrOfLaps]);
        m_sayTimeLength += m_game->m_soundNumbers[nrOfLaps]->length();
        pushEvent(Event::playSound, m_sayTimeLength, m_soundLaps);
        m_sayTimeLength += m_soundLaps->length();
    UInt nminutes = raceTime / 60000;
    UInt nseconds = (raceTime % 60000) / 1000;

    if (nminutes != 0)
    {
        pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[nminutes]);
        m_sayTimeLength += m_game->m_soundNumbers[nminutes]->length();
        if (nminutes == 1)
        {
            pushEvent(Event::playSound, m_sayTimeLength, m_soundMinute);
            m_sayTimeLength += m_soundMinute->length();
        }
        else
        {
            pushEvent(Event::playSound, m_sayTimeLength, m_soundMinutes);
            m_sayTimeLength += m_soundMinutes->length();
        }
    }
    pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[nseconds]);
    m_sayTimeLength += m_game->m_soundNumbers[nseconds]->length();
        UInt ntens = (((raceTime % 60000) / 100) % 10);
        UInt nhundreds = (((raceTime % 60000) / 10) % 10);
        UInt nthousands = ((raceTime % 60000) % 10);
        pushEvent(Event::playSound, m_sayTimeLength, m_soundPoint);
        m_sayTimeLength += m_soundPoint->length();
        pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[ntens]);
        m_sayTimeLength += m_game->m_soundNumbers[ntens]->length();
        pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[nhundreds]);
        m_sayTimeLength += m_game->m_soundNumbers[nhundreds]->length();
        pushEvent(Event::playSound, m_sayTimeLength, m_game->m_soundNumbers[nthousands]);
        m_sayTimeLength += m_game->m_soundNumbers[nthousands]->length();
    if (nseconds == 1)
    {
        pushEvent(Event::playSound, m_sayTimeLength, m_soundSecond);
        m_sayTimeLength += m_soundSecond->length();
    }
    else
    {
        pushEvent(Event::playSound, m_sayTimeLength, m_soundSeconds);
        m_sayTimeLength += m_soundSeconds->length();
    }
}

void
Menu::fadeTo(DirectX::Sound* target)
{
    if (target == m_activeTheme)
        return;
    Int sourceVolume = 100;
    Int targetVolume = 85;
    target->volume(targetVolume);
    for (UInt i = 0; i < 15; ++i)
    {
        sourceVolume -= 2;
        targetVolume += 2;
        m_activeTheme->volume(sourceVolume);
        target->volume(targetVolume);
        ::Sleep(15);
        m_game->resetTimer( );
    }
    m_activeTheme->volume(0);
    m_activeTheme = target;
}

void
Menu::fadeOut()
{
    Int volume = 100;
    for (UInt i = 0; i < 40; ++i)
    {
        volume -= 1;
        m_activeTheme->volume(volume);
        ::Sleep(20);
        m_game->resetTimer( );
    }
}

void
Menu::fadeIn()
{
    Int volume = 80;
    for (UInt i = 0; i < 20; ++i)
    {
        volume += 1;
        m_activeTheme->volume(volume);
        ::Sleep(50);
        m_game->resetTimer( );
    }
}
