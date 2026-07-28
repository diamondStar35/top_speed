# Changes

This file tracks new changes to the game for both client and server to make it easier to find previous changes.

The game versioning follows a specific pattern by using year.month.day.revision, where revision is an incremental number if there is more than one release in a single day.


## 2026.7.28.1
### Game Changes
- You can now check your car's status while the pit stop menu is open. Fuel, tire wear, distance, speed, gear and race progress all answer while you decide whether to refuel, change tires, or both. You can also sound the horn during the whole pit stop, including in the pit box and along the pit roads.
- Status keys now keep working after you finish a race while you wait for the rest of the field, instead of going silent. You can also rev your engine while waiting; the car stays in neutral and cannot move.
- Added a "Brief status reports" option in race settings. When turned on, spoken status reports are shortened for players who already know their keys: labels such as "lap percentage" and "gear" are dropped, and the number row gives a quick rundown.
- Added a "Report lap and turn instead of race percentage" option in race settings. The number row and player information then report a car's lap and the turn it is in or approaching, spoken like a spotter, for your own car and any other driver you check.
- Added a "Curve announcement method" option in race settings. The copilot can signal upcoming curves with tones instead of speech. The tighter the curve the higher the tone, and each tone plays from the side the curve turns toward.
- Keys that a menu uses to navigate no longer double as driving keys while that menu is open, so a status key mapped to an arrow key moves the menu without also speaking.
- Improved the localization workflow so translation updates are easier to keep current, and added more Spanish, Brazilian Portuguese and Chinese translations.

### Server Changes
- Improved the localization workflow so translation updates are easier to keep current, and added more Brazilian Portuguese translations, including the pit area and lap limit messages.


## 2026.7.23.1
### Game Changes
- Fixed a bug that could wreck your car for no reason as you crossed the start/finish line or pulled out of the pit lane, most often during longer races. For a single frame the road shifted a full track-width sideways and the game counted you as off the track.

### Server Changes
- Fixed the same start/finish line glitch for the computer-controlled cars, which could spin them out for no reason as they completed a lap.


## 2026.7.17.1
### Game Changes
- Fixed a crash that could happen when starting a race with a custom vehicle that was missing one of its sound files. The vehicle now stays playable: any missing sound is replaced with a built-in default (or, for optional sounds, simply left silent), and the custom vehicles menu shows a warning describing what was missing.


## 2026.7.15.1
### Game Changes
- Fixed a single race bug where the computer cars could still bump into you after you had already finished the race, such as while waiting on the finish line with your engine running. Being hit could leave your car sliding out of your control and could delay the end of the race.
- Removed some unused sound files that were bundled with the non-English language packs.

### Server Changes
- Raised the minimum supported network protocol to the 500-lap update, so game clients older than that update are now rejected at connection instead of mis-reading race data. This corrects an oversight where the 500-lap protocol change did not raise the minimum.


## 2026.7.13.1
### Game Changes
- Added support for up to 500 laps.

### Server Changes
- Updated the network protocol to support the higher lap count.


## 2026.7.12.1
### Game Changes
- Cars now consume fuel over the course of a race. Press X to hear how much fuel you have left, and a low-fuel warning alerts you when you are running low. Be careful not to run out, or you won't be able to finish the race.
- Tires now heat up, wear down, and lose grip as a race goes on. Press B to hear their current condition: cold, warming up, optimal temperature, hot, or overheated.
- You can now make a pit stop to refuel and/or change tires. Press I to request a stop, then choose Refuel, Tires, or Both from the menu the next time you reach the pit entry area, or press 1, 2, or 3 to quickly pick the service you want.
- Fuel consumption and tire wear are optional. For single race and time trial, toggle them under Options, Race settings ("Enable fuel consumption" and "Enable tire wear"). For multiplayer, the host controls them through the race rules. Either way, races play the same as before when they are left off.
- Filled in missing race-announcement audio: voice for players 9 and 10, finishing positions 8 and 9, live "you are in 8th/9th" position callouts, and "finished last" / "you are last" callouts so the final racer is always announced correctly no matter how many are in the field.
- The F1 through F8 keys for player information have been replaced with the number row keys, which now speak all the information about each player.
- Added Brazilian Portuguese (pt-BR) voice audio.
- Added a Persian translation.
- Improved how track callouts are announced by separating them from other race information.
- Fixed a multiplayer bug where a race would never finish if an earlier race in the session had been aborted.
- Fixed a hard crash that could happen when a vehicle's data file was missing its engine RPM values.
- Fixed multiplayer track names not being translated.
- Fixed incorrect Chinese wording on the race results screen and corrected several other dynamic-text translation issues.
- On Android, the game now prefers the Android text-to-speech voice first (including in automatic mode) and clears leftover update files on startup.

