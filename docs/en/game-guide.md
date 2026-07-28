# Top Speed Remake: Official Player and Server Guide

## Table of Contents
- [1. Welcome](#1-welcome)
- [2. About This Release](#2-about-this-release)
- [3. License and Third-Party Components](#3-license-and-third-party-components)
- [4. Supported Platforms](#4-supported-platforms)
    - [4.1 Core Game Components](#41-core-game-components)
- [5. Install, Update, and Remove](#5-install-update-and-remove)
- [6. Files, Folders, and What Must Stay Together](#6-files-folders-and-what-must-stay-together)
- [7. First Launch and Setup Wizard](#7-first-launch-and-setup-wizard)
- [8. Menu Navigation and Screen Reader Behavior](#8-menu-navigation-and-screen-reader-behavior)
    - [8.1 Sliders](#81-sliders)
    - [8.2 Switches, check boxes, and radio buttons](#82-switches-check-boxes-and-radio-buttons)
    - [8.3 Actions](#83-actions)
- [9. Main Menu and Game Modes](#9-main-menu-and-game-modes)
- [10. Driving Fundamentals](#10-driving-fundamentals)
    - [10.1 Road position and car control](#101-road-position-and-car-control)
    - [10.2 Automatic, manual, and engine behavior](#102-automatic-manual-and-engine-behavior)
    - [10.3 Curves, surfaces, and co-pilot timing](#103-curves-surfaces-and-co-pilot-timing)
    - [10.4 Practical driving method and recovery](#104-practical-driving-method-and-recovery)
    - [10.5 Holding the phone on Android](#105-holding-the-phone-on-android)
    - [10.6 Fuel, Tires, and Pit Stops](#106-fuel-tires-and-pit-stops)
- [11. Full Settings Guide](#11-full-settings-guide)
    - [11.1 Game Settings](#111-game-settings)
    - [11.2 Speech](#112-speech)
    - [11.3 Audio](#113-audio)
    - [11.4 Volume Settings](#114-volume-settings)
    - [11.5 Controls](#115-controls)
    - [11.6 Race Settings](#116-race-settings)
    - [11.7 Server Settings (Client-Side Multiplayer Defaults)](#117-server-settings-client-side-multiplayer-defaults)
    - [11.8 Defaults Summary](#118-defaults-summary)
- [12. Multiplayer Client Guide](#12-multiplayer-client-guide)
- [13. Communicator (Multiplayer Voice and Streaming)](#13-communicator-multiplayer-voice-and-streaming)
- [14. Dedicated Server Guide](#14-dedicated-server-guide)
- [15. Panels and Vehicle Radio](#15-panels-and-vehicle-radio)
- [16. Custom Tracks and Custom Vehicles](#16-custom-tracks-and-custom-vehicles)
- [17. Updater Behavior on Desktop and Android](#17-updater-behavior-on-desktop-and-android)
- [18. Detailed Driving Tutorials](#18-detailed-driving-tutorials)
- [19. Multiplayer Setup Walkthroughs](#19-multiplayer-setup-walkthroughs)
- [20. Settings Tuning Profiles](#20-settings-tuning-profiles)
- [21. Additional Notes for Testers and Release Preparation](#21-additional-notes-for-testers-and-release-preparation)
- [22. Troubleshooting](#22-troubleshooting)
- [23. Frequently Asked Questions](#23-frequently-asked-questions)
- [24. Control Reference](#24-control-reference)
    - [24.1 Menu Navigation](#241-menu-navigation)
    - [24.2 Main Menu and Startup](#242-main-menu-and-startup)
    - [24.3 Driving Core Intents](#243-driving-core-intents)
    - [24.4 Driving Information Requests](#244-driving-information-requests)
    - [24.5 Direct Player Keys During Race](#245-direct-player-keys-during-race)
    - [24.6 Panel and Radio Controls](#246-panel-and-radio-controls)
    - [24.7 Multiplayer Menu Touch Layout (Android)](#247-multiplayer-menu-touch-layout-android)
    - [24.8 Multiplayer Race Overlay Controls](#248-multiplayer-race-overlay-controls)
    - [24.9 Communicator Controls](#249-communicator-controls)
- [25. Credits and Acknowledgements](#25-credits-and-acknowledgements)
- [26. Contact me](#26-contact-me)

## 1. Welcome
Welcome. I am Diamond Star, the one behind this project, which took months of development and refinement, either by me or by AI, to revive this game and hopefully make it better.

Top Speed Remake is a rewrite of the legacy game Top Speed. It is an audio racing game with various modes and options.

This project would not have existed without contributions, testers, translators, and people who have donated to keep it going.

Please read this carefully. The project has grown significantly, and managing it takes me a lot of time and effort. If you find it useful, whether as a coder or as a gamer, and if you are able to, please consider [donating to me](https://paypal.me/diamondStar35). I would greatly appreciate it, and it helps me a lot to continue working on it.

I would like to thank anyone who contributed to the project. Although I get small contributions, please be patient with me when requesting new features or bug fixes, as this project is entirely managed by me, with small contributions from the community.

This guide is written for players and server hosts. It explains installation, first launch, settings, race modes, multiplayer, touch controls, and common troubleshooting.

If you are completely new, read sections 7 through 11 first, then use section 24 as your control reference while playing.

## 2. About This Release
Top Speed started as an older open-source C++ project from the 2008-2013 period. The current game is a full rewrite in C#.

All materials used in this project were taken directly from the legacy game which is open-source on GitHub [here](https://github.com/PlayingintheDark/TopSpeed).

## 3. License and Third-Party Components
Top Speed Remake project code is distributed under GNU GPL v3.

The build includes third-party components with their own licenses. If you redistribute the game, keep those third-party notices with your package.

Main third-party license groups used here include SoundFlow notices, SteamAudio.NET (MIT), Steam Audio native license text (Apache 2.0), and other bundled dependency licenses distributed with the project.

## 4. Supported Platforms
Top Speed is available on various desktop operating systems and mobile. Mobile support is currently limited to Android, and hopefully iOS will come in the future.

Desktop builds are available for Windows, Linux, and macOS. Android support targets ARM64 devices first, with a separate ARM32 (ARMv7) build for older 32-bit phones, and requires Android 11 or newer either way.

Please do not open new issues to request for supporting older Android versions as this is not possible due to the requirements of some libraries such as Prism, which requires the minimum API to be 30 (Android 11).

Dedicated server is a separate program from the game client. You can play the game and use all online features without running a local server if you join an existing server hosted by anyone. Please note that the game does not have an official server, but some community members have up-to-date public servers. The list of these is included in section 12.

### 4.1 Core Game Components
The game experience is built from a few main systems that work together.

Input handles keyboard, controller, touch gestures, and motion steering options on Android.

Audio handles menu sounds, race sounds, and positional cues that help orientation.

Speech handles screen-reader and TTS output used by menus, hints, and race announcements.

Networking handles server connection, multiplayer race synchronization, and other online features.

Localization handles translated menu text and spoken strings.

### 4.2. System Requirements
#### 4.2.1. Desktop Requirements
On Windows, the game runs on 64bit versions only.

If you are having issues or getting errors when trying to open the game, please download the [Visual C++ Redistributable bundle](https://blindhelp.net/software/vc-dx). This link will open a third-party website where you can download it from there.

On Mac OS, the game works on Mac OS 12.1 or later.

On Linux, for the speech to work correctly you need to have Orca version 49 or later.

#### 4.2.2. Mobile Requirements
On Android, the game works on Android 11 or later. Both 64-bit and 32-bit (ARMv7) builds are published. A reasonably fast processor is recommended so HRTF audio plays without stuttering, especially on entry-level 32-bit devices.

## 5. Install, Update, and Remove
### Install on Desktop
Desktop builds are portable. Extract the package into one folder and keep all bundled folders beside the executable.

Do not move only the executable into another location. The game expects its content folders to stay together. If you need a quick way of accessing the game, you may wish to create a shortcut to the executable and placing it on the desktop (for example) on Windows.

On macOS, the game may not run normally immediately after download because macOS can mark downloaded apps with quarantine attributes. After copying the `.app` file into the Applications folder, select the `.app` file in Finder and press Command+C to copy it. Open Terminal, type `xattr -c `, then press Command+V to paste the application path after the command. Press Enter to run it. After this, launch the game from Applications again.

### Install on Android
Install the APK through Android package installer.

If installation is blocked, allow install permission for the installer app that is currently performing the install, then retry.

First launch on Android may take longer than later launches because initial content preparation is done on first run.

### Built-in Updating
Top Speed Remake supports manual update checks from the main menu and optional automatic checks on startup.

Desktop update packages are zip files and are installed by the updater program.

Android update packages are APK files and are installed through Android package installation.

### Removing the game
On desktop, delete the game folder.

On Android, uninstall from system app settings or launcher app menu.

## 6. Files, Folders, and What Must Stay Together
Top Speed Remake requires its content folders to be present. If required folders are missing, parts of the game will not load or will load with missing content.

Important folders include `Sounds`, `Tracks`, `Vehicles`, `languages`.

On desktop, `settings.json` is saved beside the executable. On Android, equivalent data is managed in app storage during startup.

When adding custom tracks, custom vehicles, or language files, keep folder structure exactly correct. Incorrect placement is the most common reason custom content does not appear in menus.

## 7. First Launch and Setup Wizard
When you launch Top Speed Remake for the first time, the game checks whether a settings file already exists. If this is a fresh install, setup wizard opens automatically before the regular main menu. This is expected behavior and not an error.

If logo playback is enabled, the logo runs first. You can skip it with Enter on desktop or swipe up on mobile.

### What setup wizard does
Setup wizard prepares the game for real play by asking for language and then calibrating speech timing. If these two steps are skipped, the game can still run, but menu hints and timing can feel wrong.

### Step 1: Language selection
After the welcome screen, you move to language selection. Pick the language you want for menu text and spoken game strings. Confirm the selection, and the game saves it immediately.

If you later add a new translation file, you can return to settings and switch again. If some text still appears in English after switching, restart once so all menus reload from the selected language file.

### Step 2: Speech calibration
Calibration measures how fast your screen reader currently talks on your device and backend. The game plays a long sample sentence, and you confirm after the screen reader finishes it.

In simple terms, calibration is a timing check. It does not change your voice quality, and it does not change race physics. It only helps the game schedule delayed help text and spoken hints at the right moment for your current reading speed.

The only reason calibration exists is that there is no reliable method that works with all screen-readers and backends to query if a screen-reader is still speaking, so the way it works, is that the game runs a time-watch as soon as you hear the sample. It then divides the time by how many words that sample is to get an approximation of how long it takes for your screen-reader to speak a word. Please keep this in mind because some usage hints or delayed descriptions may not always be exactly right, but it will be very close if not right.

Without proper calibration, delayed hints can fire too soon and interrupt what you are already listening to, or too late after you have already moved to another item. A correct calibration gives the game a reliable timing value so hints arrive when they are still useful.

### When to calibrate again
Run calibration again when one of these changes happens: you switch speech backend, you change speech rate a lot, you move to another device, or hint timing starts feeling early/late. Recalibration is available in speech settings at any time.

## 8. Menu Navigation and Screen Reader Behavior
Top Speed Remake menus are designed so the same logic works across desktop and Android, even though the physical input is different. Every menu has a focused item, and all navigation actions move or edit that focus in predictable ways.

An item can be any type of controls, not necessarily a single item that you normally activate. As of now the common controls you'll find are radio buttons, sliders, switches, and normal items.

To activate a normal item, use the Enter key on your keyboard, or swipe up with one finger on mobile.

For other controls, you'll hear the type of control when you focus on it, for example, in the options menu.

Usage hints are delayed help messages for the current item. If hints are enabled, the game waits briefly and then reads the help text for that item. You can also ask for the hint explicitly at any time: Space on desktop, long press on mobile.

Wrap navigation controls what happens at list edges. If wrapping is enabled and you move forward at the last item, focus jumps to the first item. If wrapping is disabled, focus stays on the last item until you move in the opposite direction.

Some menus contain multiple screens inside one menu context. On desktop these are switched with Tab and Shift+Tab. If you hear instructions that differ between desktop and mobile, that is intentional so each platform receives the right guidance for its active input method.

### 8.1 Sliders
Sliders are a way to adjust numeric values quickly. For example, all volume settings in the options menu are sliders. They work similar to a normal slider.

To increase the value by one step, press the right arrow on your keyboard, or swipe right with 2 fingers on mobile.

To decrease the value by one step, press the left arrow on your keyboard, or swipe left with 2 fingers on mobile.

To increase the value by 10 steps, press the Page Up key on your keyboard, or swipe up with 2 fingers on mobile.

To decrease the value by 10 steps, press the Page Down key on your keyboard, or swipe down with 2 fingers on mobile.

To quickly go to the minimum value while focused on a slider, press the End key on your keyboard, or swipe down with 3 fingers on mobile.

To go to the max value, press the Home key on your keyboard, or swipe up with 3 fingers on mobile.

### 8.2 Switches, check boxes, and radio buttons
A switch is a normal item with 2 states or options. Either on/off, or a custom value. It is suitable for 2 values or states such as on or off. To change the state, simply activate the item with normal item activation action.

A check box is similar, except that it does not have custom states. Instead, it is either checked or unchecked. It also uses the normal item activation action to check or uncheck the item.

A radio button contains more than 2 options. It is similar to a slider except that it can be used even for non-numeric values.

Controlling the radio button is done by using the left/right arrows on your keyboard to switch between different values, or swiping left or right with 2 fingers on mobile.

### 8.3 Actions
An item can have multiple actions, not just a normal activate. This is similar to actions on iOS where you focus on an item and it tells you that actions are available for that item.

You'll only see that in the server management menu, but it may be used in other places.

To know if an item has multiple actions, the screen-reader will announce something like "Actions available, press right arrow to view". This instruction is different on mobile, but it is similar. To navigate between actions, use the right/left arrows, and enter to activate on keyboard. On mobile, swipe left or right with 2 fingers, and swipe up with one finger to activate the currently focused action.

## 9. Main Menu and Game Modes
The main menu is the starting point for everything in Top Speed. From here, you can start races, configure settings, join multiplayer, check updates, view version information, or exit.

Quick start is the fastest way to test gameplay and audio. It builds a race quickly with random selections, so it is ideal for short sessions or after changing input/audio settings when you want a quick confirmation run.

Time trial is the best mode for learning a vehicle and track without traffic pressure. You race alone, which makes it easier to focus on braking points, shift timing, and consistent laps. If you are learning manual transmission, this is usually the safest place to build muscle memory.

Single race adds computer opponents and race pressure while keeping setup local. Use this mode when you want to practice overtaking, race rhythm, and decision-making before joining multiplayer.

Multiplayer game opens online play: server connection, lobby, room management, preparation, and race start. Multiplayer behavior is explained later in this guide, but every online session starts from this main menu item.

Options opens all game settings. Check for updates starts manual update check immediately. About displays version and build information useful for support reports. Exit Game closes the game cleanly.

## 10. Driving Fundamentals
Top Speed is an audio racing game. You drive by listening to position, speed, surface sound, and co-pilot calls. The goal is not to force maximum speed all the time. The goal is to keep control, avoid big mistakes, and finish clean laps.

### 10.1 Road position and car control
Your engine sound is your lane reference. When the car is centered, the engine feels centered. If the car drifts left or right, that balance changes. Small corrections made early are much safer than large corrections made late.

If you leave the road, you lose time. In races with opponents, contact with other cars can also cost time and direction control. Most crashes come from entering turns too fast or from correcting too hard during a turn.

A simple rule for beginners is this: keep steering smooth, keep speed controlled before turns, and avoid panic inputs after mistakes.

### 10.2 Automatic, manual, and engine behavior
Automatic transmission is the easiest way to learn tracks because you can focus on steering and braking. Use time trial first until you can finish consistent laps without heavy crashes.

Manual transmission gives more control but requires rhythm. You need clutch and gear timing that matches your speed and turn entry. If manual feels chaotic, lower your pace and practice one track repeatedly instead of jumping between tracks.

Engine start and stop use one action. In normal situations it starts or stops the engine. After a bad event such as a stall or crash state, the same action is also part of your recovery sequence. Learn this action early so recovery becomes automatic under pressure.

### 10.3 Curves, surfaces, and co-pilot timing
Tracks contain different curve strengths and different surfaces. A turn that feels easy on asphalt can become difficult on gravel, sand, snow, or water. When grip is lower, reduce speed earlier and avoid aggressive steering.

Co-pilot settings control how much information is announced and how early it is announced. If calls feel late for your driving pace, increase lead time. If calls come too early and become noisy, reduce it. Tune this in time trial until the call timing matches your braking rhythm.

A practical cornering method is: brake before the turn, release heavy braking while rotating through the turn, then accelerate smoothly on exit. Entering too fast is the most common cause of repeated mistakes.

### 10.4 Practical driving method and recovery
For stable laps, use a repeatable routine on every major turn. Prepare speed early, place the car, rotate smoothly, then apply throttle progressively. This keeps the car predictable and reduces sudden drift.

When a mistake happens, recover in order. First stabilize direction. Second restart/recover engine state if needed. Third rebuild speed gradually. Trying to regain all lost time in one move usually creates a second mistake.

If your long-term goal is multiplayer performance, train in time trial until your laps are consistent. Consistency usually beats occasional fast laps with frequent crashes.

### 10.5 Holding the phone on Android
Android driving controls are designed for landscape mode. Hold the phone sideways, with the long edge running left to right, the same way you would normally hold a phone for a racing game. Do not think about these controls as if the phone were still in portrait mode.

In this landscape position, the vehicle control area is on the right side of the screen. This is where you control throttle, brake, engine start, gear changes, and gesture steering if motion steering is disabled. The information area is on the left side of the screen. That side is used for horn, clutch, race information, player information, pause, and backing out of a multiplayer race.

All driving gestures in the control reference are written from this landscape point of view. If the guide says drag left or drag right, it means physically moving your finger toward the left or right side while holding the phone sideways. This matters especially for steering: steering left is a left drag, and steering right is a right drag. It is not based on portrait up and down directions.

### 10.6 Fuel, Tires, and Pit Stops
Top Speed can model fuel use and tire wear during a race. Both are optional and independent of each other. For single race and time trial, turn them on under Options, then Race settings, using the "Enable fuel consumption" and "Enable tire wear" check boxes. In multiplayer the host controls them through the room's race rules, so whether they apply depends on the server and room you join. When both are off, races behave exactly as they did before, with no fuel limit and no tire wear.

Fuel: When fuel consumption is on, your car burns fuel as you drive and starts each race with a full tank. Press X during driving to hear how much fuel you have left. As the tank gets low the game gives you a low-fuel warning so the reading does not take you by surprise. If you let the tank run completely dry the car can no longer keep going, and you will not be able to finish the race, so plan a refuel before it reaches that point.

Tires: When tire wear is on, your tires begin cold, warm up to an optimal temperature where they grip best, and then gradually wear and lose grip over a long run. Press B during driving to hear their current condition. The spoken states are cold, warming up, optimal temperature, hot, and overheated. Cold and overheated tires give you less grip than tires at their optimal temperature, so the readout is a useful guide for how hard you can push and when a fresh set is worth a stop.

Making a pit stop: Tracks have a pit area by default, located at the start/finish line, though a track author can place it elsewhere or turn pitting off entirely in the track's settings. Nearly all tracks therefore let you pit. To make a stop, press I (Pit this time) while driving. The game confirms with "Pitting this time" and remembers your request for the next time you reach the pit entry area on the current lap. When you arrive, a short menu opens with three choices: Refuel, Tires, and Both. Pick the service you need. As a shortcut, while that menu is open you can press 1 for Refuel, 2 for Tires, or 3 for Both to choose quickly. The car then services in the pit lane and rejoins the race. Refueling fills the tank, changing tires restores grip, and Both does the two together, which takes a little longer than either service alone. If you press I on a track that has no pit area, the game tells you so and no stop is made. Depending on where a track places its pit exit, you may rejoin the track already on your next lap. That is normal and set by the track's design, not a mistake on your part.

Strategy: With fuel and tires enabled, a race becomes a balance of pace against time spent stopped. A single stop for Both is the simple, safe approach. If you only need one service, refueling or changing tires alone saves time. On short races you may not need to stop at all. Use the X and B readouts to judge when a stop is worth the time it costs.

## 11. Full Settings Guide
This section explains what each settings group changes in normal play. If something feels wrong, change one option, test it, and keep notes. Multiple changes at once make problems harder to trace.

### 11.1 Game Settings
These are the core behavior settings you will use most often.

Language changes menu text and spoken interface text. If you add a new translation file while the game is already installed, switch language and restart once so all menus reload from the selected language.

Include custom tracks in randomization lets random race setup pick custom tracks. If disabled, random track selection stays on built-in tracks only.

Include custom vehicles in randomization does the same for vehicles. Keep it off if you want predictable stock content. Turn it on when you actively use custom vehicles.

Units switches speed and distance announcements between metric and imperial.

Enable usage hints controls delayed help text in menus. If hints feel too early or too late, recalibrate speech timing in the speech settings section.

Automatically focus first menu item decides where focus lands when a menu opens. If enabled, the first item is focused immediately. If disabled, you stay on the title and move manually.

Enable menu wrapping controls list edges. With wrapping on, moving past the last item jumps to the first item, and the opposite. With wrapping off, focus stays at the edge.

Menu sounds chooses the sound style used while moving in menus.

Enable menu navigation panning adds left-right panning to menu movement sounds, which can help orientation for some players.

Play logo at startup enables or disables logo playback on launch.

Check for updates on startup enables automatic update checks after launch.

### 11.2 Speech
Speech settings control how game messages are delivered.

Speech backend chooses which speech engine to use. Automatic asks the game to pick a suitable available engine.

Interrupt screen reader speech allows new game messages to interrupt current speech output. Please note: this is not guaranteed to work across all screen-readers and TTS engines, especially on Android.

Speech mode chooses speech only, braille only, or speech and braille together.

Voice appears only when the active speech engine exposes voice selection. It allows you to choose a specific voice if the speech engine supports it.

Speech rate appears when the active speech engine supports rate control. It controls how fast your TTS is speaking. For screen-readers such as NVDA or JAWS, this will never show up in the settings.

Recalibrate screen reader rate runs calibration again so delayed hints match your current speech speed.

### 11.3 Audio
Audio settings affect positional clarity and overall sound feel.

Enable HRTF audio improves 3D positional cues for many players. Keep it enabled unless your device or headphones make it sound worse. HRTF gives a much stronger sense of where each sound is coming from, but some older Android devices cannot process it without crackling; if that happens on your device, turn it off. Restart the game after changing this option for it to take effect.

Stereo widening for own car: This is an accessibility option for people who struggle to differentiate between left and right. It makes your own car easier to localize for some listeners.

Automatic audio device format lets the game use the device's native format automatically. Restart after changing it to take effect.

Voice input device chooses which microphone the communicator uses to capture your voice in multiplayer. The default is your system's default capture device. Changing it while the communicator is off applies the next time you turn the communicator on; changing it while the communicator is on rearms the device automatically. This setting only matters once you are connected to a server and using the communicator; offline play does not use the microphone.

Microphone input gain is a percent slider between 0 and 400, with a default of 200. It amplifies the captured microphone signal before it is encoded and sent to other players. 100 is unity gain (no amplification), 200 is a 2x boost, and 400 is a 4x boost. Raise this if remote players say you sound too quiet; lower it if your voice clips or sounds distorted. The operating system microphone level still applies first, so set that to a sensible recording level before tuning the in-game gain.

### 11.4 Volume Settings
Volume settings let you prioritize what you hear during racing.

Master audio volume controls all audio.

Vehicle engine sounds controls your own engine and throttle layers.

Vehicle event sounds controls your own horn, backfire, and local event sounds.

Other vehicles engine sounds controls opponents' engine layers.

Other vehicles event sounds controls opponents' horns, crashes, and similar event sounds.

Surface loop sounds controls road and surface loops.

Radio volume controls other players' radio streams. This is not the only setting that controls how you hear other player's radio. If someone turns their radio volume down, it affects everyone, not just the listener. This setting is a multiplier only.

Ambients and sound sources controls ambient loops and track sound sources.

Music volume controls music layers.

Online server event sounds controls multiplayer session event sounds.

Communicator volume controls the loudness of communicator activation cues, the local open and close cues you hear when you start or stop transmitting, and the voice playback of other players speaking through the communicator. It is independent of Radio volume; lowering Radio does not lower the communicator, and vice versa. Streamed media played through the communicator also obeys this slider.

Quick volume keys: Besides these sliders, there is a faster way to adjust volume without opening the settings menu. These keys work globally, including while you are driving. Press F6 to move to the next volume category and Shift+F6 to move to the previous one; the game announces the category you land on. The categories are the same ones listed above: Master, Vehicle engine, Vehicle events, Other vehicles engine, Other vehicles events, Surface loops, Radio, Ambients and sound sources, Music, Online server events, and Communicator. Once a category is selected, press F7 to decrease its volume by one step or F8 to increase it by one step, and add Shift (Shift+F7 or Shift+F8) to change it in larger steps of ten. The selected category and its new level are spoken as you adjust, so you can balance the mix by ear in the middle of a race.

All sliders in this section use the same slider gestures and keys described earlier in section 8.

### 11.5 Controls
Controls settings define how your input devices behave.

Select device chooses keyboard, controller, or both.

Force feedback enables controller vibration/force feedback when supported.

Progressive keyboard input controls how quickly steering, throttle, and brake ramp while keys are held. If this setting is off, it will have no effect.

To clarify more, keyboard is binary. It does not have progressive input such as joystick or gestures. This setting tries to simulate some kind of progressive input by letting you choose between a minimum of 0.25 seconds to 1.0 seconds. During this period, actions such as throttle, brake, and steering ramp up until they reach to full at the end of that short time window.

Use motion sensors for steering is Android-only. When enabled, steering comes from motion sensors instead of steering drag gestures.

Please note the following:

Some older phones may not support this sensor. The specific sensor used is called Game Rotation Vector. You may wish to check if your device supports it if you have issues with this option.

If this specific sensor is not found, the game falls back to a normal rotation vector sensor, which does not provide the same accuracy as the game rotation vector. Lastly, if this is also not available, it falls back to the last option which is the gyroscope. Gyroscope is not ideal at all for steering and so please disable this option if your phone does not support the mentioned sensor for the best experience.

Map keyboard keys opens keyboard remapping for driving actions.

Map controller keys opens controller remapping.

Throttle pedal direction, brake pedal direction, and clutch pedal direction can be Auto, Normal, or Inverted. They help if some of your controls on your joystick are detected wrong or inverted, this setting may help.

Steering dead zone controls how much tiny center movement is ignored on controllers. Because controller wheels are usually not exactly centered, this setting is here to assist.

Map menu shortcuts opens shortcut remapping for menu actions. This is only for keyboard.

### 11.6 Race Settings
Race settings define your default race behavior.

Copilot chooses how much co-pilot information is announced: Off, Curves only, or All.

Curve announcements chooses fixed-distance calls or speed-dependent calls.

Speed dependent curve announcement lead time controls how early speed-dependent calls are spoken.

Automatic race information controls automatic lap and race commentary during driving.

Number of laps sets default lap count for race modes.

Number of computer players sets default bot count for single race.

Single race difficulty chooses easy, normal, or hard.

Enable fuel consumption turns on the fuel model for single race and time trial. When on, your car burns fuel and you must pit to refuel. When off, you race without a fuel limit. See section 10.6 for how fuel, tires, and pit stops work together.

Enable tire wear turns on the tire model for single race and time trial. When on, your tires heat up, wear, and lose grip, and you pit to replace them. When off, tires never wear. In multiplayer these two are set by the host through the room's race rules rather than by this menu.

A good beginner starting setup is: co-pilot on All, speed-dependent announcements enabled, automatic race information on, and moderate lap count. Leave fuel consumption and tire wear off until you are comfortable driving, then enable them when you want to try pit strategy.

### 11.7 Server Settings (Client-Side Multiplayer Defaults)
Default server port is used when you enter a host without typing a port.

Default call sign is the name pre-filled in multiplayer connection dialogs.

### 11.8 Defaults Summary
On a clean setup, the game starts with practical beginner defaults such as menu hints enabled, menu wrapping enabled, co-pilot enabled, and startup update checks enabled.

If your settings become confusing, restore only the options you changed recently instead of resetting everything at once.

## 12. Multiplayer Client Guide
Multiplayer in Top Speed Remake is very different from the original Top Speed. First you have to find a server to connect to, or host one yourself. Because I can't maintain an official server myself, some common servers maintained by community members are included in this guide.

A few things to know before starting: When you connect to a server, the game first asks you to enter your call-sign. This is an identification like a name for yourself on the server, for other players to see you.

Second, the multiplayer server in this game requires either you to create a room, or join an existing room. It is not like the original game where you could just connect and race with anyone on the server.

### Connecting to a server
You can connect in three ways.

Local network discovery searches nearby servers on the same LAN. This only works if you are the host of the server.

To access this, open the game, go to Multiplayer game, choose "Join a game on the local network". The game will search for available servers on the local network and if one is found, it prompts you to choose one.

It's also worth mentioning that this option works when you are connected to a network using some external apps such as Radmin VPN.

Another way to connect is saving the servers you visit often. If you know some external servers that are being maintained, you may wish to save them.

To do this, go to the multiplayer game and choose "Manage saved servers". A menu will appear with a list of servers you have. If you don't have anything there, click on "Add a new server", fill out the information, and save it. From now on, you'll be able to access the server without typing any info, by going to the server management menu again.

A third way to connect is that you enter the server manually, by going to the multiplayer game menu and choosing the relative option from there.

Please note the following:

Do not append "http://" or "https://" to the server host if it is a domain.

The game supports specifying the port when typing the server host by using the following format: "host:port" where "host" is the server host or IP address, and port is the port number. Only do this if you know the server uses a different port number.

The default port number used by both client and server is 28630. The server also uses another port (28631) for discovery on the local network. You do not need to worry about this port if you are running it on a VPS server as it is not needed.

### Lobby and room
After connecting, you enter the lobby. From there, you can browse rooms, create a room, join a room, or disconnect.

If you create a room, you are the host and control room-level actions. If you join a room, you control your own readiness and race setup, but host-only actions remain with the host.

The following actions are available for the host only:

"Start the game": Informs all players that the host is starting the game, and opens a menu to choose their vehicle.

"Change game options": Setup various game options such as lap count, the track used for the race, and maximum allowed players in the room.

"Add"/"Remove a bot": Adds or removes a bot, if the room type allows that. They are hidden if the room type is either "race without bots" or "one-on-one without bots".

### Chat, history, and ping
The remake adds text chat (global and per-room), voice chat through the communicator covered in section 13, and a quick way to check your current ping. Each of these has its own controls, but they all share the same multiplayer history buffers — every chat message, server announcement, and room event you receive is stored in a history category you can scroll back through later.

The default shortcut to open the global chat on keyboard is the slash (`/`) key. Room chat is opened with the backslash (`\`) key while you are inside a room. To check the ping, press F1.

#### Two ways to navigate history buffers
History buffers (global chat, room chat, server messages, room events) can be reviewed in two different ways on desktop. Pick whichever fits the moment — both end up in the same place.

The first way is the dedicated history screen. The multiplayer lobby and the in-room controls each have two screens on desktop: the first screen holds normal menu options, and the second screen is the history buffer. Press Tab to move to the next screen and Shift+Tab to move back. On the history screen, every entry appears as a normal menu item, so you can browse messages with the usual Up Arrow / Down Arrow keys (or the menu navigation gestures on mobile in the top zone). Activating an entry copies it to the clipboard.

The second way is the inline history shortcuts. These work anywhere in the multiplayer menus (and during a multiplayer race, for the in-race overlay), without having to switch screens. They are the fastest way to glance back at a message while you are doing something else:

- `,` (comma) — previous history item in the current category.
- `.` (period) — next history item in the current category.
- `Shift+,` — first history item in the current category.
- `Shift+.` — last history item in the current category.
- `[` — previous history category (e.g. global chat → server messages).
- `]` — next history category.
- `Ctrl+Space` — copy the currently focused history item to the clipboard.

When you change category with `[` or `]`, the game plays a short buffer-switch cue and announces the new category. Item navigation announces the message text. Both styles share the same focused item: if you scroll on the history screen and then leave it, `,` / `.` continue from where you stopped.

#### Mobile layout
On mobile, the multiplayer menus are split into two zones. The bottom zone is for normal menu navigation (move, activate, back out). The top zone is reserved for history buffers, chat shortcuts, the communicator, and ping — the same conceptual split is used on desktop with Tab, but on mobile the two zones are on the screen at the same time.

All gestures below assume you hold the phone in landscape mode, and they only fire when the gesture starts inside the top zone:

- Swipe up or down with one finger: switch to the previous or next history category.
- Swipe left or right with one finger: move to the previous or next history item in the current category.
- Two-finger swipe right: open the global chat input.
- Two-finger swipe left: open the room chat input (only when you are inside a room).
- Double tap with one finger: check the current ping.
- Two-finger swipe down: toggle the communicator on or off (see section 13).
- Two-finger swipe up: open frequency input for the communicator.
- Three-finger tap: speak the current communicator frequency.
- Two-finger double tap: toggle voice activation on the communicator.

This is the same multiplayer top-zone layout the driving controls use during a multiplayer race, so the buffer and communicator gestures you learn in the lobby keep working once a race starts.

### List of common community servers
These servers are fully maintained by community members and I myself have no control on any of these servers in any way. They exist for everyone to quickly connect and play with their friends without needing to host a server.

Important warning: Because the server has logging features such as messages sent in the global chat, or any other data, I am not responsible for logging of such data, nor do I have control on how your data is handled. Please be aware of that.

The following is the list of the servers. If you wish yours to be included, contact me.

The port is omitted when the server uses the default port which is 28630.

1. Muhammad Gagah:

Address: tt.mgagah.my.id

2. Adriano:

Address: adriano.mlbfan.org

Port: 25255

3. Valiant8086:

Address: valiant8086.redirectme.net

4. Christopher Wright:

Address: christopherw.me

5. Boris Churkin:

Address: iks2101.keenetic.pro

Port: 25255

## 13. Communicator (Multiplayer Voice and Streaming)
The communicator is a virtual radio device built into the multiplayer client. Once you connect to a server, you can turn it on, tune it to a frequency, and either talk to other players with your microphone or stream audio files through it. Anyone tuned to the same frequency on the same server hears you, regardless of which room they are in or whether they are in a room at all. This is what makes the communicator useful before, during, and after a race: lobby chatter, room coordination, and a private channel for friends all use the same device.

The in-vehicle radio described later in section 15 is a separate feature and only plays on your own car. The communicator is for talking to other players and for streaming media that other players can hear.

### How the communicator works
The communicator has one frequency, between 0.0 MHz and 1000.0 MHz, adjustable in 0.1 steps. The public default that every new player is tuned to is 1.0 MHz. Setting a frequency to 0.0 is treated as "no channel" and keeps the communicator silent until you tune to a real value.

Transmissions are relayed by the server to every connected player, and each receiving client decides what to play based on its own frequency. A listener tuned to a different number simply hears nothing. There is no per-room scope; the communicator works in the lobby, inside a room, and across rooms.

The communicator has two transmit modes that are mutually exclusive:

- Push-to-talk (PTT) is the default. You hold a key or gesture while you speak; the moment you release it, transmission stops.
- Voice activation (VOX) is a toggle. While it is on, the communicator transmits continuously with whatever the microphone is capturing. VOX does not use a voice detector to gate transmission; it stays open until you turn it off.

Audio is encoded as 48 kHz mono Opus in 20 ms frames. The encoder runs the moment the communicator is armed (turned on, tuned to a non-zero frequency, and connected), so the operating system microphone is opened as soon as the communicator becomes ready and is released the moment the communicator turns off. The communicator also turns off automatically when you disconnect from the server, when you press Ctrl+Shift+C to turn it off (which also turns voice activation off), or when you set the frequency to 0.0; in each of these cases the microphone is released so nothing is captured.

You hear a short local cue when transmission opens and another when it closes; remote listeners hear a separate release cue after you let go of PTT so they know you finished talking. Turning the communicator on or off also plays its own short cue. All communicator cues and remote voice playback share the same Communicator volume slider in the audio settings.

### Turning it on and using it
All communicator gestures on mobile live in the multiplayer menu top zone, the same zone used for chat history and ping. They work whenever the multiplayer menu is on screen, including the lobby and inside a room.

1. Connect to a server.
2. Turn the communicator on or off with Ctrl+Shift+C on desktop, or a two-finger swipe down in the multiplayer top zone on mobile.
3. The default frequency is 1.0. To change it, press Ctrl+Shift+F on desktop, or two-finger swipe up in the multiplayer top zone on mobile, then type a value between 0.0 and 1000.0.
4. To hear the current frequency announced, press F on desktop, or three-finger tap in the multiplayer top zone on mobile.
5. To talk, hold V on desktop. On mobile, single-finger tap in the multiplayer top zone, then within about 0.4 seconds press and hold a single finger in the same zone; the communicator transmits while the second touch is held and stops the moment you release.
6. To switch to voice activation instead, press Ctrl+Shift+V on desktop, or two-finger double tap in the multiplayer top zone on mobile. The communicator now transmits continuously until you toggle it off again.

Push-to-talk explicitly ignores key presses while Ctrl, Shift, or Alt are held, so toggling VOX with Ctrl+Shift+V never bleeds into the PTT path. The frequency announcement (F) and PTT (V) are deliberately separate keys: a tap on F speaks the current frequency without transmitting anything; pressing and holding V transmits without speaking the frequency. The mobile PTT gesture is intentionally a tap followed by a press-and-hold so it cannot trigger from a single accidental touch, and the ping gesture is briefly suppressed while it is active so the press-and-hold is not interpreted as the start of a triple-tap ping check.

Changing frequency does not break an active transmission. If you change frequency while talking, the communicator restarts the transmission on the new value, so listeners on the old frequency hear it end and listeners on the new frequency hear it begin.

### Volume
The communicator has its own slider in the volume settings, separate from the in-vehicle radio. It controls how loud the communicator activation cues are and how loud other players sound when they speak through the communicator. The in-vehicle radio volume is unaffected.

Lower this slider if voice traffic is loud compared to your race audio. Raise it if remote players sound too quiet even after they adjust their own microphone gain.

### Microphone settings
Two audio settings affect how your microphone is captured and sent to other players:

- Voice input device chooses which microphone the communicator listens to. The default is your system's default capture device. Switching device while the communicator is off takes effect the next time you turn it on; switching while it is on rearms the device automatically.
- Microphone input gain is a 0–400 percent slider with a default of 200. 100 is unity gain (no amplification); 200 is a 2x boost; 400 is a 4x boost. The gain is applied to the captured samples before they are encoded. Raise it if other players report you sound too quiet at default Windows microphone level; lower it if your voice clips or sounds distorted.

The Windows (or Android, or Linux, or macOS) microphone level slider in the operating system still applies first, then the in-game gain runs on top of it. For best quality, set the system microphone close to its normal recording level and then adjust the in-game gain to taste.

### Streaming media through the communicator
The communicator can also stream audio files into the same channel. This is useful for sharing music with people on your frequency, or for verifying that the communicator path works without needing two players in the same room.

While the communicator is on:

- Ctrl+O: load a single audio file.
- Ctrl+F: load a folder (builds a playlist from the supported files inside that folder).
- Ctrl+P: play or pause.
- Ctrl+Page Up: previous track.
- Ctrl+Page Down: next track.
- Ctrl+Up Arrow: media volume up.
- Ctrl+Down Arrow: media volume down.
- Ctrl+L: toggle loop.
- Ctrl+S: toggle shuffle.

Streamed media goes out on your current communicator frequency. Everyone tuned in hears it; everyone else does not. Folder loading is not recursive, like the in-vehicle radio: only files directly inside the chosen folder are added to the playlist.

Streaming media and talking work side by side. Holding push-to-talk (or transmitting through VOX) while media is playing sends your voice on the same frequency in parallel with the media, and remote listeners on that frequency hear both at once.

### Troubleshooting voice chat
- Remote players hear nothing while I can hear my own open cue: check that both sides have the communicator on, that the frequency is the same on both ends (try pressing F on both clients), and that both clients are connected to the same server. The most common cause is a frequency mismatch.
- I sound too quiet to other players: raise Microphone input gain in Audio settings. If you are already at 200% or higher and it is still quiet, raise the operating system microphone level first, then retune the in-game gain.
- I sound distorted or clipping: lower Microphone input gain. Distortion is usually upstream gain being too high, not the encoder.
- Voice activation (VOX) is on but nothing transmits: confirm the communicator itself is on (Ctrl+Shift+C), the frequency is non-zero, and the right voice input device is selected. VOX has no VAD gate, so if the device is the wrong one, you may have toggled VOX on for a silent microphone.
- Pressing V plays no local open cue: the communicator is off; turn it on first with Ctrl+Shift+C.

## 14. Dedicated Server Guide
Dedicated server is the best choice when you want a stable multiplayer session that does not depend on one player keeping the game client open. The server is a separate program. Players connect to it, create rooms, join rooms, chat, and race through it, but the server itself does not need to drive a vehicle or play the game.

If you are only testing with one or two people, you can still host casually. For public rooms, planned events, or a server that should stay available for a long time, use the dedicated server. This keeps hosting separate from playing, makes restarts easier to schedule, and avoids the problem where a whole session depends on the host player's client staying open.

The server supports almost all platforms, Windows, macOS, Linux, Linux ARM, Linux ARM-64, and Linux MUSL variants.

All server releases can be found on the GitHub page [here](https://github.com/diamondStar35/top_speed/releases/latest). There you can also download the game for various operating systems.

Run the server from its own folder and keep its files together. On first startup, it creates the settings file it needs. After that, you can close the server, edit settings if needed, and start it again. The important settings are the server port, discovery port, maximum connected players, language, and message of the day.

The default gameplay port is 28630. This is the port clients use when they connect to the server. The default discovery port is 28631. This is used for local network discovery, so clients on the same LAN can find the server without typing the address manually. Discovery is useful at home or on a private network, but it is not the main connection method for an internet server.

For LAN hosting, start the server on one machine and then use the client option that searches for games on the local network. If nothing appears, the server may still be working; local discovery can be blocked by router settings, Wi-Fi isolation, VPN behavior, or firewall rules. In that case, try manual connection with the computer's local IP address and the server port.

For internet hosting, players usually connect by saved server entry or manual host entry. The server port must be reachable from outside. That usually means forwarding the gameplay port on the router to the machine running the server, and allowing the same port through the operating system firewall. If players can see or reach the machine but cannot join, check the host address, forwarded port, local firewall, and whether the server is actually listening on the expected port.

The server supports command-line overrides for a few common settings. Use `--port <number>` to temporarily use a different gameplay port, `--max-players <number>` to change the maximum player count for that run, and `--motd <text>` to set the message of the day. Use `--help` to print the available command-line options.

When the server is running in an interactive console, it also has a command interface. Type `help` to list available commands. Current commands include `options`, `players`, `version`, `update`, and `shutdown`. `players` is useful when you want to confirm who is connected. `version` is useful when checking whether a client and server are from compatible builds. `shutdown` tells connected players the server is closing and then stops the process.

Use the `options` command for server-side settings that can be edited from the console. It lets you review or change language, message of the day, server port, discovery port, maximum players, server architecture used for update selection, startup update checks, and the per-feature flags described below. Some changes are useful immediately, while port-related changes are safest after a restart because connected clients and router rules already depend on the old values.

### Server feature flags
The server has a small set of feature flags that turn major features on or off for everyone connected to it. They are all on by default. Each flag can be toggled from the `options` command in the server console, or set directly in the server settings file.

- `text_chat` enables global and per-room text chat. With it off, the chat shortcuts on clients have no effect and chat history stays empty for everything that happens after the flag is turned off.
- `voice_chat` enables the voice relay used by the communicator. With it off, communicator transmissions are silently dropped on the server side; clients can still toggle their own communicator on, but no audio reaches other players. Streamed media through the communicator also goes through this flag.
- `custom_tracks` enables custom track use across the whole server. With it off, room hosts cannot enable the "Custom tracks" game rule, and the server's custom track catalog is not offered to clients. Built-in tracks are unaffected. See section 16 for how custom tracks move between client and server when this is on.
- `custom_vehicles` enables custom vehicle use across the whole server. With it off, room hosts cannot enable the "Custom vehicles" game rule, and the server's custom vehicle catalog is not offered to clients. Built-in vehicles are unaffected. Unlike the "Custom tracks" game rule, the "Custom vehicles" game rule starts off for a new room, so a host has to turn it on before players can pick a custom vehicle.

Changes to feature flags apply to traffic that arrives after the flag is changed. Players already in the middle of a transmission, chat send, or upload usually finish their current action; new ones honor the new flag.

### Hosting custom tracks from the server
The dedicated server can hold a library of custom tracks that every client sees when the room host opens the custom track catalog. To do this, create a folder named `Tracks` next to the server executable and drop one complete track package into it per track — that is, the whole folder you would normally put under your game's `Tracks` directory, with the track's `.tsm` file and every sound or other asset it references kept in the same place relative to the `.tsm`. A loose `.tsm` on its own is not enough as soon as the track references any external assets; the server resolves sound paths relative to the `.tsm`, so the assets have to travel with it.

Subfolders inside `Tracks` are scanned recursively, so a layout like `Tracks/<track-name>/track.tsm` (plus any sound subfolders the package needs) works, and you can group packages into category folders if you want. The server reads each package's `.tsm` at scan time and serves it to clients as one self-contained download.

The server scans `Tracks` at startup and again whenever a client requests the custom track catalog. New packages appear on the next catalog request; you do not have to restart the server for them to show up, but a restart is the cleanest way to refresh after a bulk update. Packages with broken or unsupported `.tsm` contents — including a `.tsm` that references sound files that are not next to it — are skipped with a warning in the server console.

In addition to the static `Tracks` folder, the server accepts uploads from room hosts who use "Upload a local track" in their room. Uploaded packages are stored server-side alongside the static library and offered through the same catalog. Section 16 covers the client-side flow in detail.

The `custom_tracks` flag must be on for any of this to be visible to clients; with it off, the catalog stays empty even if the `Tracks` folder is full.

Do not make disruptive changes while players are preparing or racing unless you have to. If you need to update the server or change network settings, announce it first, wait for the current race to finish, shut the server down cleanly, apply the change, and then start it again.

After an update or configuration change, test it before inviting players back. Connect with a client, create a room, join it, start a short race, finish or leave, then disconnect and reconnect once. That simple check catches the most common mistakes: wrong port, blocked firewall, missing content folders, incompatible version, or a server that starts but cannot actually host a room.

## 15. Panels and Vehicle Radio
During a race, Top Speed has more than one input panel. The normal driving panel is the control panel. That is where steering, throttle, brake, clutch, shifting, horn, engine start, and race information keys are handled. The radio panel is a separate panel for playing music while driving.

The radio in this section is your own car's in-vehicle radio — only you hear it (other players hear it as a faint stream coming from your car if they are nearby). If you want to share audio with other players over a frequency, that is a separate feature called the communicator; see section 13.

When you switch panels, the game announces the new panel name. This matters because the same physical key can mean different things depending on the active panel. For example, while you are in the control panel, arrow keys are part of driving. While you are in the radio panel, the radio uses its own commands for volume and track movement.

The radio panel can load a single audio file or a whole folder. Supported file extensions are `.wav`, `.ogg`, `.mp3`, `.flac`, `.aac`, and `.m4a`. When you load a folder, the game scans only that folder for supported audio files and builds a playlist. It does not require you to create a playlist file manually.

When you switch to the radio panel, the first thing to play a file is to either load a file or a folder. You can do one of the following:

To load a single file, press the letter o on your keyboard. This will open a file chooser dialog to choose a file.

To load an entire folder, press the letter f on your keyboard. It opens a folder chooser dialog to choose a folder and builds a playlist with all the supported media files it finds.

There are no gestures for the radio on mobile as of now.

To play the file after loading it, press the letter p. By default, this plays the single file that you have loaded, or the first file of the folder in alphabetical order, unless shuffle is enabled.

It is also worth mentioning that loading a folder is persisted across sessions. This is useful when you have a music folder and you want to save it without having to reload it every time.

The next time you want to play a file, you can just switch to the radio panel and press the letter p. This will immediately play the first file of the loaded folder, as mentioned above.

To change the volume, use the up and down arrows.

To navigate between tracks, use Page Up or Page Down.

To pause the currently playing track, use the letter p while playing.

To toggle shuffle, press the letter s.

To toggle looping of the file, press the letter l. Note that looping is enabled by default for single files.

Radio volume is separate from some other game volume categories. Adjusting it changes how loud the radio is compared with engine, menu, speech, and multiplayer sounds. If you are testing multiplayer radio behavior, remember that other players may also have their own radio-related volume settings, so what sounds balanced on your machine may not sound identical for everyone else.

If a radio command appears to do nothing, first make sure you are actually in the radio panel. If the panel is correct, make sure media has been loaded. If folder loading succeeds but nothing plays, check that the folder contains supported audio files directly inside it.

Please note that folder loading is not recursive. That means it only scans for top-level files inside a particular folder. It does not scan all subfolders inside it, even if there are supported media files.

## 16. Custom Tracks and Custom Vehicles
Top Speed supports custom tracks and custom vehicles, but the game only loads files it understands and only from the expected folders.

Custom tracks use `.tsm` files and belong in the `Tracks` folder. Custom vehicles use `.tsv` files and belong in the `Vehicles` folder. Both folders sit next to the game executable on desktop, and inside the app's data folder on mobile. Do not place them inside the sounds folder, language folder, or a random subfolder unless the authoring guide for that content type says otherwise.

If a custom file does not appear in selection menus, check the simple things first. Make sure the extension is correct. Make sure the file is in the correct folder. Then make sure the file itself is valid. A file with the right extension can still fail if its contents are not in the format the game expects.

Random selection has its own settings. If custom randomization is disabled, random track or vehicle selection stays with the built-in content even if custom files are installed. Enable the relevant custom random options if you want random choices to include your added content.

For creating new tracks or vehicles, use the dedicated authoring guides. This player guide explains where custom content goes and how it is selected, but the authoring guides explain file structure, supported fields, and validation rules.

Important note: all previous tracks and vehicles from the original game are incompatible, and they cannot be made compatible unless you rewrite them entirely. There is no possibility of adding a compatibility mode for these legacy tracks or vehicles because my project aims to add new features and details, and these can't be preserved or implicitly calculated based on non-existent values from the legacy files.

### Custom tracks in multiplayer
Custom tracks work in multiplayer races as well as single player, but they have a few extra moving parts because the host's chosen track has to be available on every other client before the race can start. The game handles the transfer automatically; this section explains what is happening so it is easier to understand when something goes wrong.

For a custom track to appear in a multiplayer room, three things have to be true:

1. The server has `custom_tracks` turned on. Section 14 describes the server-side flag.
2. The room host has enabled "Custom tracks" under Game rules in room options.
3. The chosen track is available either in the server's catalog (hosted on the server, or uploaded earlier by another host) or as a local file in the host's own `Tracks` folder that can be uploaded.

When all three are true, the host's track selection menu inside the room gains two extra entries:

- "Custom" opens the server's catalog. This includes tracks the server operator placed in the server's `Tracks` folder and tracks that previous room hosts uploaded.
- "Upload a local track" opens a list of the host's own `Tracks` folder. Picking one uploads it to the server. After the upload finishes, that track joins the server's catalog and is available to future rooms too.

Only the room host sees these menus; joining players see the chosen track but cannot upload or change it.

### How clients receive a custom track
The first time the host picks a custom track that a particular client does not already have, that client receives the entire track package from the server. The flow runs like this:

1. The server announces the chosen track to everyone in the room.
2. Each client checks its local cache for a track with the same content hash. If it is already cached, the client uses the cached copy directly and no download happens.
3. If the cache does not have it, the client receives the package in chunks and shows a short download progress dialog so the player knows something is happening.
4. After the last chunk arrives, the client verifies the hash to make sure transmission was not corrupted, then stores the package under the game's app data folder in `track_packages/<hash>.tspkg`. Any custom sounds or assets bundled with the package are extracted into the same cache so the race can load them.
5. Once a client has the package cached and reports it as ready, the race can start for that client. Players who already had it cached are ready immediately; players who had to download wait for the transfer to finish.

After the first time a client has a given package, the cache is used directly on every subsequent join — the same custom track never needs to be re-downloaded for that client, even across game sessions, unless the package contents change (which produces a new hash).

If a transfer is interrupted (the player disconnects, the room is closed, the server restarts), the partial download is discarded and the next attempt starts from the beginning. Uploaded packages exceed a fixed maximum size on the server side; oversized files are rejected with a message in the server console and on the host's client.

### Hosting custom tracks for everyone on a server
If you run a dedicated server, there are two ways custom tracks get added to its catalog:

- The server operator drops `*.tsm` files into a `Tracks` folder next to the server executable. The server scans this folder recursively, so the files can sit in subfolders. New files appear on the next catalog request; a server restart is the cleanest way to refresh after a bulk update.
- A room host on a connected client uses "Upload a local track" inside a room. The upload is delivered to the server, validated, stored, and added to the catalog for future rooms.

Both paths go through the same catalog, and both are gated by the server's `custom_tracks` flag. Section 14 covers the server-side details.

## 17. Updater Behavior on Desktop and Android
The updater is designed to keep the installed game current without making the player manually replace files every time. You can check for updates from the menu, and the game can also check automatically on startup if that setting is enabled.

The update process has three stages. First, the game checks the published version information. Second, if an update is available, it asks before downloading. Third, after download finishes, it starts the platform-specific install step.

On desktop, update packages are zip files. The game downloads the zip and then launches the updater program to replace the installed files safely. If the updater program is missing, the game cannot continue with automatic installation and will report that the updater was not found.

On Android, update packages are APK files. The game must not extract an APK like a zip update. Instead, it passes the APK to Android's package installer. Android then shows its own install flow and takes over from there.

Android may require permission to install unknown apps for the app or installer path being used. If the update downloads but installation does not begin, open Android settings when prompted and allow that install permission, then return to the game and try again.

If a download fails, the current game installation remains as it is. If a download succeeds but installation fails repeatedly, delete the downloaded update package if possible, run the update check again, and verify that the device has enough storage and that Android is not blocking installs from that source.

Please report bugs if you see any weird behavior with updater.

## 18. Detailed Driving Tutorials
This chapter is for learning how to drive, not only for learning which key does what. Top Speed is an audio racing game, so driving well means listening to the engine, surface, curve calls, position information, and the behavior of your own vehicle.

### Automatic mode training
Start with Time Trial, automatic transmission, one track, and one vehicle. Do not begin by chasing top speed. Your first goal is to finish laps cleanly and understand what the track sounds like.

Before the first curve, accelerate smoothly and listen to the engine. The engine pitch tells you how hard the car is working. If you brake too late, enter the curve too fast, or steer too much, you will hear the result through the car's movement and may lose the line or crash.

Curve announcements are there to help you prepare before the turn. If co-pilot calls are enabled, listen to the direction and timing of the call. A curve call should make you think about three things: whether to slow down, when to begin steering, and when to return to throttle.

In automatic mode, the game handles gear shifting for you. That lets you focus on steering, throttle, and braking. Use this mode until you can complete several laps without constantly losing control. Once you can repeat the same lap reliably, begin increasing speed gradually.

### Manual mode training
Manual transmission adds clutch and shifting. It is better to learn it after you can already drive the same vehicle in automatic mode.

When driving manual, you are responsible for keeping the engine in a useful range. Shift up as speed increases. Shift down when slowing for curves or recovering from low speed. If you try to shift without using the clutch properly, the shift may fail or the vehicle may behave badly.

Practice on a straight section first. Accelerate, use clutch, shift up, release clutch, and continue. Then slow down and practice shifting down. Do this before trying to combine manual shifting with difficult curves.

If you stall or lose speed badly, do not rush every control at once. Recover in order: point the vehicle in a safe direction, restart or stabilize the engine if needed, select the right gear, then accelerate again.

### Using shift-on-demand
Shift-on-demand lets you temporarily request shifting behavior while otherwise relying on automatic driving. It is useful if you prefer automatic transmission most of the time but want extra control in a specific moment.

Use it deliberately. If you toggle it without a reason, it becomes another thing to manage during a race. If you use it for a specific driving style or vehicle, practice it on the same track until it becomes predictable.

### Recovery after collision or drift
After a crash, bump, or heavy drift, do not immediately hold full throttle. First listen to where the vehicle is and stabilize direction. If the engine stopped or the vehicle is in the wrong gear, fix that next. Only accelerate once you know you are pointing in a useful direction.

The most common recovery mistake is trying to win back all lost time immediately. That usually causes a second crash. A clean recovery is faster than an aggressive recovery that fails.

### Android touch driving routine
On Android, learn the screen zones in stages. First practice only the driving zone: throttle, brake, engine start, gear changes, and steering method. Do not add information gestures until the basic driving controls feel natural.

After that, add the top-zone controls one at a time. Practice reporting speed, gear, lap information, and player information without losing control of the vehicle. If you use motion steering, decide early whether it works well on your phone. If the sensor feels unstable or delayed, disable motion steering and use touch steering instead.

When learning touch controls, drive slower than you would on desktop. The goal is to build reliable hand movement first. Speed comes later.

## 19. Multiplayer Setup Walkthroughs
This chapter gives practical examples of how to set up multiplayer sessions. The goal is to make sure every player understands the same sequence: connect to server, enter lobby, create or join room, prepare for race, then start.

### Local network session
For a local network session, start the dedicated server on one computer. Keep the server window open. On the client, use local network discovery first. If the server appears, choose it, enter your call sign, and connect.

If discovery does not find it, use manual connection. Find the local IP address of the computer running the server, then enter that address with the server port if needed. If manual connection works, the server is fine and only discovery is blocked by the network.

After connecting, one player creates a room. Other players join that room from the room list. The host sets options such as track, lap count, and rules. When the host starts preparation, every player chooses a vehicle and transmission and marks ready. The race starts after the required players are ready.

### Internet session
For an internet session, the host should use a dedicated server if possible. Forward the gameplay port from the router to the server machine and allow the same port through the firewall. Then give players the public host name or IP address and port.

Players should save the server if they will use it often. Saved servers reduce typing mistakes and can include a default call sign. If connection fails, verify the host and port first. Then verify the server is running and that the router forwards traffic to the correct local machine.

### Private room sequence
A private room should be organized before preparation starts. The host creates the room, sets the intended track and lap count, confirms whether bots are allowed, and announces the rules to everyone in the room.

Players should not mark ready until they have chosen the vehicle and transmission they actually want. If someone needs a change after preparation starts, the host should cancel preparation if needed, wait for everyone to adjust, then start preparation again.

### During-race coordination
Use room chat for messages related to the current room or race. Use global chat for messages intended for everyone on the server. During a race, keep messages short because players are already listening to engine, co-pilot, position, and collision information.

If a player disconnects, crashes, or needs to stop, the host should decide whether to continue or restart based on the room's expectations. For casual rooms, restarting may be fine. For organized events, decide rules before the race begins.

## 20. Settings Tuning Profiles
There is no single best settings setup. A new player needs more guidance and spoken information. An experienced player may want fewer interruptions. A controller user may need dead zone tuning. An Android player needs to choose between touch steering and motion steering.

### New player profile
For a new player, keep usage hints enabled. Keep menu wrapping enabled if wrapping makes navigation easier for you, or disable it if you prefer menus to stop at the first and last item. Keep co-pilot and automatic race information enabled while learning tracks.

Do not reduce speech too early. The information may feel like a lot at first, but it teaches you how menus and races are structured. Once you know the game, you can reduce what you no longer need.

### Competitive keyboard profile
For keyboard racing, focus on consistent input. If progressive keyboard input is enabled, tune it until steering, throttle, and brake ramp in a way you can predict. If you prefer immediate key response, turn it off.

Reduce non-essential race speech only after you know what you are removing. Keep the information that helps you avoid crashes and judge race progress.

### Controller profile
For controller, wheel, or pedals, start with dead zone. If steering drifts when you are not touching the wheel, increase the dead zone slightly. If steering feels unresponsive around center, reduce it carefully.

Then check throttle, brake, and clutch direction. If pressing a pedal makes the game behave as if you released it, use the pedal direction options. Test every change on the same straight and curve so you can compare accurately.

### Android touch profile
On Android, choose one steering method first. If motion steering works well on your phone, use it consistently and practice holding the phone in a stable neutral position. If motion steering feels unstable, disable it and use gesture steering.

Learn the driving zone before learning every information gesture. The safest order is throttle and brake, then steering, then gears and engine, then horn and clutch, then race information gestures.

### Translation testing profile
For translation testing, enable usage hints and walk through menus slowly. Record the exact English line, the menu where it appears, and the language being tested. Do not only report that "some lines are English"; that is not enough to find the missing text.

After replacing a language file, restart the game once before deciding that a string is still untranslated. This avoids confusing stale menu text with a missing translation.

## 21. Additional Notes for Testers and Release Preparation
Testing is most useful when the report can be repeated by someone else. Always include the platform, game version, what you were doing, what you expected, and what actually happened.

### Minimum test
A basic test should cover startup speech, first menu navigation, settings opening, starting a time trial, finishing or leaving a race, connecting to a multiplayer server, joining and leaving a room, and running a manual update check.

For race testing, do not only start a race and quit immediately. Drive long enough to hear curve calls, lap information, vehicle sounds, and at least one information request.

### Android test
On Android, test fullscreen startup, menu gestures, logo skipping, text input, speech output, race touch zones, motion steering if enabled, multiplayer connection, and APK updater permission behavior.

If a new APK appears to behave like an old build, uninstall completely and reinstall before reporting a bug. Stale installs can make testing results misleading.

### Bug report quality standard
A good report gives enough information for someone else to reproduce the issue. Include device or operating system, game version, input method, language, speech backend if speech is involved, and whether the issue happens every time or only sometimes.

For multiplayer issues, also include whether you were host or member, whether you connected by discovery, saved server, or manual host, and whether the issue happens on LAN, internet, or both.

### Common mistakes during testing
Common testing mistakes are stale Android installs, typing the wrong server host or port, testing on a router with isolation enabled, using a broken custom track or vehicle, and changing too many settings before retesting.

Check those first. If the issue remains after that, write the report with the exact steps and keep the broken setup available in case more logs are needed.

## 22. Troubleshooting
This section explains common problems in plain terms. When something goes wrong, change one thing at a time and test again. If you change several settings at once, it becomes much harder to know which change actually fixed or caused the problem.

### Game starts but no speech
If the game opens but you hear no speech, first check the speech mode. If speech mode is set to braille only, spoken output is not expected. Switch back to speech output or speech and braille output before testing anything else.

If speech mode is correct, open speech settings and test another backend. After changing backend, move to a simple menu item and see whether it speaks. If one backend works and another does not, keep using the working backend and report the exact backend names involved.

On Android, backend switching depends on the Prism speech layer and the available Android speech services. If speech becomes silent immediately after changing backend, restart the game once and test the same backend again. That tells you whether the backend itself is unusable, or whether only the live switch failed.

### Language changed but some lines are still English
If you install or replace a language file while testing, some menus may already have been built before the new translation was loaded. Change to the target language, leave the current menu, and return to the same place. If the same lines remain English, restart the game once and check again.

If the text is still English after restarting, the language file probably does not contain those lines, or it was made for a different game version. In that case, note the exact menu name and the exact English sentence so the translation file can be updated.

### Menu hints do not speak
Usage hints can be turned on or off. If automatic hints do not speak, open settings and make sure usage hints are enabled.

Then request the hint manually on the current item. On desktop, use the hint key. On mobile, use the hint gesture. If manual hints work but automatic hints are too early, too late, or interrupt too aggressively, run screen-reader calibration again. Calibration helps the game estimate how long speech takes before the next delayed hint should be spoken.

### Startup logo does not skip
While the logo audio is playing, press Enter on desktop or swipe up on mobile. The skip input is handled during the logo screen, before the main menu opens.

If the logo does not skip on mobile but gestures work after the menu opens, restart the app and test again from a clean launch. If gestures do not work anywhere, the issue is touch input, not only logo skipping.

### LAN server not found
LAN discovery needs devices to see broadcast traffic on the local network. Some routers block that when AP isolation, client isolation, guest network isolation, or similar options are enabled.

If discovery finds nothing, try manual connection to the server computer's local IP address and port 28630. If manual connection works, the server is running and only discovery is blocked. If manual connection also fails, check firewall rules and confirm the server is actually running.

### Manual host fails but saved server works
Manual host entry must be typed exactly. Do not add `http://` or `https://` before a domain name. If the server uses a custom port, type it as `host:port`.

If a saved server connects but manual entry does not, compare the saved host and port with what you typed manually. A wrong port, old address, extra space, or protocol prefix is enough to make the manual connection fail.

### Android opens but appears blank or non-responsive
If an Android build appears to install but behavior does not change, first suspect a stale install. Fully uninstall the app from Android settings, then install the APK again.

If the app opens to a blank or non-responsive screen after a clean install, capture Android logs during startup. Startup errors usually show as missing native libraries, missing managed assemblies, SDL initialization errors, speech initialization failures, or unhandled game exceptions.

### Android networking fails only on one device
If other devices can connect but one Android device cannot, the problem is probably device or network configuration. Test the same APK on another network if possible.

Check VPN, private DNS, mobile security filters, restricted Wi-Fi networks, and router isolation. If local discovery fails but direct IP works, discovery traffic is blocked. If both local IP and internet host fail on only one phone, focus on that phone's network settings.

### Updater download succeeds but install fails
On desktop, the updater must be present beside the game and must be able to write to the installation folder. If the game is inside a protected folder, the updater may not be able to replace files.

On Android, the downloaded file must be an APK, and Android must allow the install source. Grant install-unknown-apps permission when prompted. If installation still fails, delete the downloaded package and download again in case the previous download was incomplete.

### Custom content not showing
For tracks, check that the file ends in `.tsm` and is inside `Tracks`. For vehicles, check that the file ends in `.tsv` and is inside `Vehicles`.

If the folder and extension are correct, the file itself may be invalid. Test with a known-good custom file. If the known-good file appears and yours does not, the problem is in the custom file's contents.

### Controller input feels wrong
If a controller, wheel, or pedal setup feels wrong, start with dead zone and pedal direction settings. A wheel may not rest exactly at center, and some pedals report their values backwards.

Change one setting, drive the same short track section, then change another setting only if needed. Do not tune steering, throttle, brake, and clutch all at once, because that makes the result impossible to judge.

### Motion steering feels unstable
Motion steering uses the phone's orientation. Hold the phone the way you naturally play, then make small steering movements. Do not start by exaggerating the tilt.

If the car drifts when the phone feels centered, recalibrate or disable motion steering and use gesture steering. Some phones have better motion sensors than others. The game prefers Game Rotation Vector, then falls back to less ideal sensors if needed.

### Dedicated server has no command prompt
The server command interface needs standard input. If the server is running as a background service, inside a restricted host, or with input detached, commands may be disabled.

When commands are unavailable, administer the server through settings files, logs, and controlled restarts. If you need live commands, run the server in a normal interactive console.

### Port checklist
The default gameplay port is 28630. The default LAN discovery port is 28631.

For local testing, allow the server program through the firewall. For internet hosting, forward the gameplay port to the server machine and allow it in the firewall. Only forward the discovery port if you specifically know you need it for your network setup.

### Settings parse warning on startup
If the game reports a settings warning on startup, it means a saved setting was missing, invalid, or outside the allowed range. The game usually replaces invalid values with safe defaults so it can continue running.

Read the warning, open the related settings menu, and set that option again. If many settings are broken, move the settings file aside and let the game create a new one, then reapply your preferred options gradually.

## 23. Frequently Asked Questions
### Is Top Speed keyboard-only?
No. Desktop supports keyboard and controller input. Android is designed around touch gestures and can also use motion steering if the device supports the required sensors. A keyboard still works on Android if one is attached, but Android play should not depend on it; the keyboard is optional there.

### Can I play multiplayer without hosting my own server?
Yes. If a community server is available and reachable from your network, you can join it. You only need to host your own server if you want to run a private session, test locally, or maintain a public server yourself.

### Can I remap controls?
Yes. Driving controls can be mapped for keyboard and controller. Menu shortcuts can also be mapped for keyboard. Some platform or safety actions may stay fixed, and Android gestures are handled separately from keyboard mapping.

Custom gesture mapping on Android is not currently supported and is unlikely to be added in the near future.

### Why are some gestures limited to specific parts of the screen?
During a race, touch controls are split into zones so the same gesture can mean different things depending on where it starts. This is necessary because driving needs continuous controls, while race information needs quick gestures that should not interfere with throttle, brake, steering, clutch, or horn.

### Can I disable startup logo and automatic update checks?
Yes. Open Settings → Game settings; both "Play logo at startup" and "Check for updates on startup" live there.

### I changed language, but one menu still sounds untranslated. Is this normal?
It can happen while testing a language file, especially if menus were already opened before the new language was selected. Restart once after switching language. If the same text remains English after restart, the language file needs those strings added or updated.

### Is custom content supported in random selection?
Yes, but only if the matching custom randomization option is enabled. Otherwise random selection uses built-in content even when custom files are present.

### Where are track and vehicle authoring rules documented?
Use the dedicated track and vehicle creation guides. You can find them in the "docs" folder that ships with the game. This guide is for playing, installing, hosting, and troubleshooting; the authoring guides explain the file formats.

## 24. Control Reference
This reference includes every supported action, along with its hotkey on keyboard, or the equivalent gesture on mobile if available.

### 24.1 Menu Navigation
Move to previous menu item:
Desktop: Up Arrow.
Mobile: Swipe left.

Move to next menu item:
Desktop: Down Arrow.
Mobile: Swipe right.

Activate current menu item:
Desktop: Enter or Numpad Enter.
Mobile: Swipe up.

Go back from current menu:
Desktop: Escape.
Mobile: Swipe down with one finger.

In multiplayer lobby, this gesture only works in the bottom zone of the screen, because the top zone is reserved for chat and history features.

During a race, going back only works when you swipe down from the top left zone of the screen.

Otherwise, swiping down from anywhere works, unless stated explicitly.

Jump to first item in menu:
Desktop: Home.
Mobile: Two-finger swipe up when focused item is not a slider.

Jump to last item in menu:
Desktop: End.
Mobile: Two-finger swipe down when focused item is not a slider.

Adjust radio button or navigating between item actions:
Desktop: Left or Right Arrow.
Mobile: Two-finger swipe left or two-finger swipe right.

Decrease slider by one step:
Desktop: Left Arrow.
Mobile: Two-finger swipe left.

Increase slider by one step:
Desktop: Right Arrow.
Mobile: Two-finger swipe right.

Decrease slider by 10 steps:
Desktop: Page Down.
Mobile: Two-finger swipe down.

Increase slider by 10 steps:
Desktop: Page Up.
Mobile: Two-finger swipe up.

Jump to the minimum value in a slider:
Desktop: End.
Mobile: Three-finger swipe down.

Jump to the maximum value in a slider:
Desktop: Home.
Mobile: Three-finger swipe up.

Repeat current usage hint on demand:
Desktop: Space.
Mobile: Long press.

Next screen inside current menu:
Desktop: Tab.
Mobile: No global default gesture for this action.

Previous screen inside current menu:
Desktop: Shift+Tab.
Mobile: No global default gesture for this action.

### 24.2 Main Menu and Startup
Skip startup logo:
Desktop: Enter or Numpad Enter.
Mobile: Swipe up with one finger.

Exit game from root menu:
Desktop: Activate Exit Game item, or Escape from root when no deeper stack exists.
Mobile: Activate Exit Game item, or back out from root menu.

### 24.3 Driving Core Intents
A few things to note for mobile:

1. The screen on mobile during driving is divided into 2 essential parts. The names in the guide follow the race gesture zones, but the phone is meant to be held in landscape mode. In that position, these areas are felt as left and right parts of the phone.

The bottom part, which becomes the right side in landscape mode, is where you control the vehicle, such as starting the engine, throttling, braking, shifting gears, or steering if you are not using your phone's motion sensor for steering. This is refered to as the vehicle control zone in the guide.

The top part, which becomes the left side in landscape mode, contains information and auxiliary race actions. This area is split into top-left and top-right race gesture zones in the reference below. Each one contains different actions.

All drag directions in this driving section are landscape directions. Drag left means move your finger toward the physical left side of the phone while it is sideways. Drag right means move your finger toward the physical right side of the phone.

Steer left:
Desktop: Left Arrow by default.
Mobile: If motion steering is disabled, drag left with one finger in the vehicle control zone. If motion steering is enabled, tilt the phone left from your calibrated neutral position.

Steer right:
Desktop: Right Arrow by default.
Mobile: If motion steering is disabled, drag right with one finger in the vehicle control zone. If motion steering is enabled, tilt the phone right from your calibrated neutral position.

Throttle:
Desktop: Up Arrow by default.
Mobile: Drag up with one finger in the vehicle control zone.

Brake:
Desktop: Down Arrow by default.
Mobile: Drag down with one finger in the vehicle control zone.

Clutch:
Desktop: Shift by default.
Mobile: Hold one finger in the top-left information zone. The clutch stays engaged while your finger remains down, and releases when you lift it.

Shift gear up:
Desktop: A by default (remappable).
Mobile: Two-finger swipe left in the vehicle control zone.

Shift gear down:
Desktop: Z by default.
Mobile: Two-finger swipe right in the vehicle control zone.

Horn:
Desktop: Space by default (remappable).
Mobile: Hold one finger in the top-right information zone. The horn sounds only while your finger remains down, and stops when you lift it.

Start or stop engine:
Desktop: Enter by default.
Mobile: Double tap with one finger in the vehicle control zone.

Pause or resume race:
Desktop: P by default.
Mobile: Three-finger triple tap in the top-left information zone. This means tapping three times while using three fingers.

Toggle shift-on-demand:
Desktop: M (fixed default shortcut).
Mobile: No dedicated gesture.

Request a pit stop (Pit this time):
Desktop: I by default (remappable).
Mobile: No dedicated gesture.
This arms a pit stop for the next time you reach the pit entry area. When the pit menu opens, choose Refuel, Tires, or Both, or press 1, 2, or 3 respectively as a shortcut while the menu is open. Only applies when fuel consumption or tire wear is enabled, and only on tracks that have a pit area. See section 10.6.

### 24.4 Driving Information Requests
Speak current gear:
Desktop: Q by default.
Mobile: Three-finger tap in the top-right information zone. This is a single tap using three fingers.

Speak current lap number:
Desktop: W by default.
Mobile: Double tap with one finger in the top-right information zone.

Speak current race percentage:
Desktop: E by default.
Mobile: Two-finger double tap in the top-left information zone.

Speak current lap percentage:
Desktop: R by default.
Mobile: Three-finger double tap in the top-left information zone.

Speak current race time:
Desktop: T by default.
Mobile: Two-finger triple tap in the top-left information zone. This means tapping three times with two fingers.

Report distance traveled:
Desktop: C by default.
Mobile: Two-finger double tap in the top-right information zone.

Report speed, RPM, and horsepower:
Desktop: S by default.
Mobile: Double tap with one finger in the top-left information zone.

Report fuel remaining:
Desktop: X by default (remappable).
Mobile: No dedicated gesture.
Only meaningful when fuel consumption is enabled. See section 10.6.

Report tire condition:
Desktop: B by default (remappable).
Mobile: No dedicated gesture.
Speaks the current tire state: cold, warming up, optimal temperature, hot, or overheated. Only meaningful when tire wear is enabled. See section 10.6.

Report track name:
Desktop: F9 by default.
Mobile: No dedicated gesture.

Request position information set:
Desktop: Tab by default.
Mobile: Three-finger double tap in the top-right information zone.

Select previous player information target:
Desktop: No default desktop key.
Mobile: Two-finger swipe right in the top-right information zone.

Select next player information target:
Desktop: No default desktop key.
Mobile: Two-finger swipe left in the top-right information zone.

Repeat information for the currently selected player:
Desktop: No default desktop key.
Mobile: Two-finger triple tap in the top-right information zone.

Note that player information on desktop uses the number row: press 1 through 9 for players 1 through 9, and 0 for player 10, to hear that player's full information, including vehicle and current position. See section 24.5.

Exit the race:
Desktop: Escape.
Mobile: Swipe down with one finger in the top-left information zone.

### 24.5 Direct Player Keys During Race
During a race you can ask about a specific player by pressing that player's number on the number row. Each key speaks the full information for that player slot, including their vehicle and current race position. This replaces the older layout that used F1 through F8 for vehicle names and the number row only for positions.

Speak full information for player slot 1:
Desktop: 1.
Mobile: No direct equivalent gesture.

Speak full information for player slot 2:
Desktop: 2.
Mobile: No direct equivalent gesture.

Speak full information for player slot 3:
Desktop: 3.
Mobile: No direct equivalent gesture.

Speak full information for player slot 4:
Desktop: 4.
Mobile: No direct equivalent gesture.

Speak full information for player slot 5:
Desktop: 5.
Mobile: No direct equivalent gesture.

Speak full information for player slot 6:
Desktop: 6.
Mobile: No direct equivalent gesture.

Speak full information for player slot 7:
Desktop: 7.
Mobile: No direct equivalent gesture.

Speak full information for player slot 8:
Desktop: 8.
Mobile: No direct equivalent gesture.

Speak full information for player slot 9:
Desktop: 9.
Mobile: No direct equivalent gesture.

Speak full information for player slot 10:
Desktop: 0.
Mobile: No direct equivalent gesture.

Speak your own player number:
Desktop: F11.
Mobile: No direct equivalent gesture.

### 24.6 Panel and Radio Controls
Switch to next panel:
Desktop: Ctrl+Tab.
Mobile: No dedicated touch gesture.

Switch to previous panel:
Desktop: Ctrl+Shift+Tab.
Mobile: No dedicated touch gesture.

Open radio media file picker:
Desktop: the letter O while radio panel is active.
Mobile: No dedicated touch gesture.

Open radio folder picker:
Desktop: F while radio panel is active.
Mobile: No dedicated touch gesture.

Toggle radio playback:
Desktop: P while radio panel is active.
Mobile: No dedicated touch gesture.

Radio next track:
Desktop: Page Down while radio panel is active.
Mobile: No dedicated touch gesture.

Radio previous track:
Desktop: Page Up while radio panel is active.
Mobile: No dedicated touch gesture.

Radio volume up:
Desktop: Up Arrow while radio panel is active.
Mobile: No dedicated touch gesture.

Radio volume down:
Desktop: Down Arrow while radio panel is active.
Mobile: No dedicated touch gesture.

Toggle shuffle in radio:
Desktop: S while radio panel is active.
Mobile: No dedicated touch gesture.

Toggle looping of the file in the radio:
Desktop: L while radio panel is active.
Mobile: No dedicated touch gesture.

### 24.7 Multiplayer Menu Touch Layout (Android)
Navigate previous item in multiplayer menus:
Desktop: Up Arrow.
Mobile: Swipe left with one finger in the multiplayer bottom zone.

Navigate next item in multiplayer menus:
Desktop: Down Arrow.
Mobile: Swipe right with one finger in the multiplayer bottom zone.

Back out of the multiplayer menu:
Desktop: Escape.
Mobile: Swipe down with one finger in the multiplayer bottom zone.

Open the history buffer screen (chat, server messages, room events):
Desktop: Tab to move forward to the next screen, Shift+Tab to move back. The history buffer is the second screen of the multiplayer lobby and the in-room controls.
Mobile: The history buffer lives in the top zone of the multiplayer menu; there is no separate screen to switch to.

Switch to next history category:
Desktop: `]` (right bracket).
Mobile: Swipe up with one finger in the multiplayer top zone.

Switch to previous history category:
Desktop: `[` (left bracket).
Mobile: Swipe down with one finger in the multiplayer top zone.

Move to next history item:
Desktop: `.` (period) anywhere in multiplayer menus, or Down Arrow on the history buffer screen.
Mobile: Swipe right with one finger in the multiplayer top zone.

Move to previous history item:
Desktop: `,` (comma) anywhere in multiplayer menus, or Up Arrow on the history buffer screen.
Mobile: Swipe left with one finger in the multiplayer top zone.

Jump to the last history item in the current category:
Desktop: Shift+. (Shift + period).
Mobile: No dedicated gesture; use the menu navigation on the history buffer screen to jump to the end.

Jump to the first history item in the current category:
Desktop: Shift+, (Shift + comma).
Mobile: No dedicated gesture; use the menu navigation on the history buffer screen to jump to the start.

Copy the focused history item to the clipboard:
Desktop: Ctrl+Space, or activate the focused entry on the history buffer screen (Enter).
Mobile: Activate the focused entry on the history buffer screen.

Check ping:
Desktop: F1.
Mobile: Double tap with one finger in the multiplayer top zone.

Open global chat input:
Desktop: Slash (`/`) shortcut.
Mobile: Two-finger swipe right in the multiplayer top zone.

Open room chat input:
Desktop: Backslash (`\`) shortcut while in room.
Mobile: Two-finger swipe left in the multiplayer top zone.

View current room game rules:
Desktop: R while inside a room.
Mobile: No dedicated gesture.

### 24.8 Multiplayer Race Overlay Controls
Open global chat during multiplayer race:
Desktop: Slash (`/`).
Mobile: No dedicated race gesture.

Open room chat during multiplayer race:
Desktop: Backslash (`\`).
Mobile: No dedicated race gesture.

Open quit prompt during multiplayer race:
Desktop: Escape.
Mobile: Swipe down with one finger in the top-left information zone. This is the race back-out gesture.

In addition to the controls above, the inline history shortcuts (`,` `.` `Shift+,` `Shift+.` `[` `]` `Ctrl+Space`) and the core communicator shortcuts (Ctrl+Shift+C, Ctrl+Shift+F, F, V, Ctrl+Shift+V) keep working during a multiplayer race. Use them to scroll back through chat or talk to other players without leaving the driving panels. The streaming media shortcuts (Ctrl+O, Ctrl+F, Ctrl+P, Ctrl+L, Ctrl+S, Ctrl+Page Up / Page Down, Ctrl+Up / Down) are menu-only and do not respond during a race.

### 24.9 Communicator Controls
These controls are available whenever you are in the multiplayer lobby or inside a room. The core communicator controls (toggle, set frequency, announce frequency, push to talk, voice activation) also stay active during a multiplayer race, so you can talk to other players without leaving the driving panels. The streaming media controls listed at the end of this section only respond from the multiplayer menus, not during a race. Section 13 explains the feature itself.

Toggle communicator on or off:
Desktop: Ctrl+Shift+C.
Mobile: Two-finger swipe down in the multiplayer top zone.

Open frequency input (set frequency):
Desktop: Ctrl+Shift+F.
Mobile: Two-finger swipe up in the multiplayer top zone.

Announce current frequency:
Desktop: F.
Mobile: Three-finger tap in the multiplayer top zone.

Push to talk (hold while speaking):
Desktop: V. PTT explicitly ignores key presses while Ctrl, Shift, or Alt are held.
Mobile: Single-finger tap in the multiplayer top zone, then within about 0.4 seconds press and hold a single finger in the same zone. Release the held finger to stop transmitting.

Toggle voice activation (VOX) on or off:
Desktop: Ctrl+Shift+V.
Mobile: Two-finger double tap in the multiplayer top zone.

Load a single audio file for streaming through the communicator:
Desktop: Ctrl+O while the communicator is on.
Mobile: No dedicated gesture.

Load a folder for streaming through the communicator:
Desktop: Ctrl+F while the communicator is on.
Mobile: No dedicated gesture.

Play or pause the streaming media:
Desktop: Ctrl+P while the communicator is on.
Mobile: No dedicated gesture.

Previous streaming track:
Desktop: Ctrl+Page Up while the communicator is on.
Mobile: No dedicated gesture.

Next streaming track:
Desktop: Ctrl+Page Down while the communicator is on.
Mobile: No dedicated gesture.

Streaming media volume up:
Desktop: Ctrl+Up Arrow while the communicator is on.
Mobile: No dedicated gesture.

Streaming media volume down:
Desktop: Ctrl+Down Arrow while the communicator is on.
Mobile: No dedicated gesture.

Toggle streaming media loop:
Desktop: Ctrl+L while the communicator is on.
Mobile: No dedicated gesture.

Toggle streaming media shuffle:
Desktop: Ctrl+S while the communicator is on.
Mobile: No dedicated gesture.

All communicator shortcuts on desktop can be remapped from Settings → Controls → Map menu shortcuts. The mobile gestures cannot be remapped.

## 25. Credits and Acknowledgements
Top Speed Remake continues the original open-source Top Speed project while rebuilding the game on a modern C# codebase.

Project code is distributed under GNU GPL v3. Third-party components keep their own upstream licenses.

Some assets were used from some open-source projects which are listed below.

Some menu sounds were used from the open-source Three-D-Velocity game which you can find [here](https://github.com/munawarb/Three-D-Velocity). Some early code was also taken from the project, but not directly.

All sounds and tracks were copied directly from the original Top Speed project and obtained legally as they are on GitHub [here](https://github.com/PlayingintheDark/TopSpeed).

This project exists because of long-term community effort: players, translators, server hosts, testers, and contributors who provide clear reports and repeatable test steps.

Thanks to anyone who helped in any way to keep this project going, whether with financial support, helping with bugs, new features, or sharing the game with their friends. I am truly thankful for the wonderful community that supports the project and keeps it going.

## 26. Contact me
If you wish to report bugs, please open new issues on [GitHub](https://github.com/diamondStar35/top_speed) directly so we can track the issue.

Please make sure you are using the latest version of the game, and be sure to include the game version, the operating system, and any other details when reporting issues. Do not open issues to suggest new features; use the discussions instead.

The game also has a community group on Telegram. You can [join the group](https://t.me/top_speed_remake) to discuss about the game, issues, or new features.

If you wish to contact me personally, you may use one of the following ways.

[WhatsApp](https://wa.me/201067573360)

[Telegram](https://t.me/diamondStar35)

Or email me directly: ramymaherali55@gmail.com

If you wish to donate to keep the project going, please use [this PayPal link](https://paypal.me/diamondStar35).

Finally, if you've reached here, I would like to thank you for taking the time to read this long document.

I hope this guide helps you get into the game, understand how it works, and enjoy Top Speed Remake as much as the community has helped bring it back to life.