### Server Changes
- Added support for the fuel consumption and tire wear race rules, broadcasting the effective rules for each race so clients set up their cars correctly.
- Updated the network protocol for the fuel, tire wear, and pit stop features. Clients and servers must both be on this release to play together.


## 2026.5.14.1
### Game Changes
- Added a full voice chat system to the game. Any player who is connected to a server can enable their communicator by pressing ctrl+shift+c to listen to other players, and either holding v or ctrl+shift+v to talk.
- The communicator has a frequency, between 0.0 and 1000.0. The default public frequency is 1.0 which is by default all players are tuned to. You can read the current frequency by pressing f, and change it by pressing ctrl+f.
- There are new settings in the audio to choose the default voice input device and Microphone gain.
- Added a new category in the volume settings for communicator. This controls the loudness of communicator sounds as well as other players. This does not affect the radio.
- You now have the ability to stream files anywhere using your communicator by pressing ctrl+f to load a folder, or ctrl+o to load a file, then playing it with ctrl+p. Shortcut keys are similar to the radio, except adding ctrl with the key. For example, toggle loop is ctrl+l.
- Added a new quicker way of controling different volume categories, by pressing f6 and shift+f6 to switch between different categories, and f7, f8 + adding shift with those keys control the actual volume.
- Added proxy support to the game when downloading updates or external requests.


### Server Changes
- Added voice chat support.
- Added a new flag to control voice chat on the server level.


## 2026.5.9.2
### Game Changes
- Fixed multiplayer voice chat: remote players could not hear each other at all. The communicator now works in the multiplayer lobby in addition to inside rooms. Anyone tuned to the same communicator frequency hears the transmission regardless of which room (or no room) they are in.
- Removed the leftover `TOPSPEED_VOICE_DEBUG` opt-in voice-chat tracing introduced while diagnosing the regression above.

### Server Changes
- Voice chat is now relayed to every connected player on the server (filtered client-side by communicator frequency) instead of being scoped to a single room, so voice works in the lobby and across rooms.


## 2026.5.9.1
### Game Changes
- Fixed the in-vehicle radio in multiplayer crashing when a track finishes and loops back to the start (notably with FLAC files). The fix is in the SoundFlow native FFmpeg wrapper: tail-of-stream codec/demuxer hiccups are now reported as graceful end-of-stream instead of as fatal decoder errors, so the radio source's `Seek(0)`+retry path recovers cleanly.


## 2026.5.5.1
### Game Changes
- Fixed many bugs with the multiplayer server.
- Added a new way of navigating through message history by using the comma to move to the previous item, period to move to the next item, and left/right brackets are used to navigate between buffers. The separate history screen is still available.
- Added an ability to copy the current buffer item to the clipboard by pressing ctrl+space, or by going to the history and pressing enter on any message there.
- Added the ability to reset menu shortcuts to their defaults.


### Server Changes
- Fixed many bugs related to server connection and room deadlocks where players were being stuck in a room after joining multiple times.


## 2026.5.4.3
### Game Changes
- Added the ability to choose which modifier keys are being used when you remap a key in the game. This allows you to either use both modifiers, or the left/right.
- Fixed a critical crash with ZDSR by disabling CET compatibility. The game should no longer crash again when ZDSR is installed.
- Fixed some critical crashes when discovering local servers on the network.
- Android version now runs in landscape mode.


### Server Changes
- Fixed a regression where protocol version mismatches did not trigger a hard fail.

## 2026.5.4.2
This is a hot fix for Android arm 32 and Mac.

## 2026.5.4.1

### Game Changes

* Fixed many crashes that could happen randomly due to audio processing for invalid audio buffers.
* Added Spanish translation for copilot and race announcements.
* Added support for Mac ARM-64 and Android arm-32 (ARM-v7) builds.
* Added support for uploading your custom tracks to the server.
* Android builds now use a permenant signature and no longer conflicts with existing versions.


### Server Changes

* Refactored server and made race finish events much more reliable.
* Added reconnect support, when a player loses connection suddenly, there is now a 20 seconds reconnect period before fully disposing the player.
* Fixed player randomization.
* Added moderation tools to prevent duplicate names on the server, prevent long names, prevent repeated letters in a name, and control text chat on the server level.
* Added initial support for custom tracks.
* You can now host your own custom tracks on the server, and other people can see them when they enable custom tracks.

