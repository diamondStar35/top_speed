# Top Speed Track Creation Guide

## Table of Contents

[1. Introduction](#sec-1-introduction)
[2. How to Imagine a Track Shape as an Author](#sec-2-how-to-imagine-a-track-shape-as-an-author)
[3. Folder Layout, File Discovery, and Loading Rules](#sec-3-folder-layout-file-discovery-and-loading-rules)
[4. TSM File Format Rules](#sec-4-tsm-file-format-rules)
[5. Sections and How They Connect](#sec-5-sections-and-how-they-connect)
[6. Detailed Key Reference](#sec-6-detailed-key-reference)
[6.1 Meta Section](#sec-6-1-meta-section)
[6.2 Weather Profile Sections](#sec-6-2-weather-section)
[6.3 Room Sections](#sec-6-3-room-section)
[6.4 Segment Sections](#sec-6-4-segment-section)
[6.5 Sound Source Sections](#sec-6-5-soundid-and-soundtypeid-sections)
[6.6 Value Parsing and Token Rules](#sec-6-6-value-parsing-and-token-rules)
[7. Room Reverb Model and Full Preset Catalog](#sec-7-room-reverb-model-and-full-preset-catalog)
[7.1 Reverb Parameter Meanings](#sec-7-1-reverb-parameter-meanings)
[7.2 Full Built-In Room Presets](#sec-7-2-full-built-in-room-presets-all-66)
[8. Full Example TSM Track File](#sec-8-full-example-tsm-track-file)
[9. Validation and Troubleshooting](#sec-9-validation-and-troubleshooting)
[10. Practical Authoring Workflow](#sec-10-practical-authoring-workflow)

<a id="sec-1-introduction"></a>
## 1. Introduction

This guide documents the current custom track format used by Top Speed and is based on the current parser and runtime code paths. It is written as an authoring reference, not a marketing overview, so the goal is practical correctness.

A custom track file controls four things at once: the road shape and surface flow, the active weather model and its transition behavior, the acoustic room profile applied while driving through segments, and optional custom sound sources placed on the track. Each of those systems is linked through ids and validated before the track is accepted.

This means track authoring is not only about writing valid keys. You also need to understand which keys are actually consumed by runtime, which keys are only metadata, how clamping changes extreme values, and which references must exist up front.

<a id="sec-2-how-to-imagine-a-track-shape-as-an-author"></a>
## 2. How to Imagine a Track Shape as an Author

The most accurate mental model is a timeline of segments that is replayed in a loop. Segment order is the track order. Segment length is physical distance in meters. Turn type changes how fast the lane center drifts left or right as the player moves through that segment.

`straight` keeps lateral center drift at zero. Left and right curves apply continuous drift with different strengths. Because drift is evaluated across segment length, long segments create larger accumulated center movement than short segments with the same turn type.

Current curve-strength factors used by runtime are:

- `straight`: `0`
- `easy_left` / `easy_right`: `0.5`
- `left` / `right`: `0.6667`
- `hard_left` / `hard_right`: `1.0`
- `hairpin_left` / `hairpin_right`: `1.5`

Segment width is separate from turn shape. If `width > 0`, that segment uses its own width. If `width <= 0`, runtime falls back to the default lane width. This matters for track feel because the exact same turn sequence can feel forgiving or punishing depending on width profile.

A practical workflow is to first author only segment sequence, length, and width, then drive and tune pacing. Add weather, rooms, and custom sounds after geometry feels right.

<a id="sec-3-folder-layout-file-discovery-and-loading-rules"></a>
## 3. Folder Layout, File Discovery, and Loading Rules

Custom loading accepts either a folder path or a file path. If you pass a folder, loader scans that folder for `*.tsm` and picks the first filename in case-insensitive sort order. If you pass a file path, it must be a `.tsm` file that exists inside an existing folder.

Recommended layout:

```text
Tracks/
  MyTrack/
    track.tsm
    Audio/
      ambience_loop.ogg
      birds_1.ogg
```

Sound paths in sound sections are resolved relative to the folder containing the `tsm` file. They are not resolved from the game root and not resolved from the Sounds asset root. This keeps each custom track self-contained.

Display name resolution for custom tracks follows this order:

1. `meta.name` if defined and non-empty.
2. Parent folder name.
3. Filename without extension.

<a id="sec-4-tsm-file-format-rules"></a>
## 4. TSM File Format Rules

TSM is line-based and supports exactly two statement forms: section headers and key-value assignments.

Section header format:

```text
[kind]
[kind:id]
```

Key-value format:

```text
key = value
```

Inline comments start with `#` and are stripped before parsing. Empty lines are ignored.

Supported section kinds are `meta`, `segment`, `weather`, `room`, and `sound`. `segment`, `weather`, `room`, and `sound` require ids (`[kind:id]`). `meta` does not use an id (`[meta]`).

Unknown section kinds fail validation. Unknown keys in strict sections also fail validation, except extension-style keys that start with `meta` or `metadata`.

Section order is flexible. The parser reads top-to-bottom and finalizes each section object when a new section header starts. Repeating a key in the same section is allowed; the later value overrides the earlier value for that section object.

Key and section-kind parsing is normalized to lower-case, and spaces/hyphens become underscores. IDs are trimmed and matched case-insensitively. ID uniqueness is enforced per section family (`segment`, `weather`, `room`, `sound`), so duplicate ids in the same family are validation errors.

<a id="sec-5-sections-and-how-they-connect"></a>
## 5. Sections and How They Connect

At least one segment section is required. Also, `meta.weather` is required and must point to an existing weather profile id. If either is missing, the track is rejected.

Segments are the backbone and can reference:

- weather profile ids
- room ids or built-in room preset names
- sound source ids

Sound sources can reference segment ids with `start_area` and `end_area`, so segment ids are used by both geometry and audio trigger logic.

Room behavior has two override layers:

- room section values override room preset baseline
- segment room override keys override the selected room only for that segment

Weather behavior also has two layers:

- track-level default profile from `meta.weather`
- optional segment-level weather id with optional blend time (`weather_transition_seconds`)

<a id="sec-6-detailed-key-reference"></a>
## 6. Detailed Key Reference

<a id="sec-6-1-meta-section"></a>
### 6.1 Meta Section

Header syntax for this section is `[meta]`.

`name`
This is the display name used for the custom track in menus and announcements.
Allowed values:
- Any non-empty text value.

`version`
This is optional author metadata for your own revisioning.
It is stored with track data but does not change runtime driving/audio behavior by itself.
Allowed values:
- Any non-empty text value.

`weather`
This is the required default weather profile id for the track.
Allowed values:
- Any existing weather section id.
- Example: if the file contains `[weather:clear]`, then `weather = clear` is valid.
- If the id does not exist, loading fails.

`ambience`
This selects the legacy ambience loop family.
Allowed values:
- `none`
  Disables legacy ambience loop playback.
- `desert`
  Uses the desert ambience loop behavior.
- `airport`
  Uses the airport ambience loop behavior.

Alias values:
- `0`: `none`
- `noambience`: `none`
- `1`: `desert`
- `2`: `airport`

`pit_area`
This controls whether the track has a pit area where cars can stop to refuel and change tires when fuel consumption or tire wear is enabled. It is optional and defaults to true, so every track has a pit area unless you turn it off here.
Allowed values:
- Omitted, or any value other than the false tokens below: the track has a pit area. If you do not define pit entry/exit segments (see section 6.4), the pit area falls back to the start/finish line.
- `false` (also `0`, `no`, `off`): the track has no pit area. Any pit entry/exit segment markers are ignored, and a validation warning is reported. Players cannot pit on such a track, so do not pair it with fuel consumption or tire wear.

<a id="sec-6-2-weather-section"></a>
### 6.2 Weather Profile Sections

Header syntax is `[weather:<id>]`, for example `[weather:clear]`.

`kind`
This picks the base preset for the profile.
When this key is set, runtime first loads the preset values, then applies any explicit numeric keys in the same weather section.
Allowed values:
- `sunny`
  Clear weather baseline with neutral wind and long visibility.
- `rain`
  Rain profile baseline with wet ambience emphasis.
- `wind`
  Wind profile baseline with stronger lateral wind.
- `storm`
  Storm profile baseline with strongest weather intensity.

Alias values:
- `0`: `sunny`
- `1`: `rain`
- `rainy`: `rain`
- `2`: `wind`
- `windy`: `wind`
- `3`: `storm`
- `stormy`: `storm`

`longitudinal_wind_mps`
This is the wind component along the driving axis, in meters per second.
Positive values act like headwind and increase relative air speed; negative values act like tailwind.
Allowed values:
- Any finite numeric value.

`lateral_wind_mps`
This is the crosswind component in meters per second.
It contributes side-air-speed into aerodynamic drag magnitude.
Allowed values:
- Any finite numeric value.

`air_density`
This controls air density in kg/m³ used by aerodynamic force calculations.
Allowed values:
- Any numeric value greater than `0`.

Runtime note: if an invalid non-positive value still reaches constructor-level code, it falls back to `1.225`.

`drafting_factor`
This is a direct multiplier on aerodynamic drag in the resistance model.
`1.0` is neutral. Values above `1.0` increase drag. Values below `1.0` reduce drag.
Allowed values:
- Any numeric value greater than `0`.

Runtime note: constructor-level clamping enforces a floor of `0.1`.

`temperature_c`
This stores weather temperature in Celsius for profile state.
Current driving resistance logic does not directly depend on this key, but it remains part of the weather profile data.
Allowed values:
- Any finite numeric value.

`humidity`
This stores relative humidity as a normalized ratio.
Allowed values:
- Any numeric value from `0` to `1`.

Runtime note: values are clamped to `0..1` in weather profile construction.

`pressure_kpa`
This stores atmospheric pressure in kPa for the profile.
Allowed values:
- Any numeric value greater than `0`.

Runtime note: if invalid non-positive input reaches constructor-level code, it falls back to `101.325`.

`visibility_m`
This stores visibility distance in meters for weather profile state.
Allowed values:
- Any numeric value greater than `0`.

Runtime note: if invalid non-positive input reaches constructor-level code, it falls back to `20000`.

`rain_gain`
This sets rain-loop weather intensity gain for the profile.
Allowed values:
- Any numeric value greater than or equal to `0`.

Runtime note: profile stores `0..4` after clamping; playback path currently applies final `0..1` clamp before source volume.

`wind_gain`
This sets wind-loop weather intensity gain for the profile.
Allowed values:
- Any numeric value greater than or equal to `0`.

Runtime note: profile stores `0..4` after clamping; playback path currently applies final `0..1` clamp before source volume.

`storm_gain`
This sets storm-loop weather intensity gain for the profile.
Allowed values:
- Any numeric value greater than or equal to `0`.

Runtime note: profile stores `0..4` after clamping; playback path currently applies final `0..1` clamp before source volume.

Weather switching behavior:
When a segment sets `weather = some_id`, runtime blends toward that profile.
Blend speed is controlled by segment key `weather_transition_seconds`.

<a id="sec-6-3-room-section"></a>
### 6.3 Room Sections

Header syntax is `[room:<id>]`, for example `[room:tunnel_a]`.

`name`
This is an optional readable label for the room profile.
Allowed values:
- Any non-empty text value.

`room_preset`
This selects the built-in preset baseline used to initialize room acoustics.
Any numeric room keys in the same section override that baseline.
Allowed values:
- Any preset id listed in Section 7.2.

`reverb_time`
This controls total decay duration of the room tail in seconds.
Allowed values:
- Any numeric value.
- Runtime clamps to non-negative.

`reverb_gain`
This controls overall reverb return level for the room.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1`.

`hf_decay_ratio`
This controls how fast high frequencies decay compared to low/mid frequencies in the reverb tail.
Lower values darken quickly. Higher values preserve brightness longer.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1`.

`late_reverb_gain`
This controls the gain of the late-tail component (late reflections).
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1`.

`diffusion`
This controls reflection density and smoothness.
Lower values sound more discrete/grainy; higher values sound smoother and denser.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1`.

`air_absorption`
This controls how much high-frequency content is lost as sound travels through air.
Lower values keep sources brighter at distance. Higher values make distant sources duller faster.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1`.

`occlusion_scale`
This controls how strongly obstacles reduce the direct sound path.
Lower values make obstacles less blocking. Higher values make obstacles more blocking.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1`.

`transmission_scale`
This controls how much sound leaks through boundaries such as walls or heavy structures.
Lower values make through-wall sound weaker and more blocked. Higher values let more sound pass through.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1`.

`occlusion_override`
This is an optional direct value for occlusion.
When set, it overrides the derived occlusion amount for the room/profile path.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1` when set.

`transmission_override`
This sets one transmission value and applies it to all three frequency bands (low, mid, high).
Use this when you want one global transmission override instead of per-band tuning.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1` when set.

`transmission_override_low`
This sets low-band transmission override.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1` when set.

`transmission_override_mid`
This sets mid-band transmission override.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1` when set.

`transmission_override_high`
This sets high-band transmission override.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1` when set.

`air_absorption_override`
This sets one air-absorption override and applies it to low, mid, and high bands together.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1` when set.

`air_absorption_override_low`
This sets low-band air-absorption override.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1` when set.

`air_absorption_override_mid`
This sets mid-band air-absorption override.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1` when set.

`air_absorption_override_high`
This sets high-band air-absorption override.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1` when set.

Segment reference behavior:
A segment can reference a room section id or preset id.
If segment-level room overrides are present, those apply after the selected room baseline.

<a id="sec-6-4-segment-section"></a>
### 6.4 Segment Sections

Header syntax is `[segment:<id>]`, for example `[segment:s12]`.

`type`
This sets the curve class for the segment.
The selected class affects how road center drifts across this segment as distance advances.
Allowed values:
- `straight`
  No lateral drift.
- `easy_left`
  Gentle left drift.
- `left`
  Medium left drift.
- `hard_left`
  Strong left drift.
- `hairpin_left`
  Very strong left drift.
- `easy_right`
  Gentle right drift.
- `right`
  Medium right drift.
- `hard_right`
  Strong right drift.
- `hairpin_right`
  Very strong right drift.

Alias values:
- `0`: `straight`
- `1`: `easy_left`
- `2`: `left`
- `3`: `hard_left`
- `4`: `hairpin_left`
- `5`: `easy_right`
- `6`: `right`
- `7`: `hard_right`
- `8`: `hairpin_right`

`surface`
This sets segment surface category.
Allowed values:
- `asphalt`
  Standard paved behavior.
- `gravel`
  Loose-surface behavior.
- `water`
  Wet/low-grip behavior.
- `sand`
  Soft-surface behavior.
- `snow`
  Snow-like low-grip behavior.

Alias values:
- `0`: `asphalt`
- `1`: `gravel`
- `2`: `water`
- `3`: `sand`
- `4`: `snow`

`noise`
This sets legacy segment noise behavior.
Allowed values:
- `none`
  No legacy segment noise.
- `crowd`
  Crowd ambience behavior.
- `ocean`
  Ocean ambience behavior.
- `runway`
  Runway/airplane behavior.
- `clock`
  Clock ambience behavior.
- `jet`
  Jet event behavior.
- `thunder`
  Thunder event behavior.
- `pile`
  Pile ambience behavior.
- `construction`
  Construction ambience behavior.
- `river`
  River ambience behavior.
- `helicopter`
  Helicopter event behavior.
- `owl`
  Owl event behavior.

Alias values:
- `0`: `none`
- `nonoise`: `none`
- `off`: `none`
- `1`: `crowd`
- `2`: `ocean`
- `3`: `runway`
- `4`: `clock`
- `5`: `jet`
- `6`: `thunder`
- `7`: `pile`
- `8`: `construction`
- `9`: `river`
- `10`: `helicopter`
- `11`: `owl`

`length`
This sets segment length in meters.
The parser enforces a minimum part length threshold.
Allowed values:
- Any numeric value.
Values below the current parser minimum are warned and clamped.
Default minimum is `50` meters.

`width`
This sets segment width override in meters for this segment.
Allowed values:
- Any numeric value.
- Runtime clamps to `>= 0`.
- Non-positive values fall back to default lane width.

`height`
This stores segment height value.
Allowed values:
- Any numeric value.
- Parsed and stored, not currently applied by active road geometry behavior.

`weather`
This assigns weather profile id for this segment.
Allowed values:
- Existing weather profile ids only.

`weather_transition_seconds`
This sets blend duration when transitioning into this segment's weather profile.
Allowed values:
- Finite numeric values `>= 0`.

`room`
This assigns room/acoustics source for this segment.
Allowed values:
- Existing room section id.
- Built-in room preset id.

Alias keys:
- `room_profile`: `room`
- `room_preset`: `room`

`room_profile`
This is an alias key for `room`.
Allowed values:
- Same as `room`.

`room_preset`
This is an alias key for `room`.
Allowed values:
- Same as `room`.

`sound_sources`
This assigns sound source ids directly to this segment.
Allowed values:
- Comma-separated list of existing sound ids.

Alias keys:
- `sound_source_ids`: `sound_sources`

`sound_source_ids`
This is an alias key for `sound_sources`.
Allowed values:
- Same as `sound_sources`.

`pit`
This marks the segment as the pit entry, the pit exit, or both, defining where pit road connects to the track. It only matters when the track has a pit area (see `pit_area` in section 6.1). A single segment can carry both markers by using two `pit` lines in any order.
Allowed values:
- `pit_entry` (also accepted as `pitentry`)
  Cars leave the track and enter pit road at the start of this segment.
- `pit_exit` (also accepted as `pitexit`)
  Cars rejoin the track at this segment, at the position described below.

If you do not mark any pit entry or exit segments, the pit area falls back to the start/finish line.

Pit entry and exit behavior:
- Cars always enter pit road at the start of the pit-entry segment.
- If the pit-exit segment is the same segment as the pit-entry segment, or comes later in the lap, cars rejoin at the end of the pit-exit segment on the same lap.
- As a special case of the above, if the pit-exit segment is the last segment on the lap, cars rejoin at its end, which sits right at the start/finish line. They therefore cross it immediately and begin the next lap, so this layout effectively advances the lap on exit as well.
- If the pit-exit segment comes earlier in the lap than the pit-entry segment, a car that pits drives all the way through the pit and rejoins at the start of the pit-exit segment on the next lap. The car's lap count advances as it comes out, which is expected. Use this layout deliberately when you want the pit lane to carry cars across the start/finish line.

Additional segment note:
Room numeric keys and room override keys can appear directly in segment sections.
When present, they override room acoustics only for that segment.

Simple shaping strategy:

1. Build shape with `type` + `length`.
2. Set consistent `width` band.
3. Add `surface`.
4. Add weather/room transitions.
5. Add sounds last.

<a id="sec-6-5-soundid-and-soundtypeid-sections"></a>
### 6.5 Sound Source Sections

Header syntax supports two forms:

- `[sound:<id>]`
- `[sound_<type>:<id>]`
  In this form, `<type>` can be:
  - `ambient`
  - `static`
  - `moving`
  - `random`

Example: `[sound_random:wind_gusts]` implicitly sets `type = random`.

`type`
This sets sound source behavior type.
Allowed values:
- `ambient`
  Environment-style source behavior.
- `static`
  Fixed source behavior.
- `moving`
  Path/motion-driven source behavior.
- `random`
  Variant-selecting source behavior.

Alias values:
- `0`: `ambient`
- `1`: `static`
- `2`: `moving`
- `3`: `random`

`path`
Primary source file path.
Allowed values:
- Track-relative path only.

Alias keys:
- `file`: `path`

`file`
This is an alias key for `path`.
Allowed values:
- Same as `path`.

`variant_paths`
Additional file variants for selection.
Allowed values:
- Comma-separated track-relative file paths.
- Empty list is invalid.

`variant_source_ids`
Additional variants by referencing other sound source ids.
Allowed values:
- Comma-separated list of existing sound source ids.
- Empty list is invalid.

`random_mode`
Variant refresh mode.
Allowed values:
- `onstart`
  Choose variant once when created/started, then keep it.
- `perarea`
  Refresh variant when area/segment context changes.

Alias values:
- `0`: `onstart`
- `1`: `perarea`

`loop`
Loop playback flag.
Allowed values:
- `true`
  Enabled.
- `false`
  Disabled.

Alias values:
- `1`: `true`
- `0`: `false`
- `yes`: `true`
- `no`: `false`
- `on`: `true`
- `off`: `false`

`spatial`
Enable spatialized source behavior.
Allowed values:
- `true`
  Enabled.
- `false`
  Disabled.

Alias values:
- `1`: `true`
- `0`: `false`
- `yes`: `true`
- `no`: `false`
- `on`: `true`
- `off`: `false`

`allow_hrtf`
Allow HRTF in spatial path.
Allowed values:
- `true`
  Enabled.
- `false`
  Disabled.

Alias values:
- `1`: `true`
- `0`: `false`
- `yes`: `true`
- `no`: `false`
- `on`: `true`
- `off`: `false`

`global`
Keep source globally active without area/segment gating.
Allowed values:
- `true`
  Enabled.
- `false`
  Disabled.

Alias values:
- `1`: `true`
- `0`: `false`
- `yes`: `true`
- `no`: `false`
- `on`: `true`
- `off`: `false`

`volume`
Base source gain.
Allowed values:
- Any numeric value.
- Runtime clamps to `0..1`.

`fade_in`
Fade-in time in seconds.
Allowed values:
- Any numeric value.
- Runtime clamps to non-negative.

`fade_out`
Fade-out time in seconds.
Allowed values:
- Any numeric value.
- Runtime clamps to non-negative.

`crossfade_seconds`
Crossfade time for variant transition.
Allowed values:
- Any numeric value.
- Runtime clamps to non-negative.

`pitch`
Playback pitch factor.
Allowed values:
- Any numeric value.
- Runtime replaces `<= 0` with `1`.

`pan`
Stereo pan value.
Allowed values:
- Any numeric value.
- Runtime clamps to `-1..1`.

`min_distance`
Spatial minimum distance setting.
Allowed values:
- Any numeric value.

`max_distance`
Spatial maximum distance setting.
Allowed values:
- Any numeric value.

`rolloff`
Spatial distance rolloff setting.
Allowed values:
- Any numeric value.

`start_area`
Segment id trigger for activation start.
Allowed values:
- Existing segment ids only.

`end_area`
Segment id trigger for activation end.
Allowed values:
- Existing segment ids only.

`start_position`
Activation start position.
Allowed values:
- Vector text `x,y,z`.

`end_position`
Activation end position.
Allowed values:
- Vector text `x,y,z`.

`position`
Static or anchor position.
Allowed values:
- Vector text `x,y,z`.

`start_radius`
Radius for start position trigger.
Allowed values:
- Any numeric value.

Runtime note: missing or non-positive values fall back to `1` meter at trigger evaluation.

`end_radius`
Radius for end position trigger.
Allowed values:
- Any numeric value.

Runtime note: missing or non-positive values fall back to `1` meter at trigger evaluation.

`speed`
Moving-source speed value.
Allowed values:
- Any numeric value.

Alias keys:
- `speed_meters_per_second`: `speed`

`speed_meters_per_second`
This is an alias key for `speed`.
Allowed values:
- Same as `speed`.

Sound path validation note:
Absolute paths, drive-letter paths, and traversal path segments are rejected.

Runtime activation summary:
If `global = true`, source is active without area/segment checks.
Otherwise activation depends on segment assignment, area spans, and start/end trigger conditions.

<a id="sec-6-6-value-parsing-and-token-rules"></a>
### 6.6 Value Parsing and Token Rules

Numeric parsing is invariant-culture, so decimals must use `.`.  
Enum parsing is case-insensitive and tolerant of spaces/hyphens/underscores in token text.  
Boolean parsing accepts `1/0`, `true/false`, `yes/no`, and `on/off`.

Vector keys must be exactly three comma-separated float values (`x,y,z`).  
Unknown keys are validation errors in strict sections unless key name starts with `meta` or `metadata`.  
Sound path safety rules reject rooted paths, drive letters, and traversal segments (`.` and `..` path components).  
Cross-reference validation runs after parse and ensures referenced weather ids, room ids, segment ids, and sound ids all exist.

<a id="sec-7-room-reverb-model-and-full-preset-catalog"></a>
## 7. Room Reverb Model and Full Preset Catalog

<a id="sec-7-1-reverb-parameter-meanings"></a>
### 7.1 Reverb Parameter Meanings

This section explains each room parameter in plain language.

| Parameter | What it controls | What happens when increased |
|---|---|---|
| `reverb_time_seconds` | How long the reverb tail lasts before fading out. | The space sounds longer and more lingering. |
| `reverb_gain` | Overall strength of the reverb return. | Reflections become louder compared to the dry signal. |
| `hf_decay_ratio` | How long high frequencies survive in the tail relative to the rest. | High-frequency content stays brighter for longer. |
| `late_reverb_gain` | Level of the late, dense tail component. | The late reverb body becomes stronger and more obvious. |
| `diffusion` | How dense/smooth early and late reflections feel. | Reflections sound less discrete and more blended/smooth. |
| `air_absorption` | High-frequency loss caused by distance through air. | Distant sound loses more brightness and clarity. |
| `occlusion_scale` | How strongly obstacles reduce direct sound. | Obstacle blocking effect becomes stronger. |
| `transmission_scale` | How much sound passes through walls/structures. | More through-boundary sound leakage is audible. |

<a id="sec-7-2-full-built-in-room-presets-all-66"></a>
### 7.2 Full Built-In Room Presets

This table uses separate columns for every reverb parameter so values are directly comparable without tuple shorthand.

| Category | Preset | Acoustic character | reverb_time_seconds | reverb_gain | hf_decay_ratio | late_reverb_gain | diffusion | air_absorption | occlusion_scale | transmission_scale |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Outdoor environments | `outdoor_open` | Very open plain with minimal tail. | 0.35 | 0.08 | 0.85 | 0.08 | 0.20 | 0.65 | 0.35 | 0.75 |
| Outdoor environments | `outdoor_field` | Open field, slightly fuller than open plain. | 0.45 | 0.10 | 0.82 | 0.10 | 0.25 | 0.62 | 0.38 | 0.72 |
| Outdoor environments | `outdoor_urban` | Dense urban reflections and stronger return energy. | 0.90 | 0.22 | 0.70 | 0.24 | 0.45 | 0.48 | 0.55 | 0.52 |
| Outdoor environments | `outdoor_suburban` | Mixed suburban reflections with medium tail. | 0.75 | 0.18 | 0.74 | 0.20 | 0.38 | 0.50 | 0.48 | 0.58 |
| Outdoor environments | `outdoor_forest` | Damped highs, foliage-heavy absorption feel. | 0.70 | 0.16 | 0.52 | 0.18 | 0.32 | 0.82 | 0.60 | 0.60 |
| Outdoor environments | `outdoor_mountains` | Large reflected space with longer tail. | 1.80 | 0.34 | 0.52 | 0.36 | 0.42 | 0.42 | 0.55 | 0.52 |
| Outdoor environments | `outdoor_desert` | Dry open terrain with modest reflection body. | 0.55 | 0.12 | 0.76 | 0.12 | 0.26 | 0.58 | 0.42 | 0.66 |
| Outdoor environments | `outdoor_snowfield` | Open but softened highs, snowy damping. | 0.90 | 0.20 | 0.62 | 0.24 | 0.34 | 0.70 | 0.45 | 0.62 |
| Outdoor environments | `outdoor_coast` | Shoreline openness with moderate reflections. | 0.85 | 0.20 | 0.68 | 0.22 | 0.36 | 0.52 | 0.46 | 0.60 |
| Outdoor environments | `outdoor_valley` | Broad valley bloom with extended return. | 1.60 | 0.30 | 0.56 | 0.34 | 0.46 | 0.46 | 0.58 | 0.52 |
| Tunnels | `tunnel_short` | Short tunnel with clear reflections. | 1.10 | 0.48 | 0.62 | 0.52 | 0.72 | 0.22 | 0.80 | 0.32 |
| Tunnels | `tunnel_medium` | Medium tunnel with fuller tail. | 1.80 | 0.60 | 0.56 | 0.62 | 0.78 | 0.20 | 0.86 | 0.26 |
| Tunnels | `tunnel_long` | Long tunnel with sustained late field. | 2.70 | 0.72 | 0.50 | 0.76 | 0.82 | 0.18 | 0.90 | 0.22 |
| Tunnels | `tunnel_concrete` | Hard concrete tunnel signature. | 2.10 | 0.66 | 0.54 | 0.70 | 0.80 | 0.20 | 0.88 | 0.24 |
| Tunnels | `tunnel_brick` | Brick tunnel, warmer and slightly shorter. | 1.70 | 0.58 | 0.58 | 0.62 | 0.76 | 0.22 | 0.84 | 0.30 |
| Tunnels | `tunnel_metal` | Metallic tunnel with bright return energy. | 2.00 | 0.70 | 0.46 | 0.74 | 0.84 | 0.16 | 0.88 | 0.22 |
| Tunnels | `tunnel_stone` | Stone tunnel with heavy late response. | 2.40 | 0.68 | 0.50 | 0.72 | 0.80 | 0.18 | 0.90 | 0.24 |
| Underpasses and bridge structures | `underpass_small` | Tight underpass with quick dense return. | 0.95 | 0.38 | 0.62 | 0.42 | 0.62 | 0.26 | 0.78 | 0.34 |
| Underpasses and bridge structures | `underpass_large` | Larger underpass with longer reflection train. | 1.35 | 0.46 | 0.56 | 0.52 | 0.68 | 0.24 | 0.82 | 0.30 |
| Underpasses and bridge structures | `overhang` | Partial cover, moderate reflection spread. | 0.75 | 0.30 | 0.66 | 0.34 | 0.56 | 0.28 | 0.72 | 0.38 |
| Underpasses and bridge structures | `bridge_truss` | Bridge truss cavity response. | 0.65 | 0.24 | 0.64 | 0.26 | 0.44 | 0.34 | 0.60 | 0.46 |
| Garages and parking spaces | `garage_small` | Small garage with compact reflections. | 0.95 | 0.40 | 0.64 | 0.44 | 0.62 | 0.30 | 0.72 | 0.34 |
| Garages and parking spaces | `garage_medium` | Medium garage with broader tail. | 1.30 | 0.48 | 0.60 | 0.52 | 0.68 | 0.28 | 0.74 | 0.30 |
| Garages and parking spaces | `garage_large` | Large garage with stronger late field. | 1.80 | 0.56 | 0.58 | 0.60 | 0.72 | 0.26 | 0.76 | 0.28 |
| Garages and parking spaces | `parking_open` | Mostly open parking structure. | 0.70 | 0.20 | 0.70 | 0.22 | 0.38 | 0.40 | 0.48 | 0.58 |
| Garages and parking spaces | `parking_covered` | Covered parking with stronger reflections. | 1.20 | 0.44 | 0.60 | 0.48 | 0.64 | 0.28 | 0.72 | 0.34 |
| Garages and parking spaces | `parking_underground` | Underground parking, enclosed and persistent. | 1.90 | 0.62 | 0.54 | 0.66 | 0.76 | 0.22 | 0.84 | 0.24 |
| Warehouses and industrial halls | `warehouse_small` | Small warehouse with clear room response. | 1.10 | 0.38 | 0.62 | 0.42 | 0.66 | 0.30 | 0.70 | 0.34 |
| Warehouses and industrial halls | `warehouse_medium` | Medium warehouse with fuller tail. | 1.70 | 0.50 | 0.56 | 0.56 | 0.74 | 0.28 | 0.74 | 0.30 |
| Warehouses and industrial halls | `warehouse_large` | Large warehouse with long decay. | 2.40 | 0.62 | 0.50 | 0.68 | 0.80 | 0.24 | 0.78 | 0.26 |
| Warehouses and industrial halls | `factory_hall` | Industrial hall with dense reflections. | 2.20 | 0.60 | 0.48 | 0.66 | 0.78 | 0.24 | 0.80 | 0.26 |
| Warehouses and industrial halls | `machine_shop` | Machine room, tighter than warehouse halls. | 1.30 | 0.44 | 0.54 | 0.50 | 0.70 | 0.26 | 0.76 | 0.30 |
| Hangars and transit stations | `hangar_small` | Small hangar with broad reflections. | 2.00 | 0.56 | 0.54 | 0.60 | 0.76 | 0.24 | 0.72 | 0.30 |
| Hangars and transit stations | `hangar_large` | Very large hangar with long reverb field. | 3.10 | 0.68 | 0.48 | 0.74 | 0.82 | 0.22 | 0.76 | 0.26 |
| Hangars and transit stations | `airport_terminal` | Terminal concourse reflection style. | 1.80 | 0.52 | 0.58 | 0.58 | 0.74 | 0.28 | 0.66 | 0.36 |
| Hangars and transit stations | `subway_station` | Deep station with strong enclosed tail. | 2.30 | 0.64 | 0.50 | 0.70 | 0.80 | 0.22 | 0.84 | 0.24 |
| Hangars and transit stations | `rail_station` | Large station, less enclosed than subway. | 1.90 | 0.54 | 0.56 | 0.60 | 0.74 | 0.26 | 0.72 | 0.32 |
| Corridors, basements, and secure spaces | `corridor_short` | Short corridor with quick feedback. | 0.85 | 0.36 | 0.62 | 0.40 | 0.58 | 0.30 | 0.72 | 0.34 |
| Corridors, basements, and secure spaces | `corridor_long` | Long corridor with stretched reflections. | 1.40 | 0.50 | 0.56 | 0.56 | 0.68 | 0.26 | 0.80 | 0.28 |
| Corridors, basements, and secure spaces | `stairwell_concrete` | Concrete stairwell with firm late return. | 1.60 | 0.54 | 0.54 | 0.58 | 0.70 | 0.26 | 0.78 | 0.28 |
| Corridors, basements, and secure spaces | `basement_low` | Low basement, medium enclosed tail. | 1.10 | 0.44 | 0.58 | 0.48 | 0.66 | 0.28 | 0.76 | 0.30 |
| Corridors, basements, and secure spaces | `basement_large` | Large basement with heavier late energy. | 1.90 | 0.58 | 0.52 | 0.64 | 0.76 | 0.24 | 0.82 | 0.26 |
| Corridors, basements, and secure spaces | `bunker` | Heavy enclosed bunker response. | 2.10 | 0.64 | 0.46 | 0.70 | 0.80 | 0.20 | 0.90 | 0.20 |
| Corridors, basements, and secure spaces | `vault` | Highly enclosed vault signature. | 2.60 | 0.72 | 0.42 | 0.78 | 0.84 | 0.18 | 0.92 | 0.18 |
| Halls, arenas, and stadiums | `hall_small` | Small hall with coherent reflections. | 1.10 | 0.40 | 0.64 | 0.44 | 0.70 | 0.30 | 0.68 | 0.34 |
| Halls, arenas, and stadiums | `hall_medium` | Medium hall with fuller density. | 1.70 | 0.52 | 0.58 | 0.56 | 0.78 | 0.28 | 0.72 | 0.30 |
| Halls, arenas, and stadiums | `hall_large` | Large hall with long spacious tail. | 2.70 | 0.62 | 0.50 | 0.66 | 0.82 | 0.24 | 0.78 | 0.26 |
| Halls, arenas, and stadiums | `arena_indoor` | Big indoor arena with high diffusion. | 3.00 | 0.66 | 0.48 | 0.72 | 0.84 | 0.24 | 0.70 | 0.32 |
| Halls, arenas, and stadiums | `stadium_open` | Open stadium, lighter enclosure. | 1.50 | 0.45 | 0.60 | 0.50 | 0.70 | 0.40 | 0.40 | 0.60 |
| Halls, arenas, and stadiums | `stadium_closed` | Closed stadium with extended late field. | 2.80 | 0.64 | 0.50 | 0.70 | 0.82 | 0.28 | 0.68 | 0.34 |
| Rooms and studios | `room_small` | Small room with short focused response. | 0.70 | 0.30 | 0.70 | 0.32 | 0.62 | 0.36 | 0.60 | 0.40 |
| Rooms and studios | `room_medium` | Medium room with balanced body and tail. | 1.10 | 0.40 | 0.62 | 0.42 | 0.70 | 0.30 | 0.62 | 0.34 |
| Rooms and studios | `room_large` | Large room with longer lingering reflections. | 1.80 | 0.50 | 0.54 | 0.54 | 0.76 | 0.26 | 0.68 | 0.30 |
| Rooms and studios | `studio_dry` | Dry studio response with short tail. | 0.35 | 0.12 | 0.78 | 0.14 | 0.40 | 0.50 | 0.40 | 0.70 |
| Rooms and studios | `studio_live` | Live room studio with more bloom. | 0.90 | 0.34 | 0.66 | 0.38 | 0.66 | 0.34 | 0.58 | 0.44 |
| Rooms and studios | `broadcast_booth` | Very dry booth with tight control. | 0.28 | 0.08 | 0.82 | 0.10 | 0.30 | 0.58 | 0.45 | 0.70 |
| Sacred, cavern, canyon, and sewer spaces | `church_small` | Small church with pronounced tail. | 2.40 | 0.56 | 0.46 | 0.60 | 0.82 | 0.26 | 0.72 | 0.30 |
| Sacred, cavern, canyon, and sewer spaces | `church_large` | Large church with long reverberant bloom. | 3.80 | 0.70 | 0.40 | 0.76 | 0.86 | 0.22 | 0.78 | 0.24 |
| Sacred, cavern, canyon, and sewer spaces | `cathedral` | Very large sacred hall, very long decay. | 5.40 | 0.78 | 0.34 | 0.84 | 0.90 | 0.20 | 0.82 | 0.20 |
| Sacred, cavern, canyon, and sewer spaces | `cave_small` | Small cave with dense enclosed reflections. | 2.60 | 0.62 | 0.46 | 0.66 | 0.74 | 0.20 | 0.86 | 0.24 |
| Sacred, cavern, canyon, and sewer spaces | `cave_large` | Large cave with long dark tail. | 4.50 | 0.78 | 0.34 | 0.84 | 0.84 | 0.16 | 0.92 | 0.16 |
| Sacred, cavern, canyon, and sewer spaces | `cave_ice` | Ice cave style with bright lingering return. | 3.80 | 0.72 | 0.40 | 0.80 | 0.80 | 0.24 | 0.86 | 0.22 |
| Sacred, cavern, canyon, and sewer spaces | `canyon_narrow` | Narrow canyon with directional returns. | 2.10 | 0.54 | 0.46 | 0.58 | 0.52 | 0.34 | 0.64 | 0.42 |
| Sacred, cavern, canyon, and sewer spaces | `canyon_wide` | Wide canyon with broader spread and less enclosure. | 2.90 | 0.60 | 0.44 | 0.64 | 0.48 | 0.36 | 0.56 | 0.48 |
| Sacred, cavern, canyon, and sewer spaces | `sewer_brick` | Brick sewer channel with wet enclosed tail. | 2.10 | 0.62 | 0.50 | 0.68 | 0.78 | 0.22 | 0.86 | 0.22 |
| Sacred, cavern, canyon, and sewer spaces | `sewer_concrete` | Concrete sewer with hard reflective body. | 2.40 | 0.68 | 0.46 | 0.74 | 0.82 | 0.20 | 0.88 | 0.20 |

<a id="sec-8-full-example-tsm-track-file"></a>
## 8. Full Example TSM Track File

```ini
[meta]
name = Demo Accessible Track
version = 1
weather = clear
ambience = noambience

[weather:clear]
kind = sunny
longitudinal_wind_mps = 0
lateral_wind_mps = 0
air_density = 1.225
drafting_factor = 1
temperature_c = 22
humidity = 0.45
pressure_kpa = 101.325
visibility_m = 20000
rain_gain = 0
wind_gain = 0
storm_gain = 0

[weather:storm_edge]
kind = storm
longitudinal_wind_mps = 4
lateral_wind_mps = 8
air_density = 1.24
drafting_factor = 1
temperature_c = 17
humidity = 1
pressure_kpa = 99.8
visibility_m = 3500
rain_gain = 0.2
wind_gain = 0.4
storm_gain = 1.0

[room:open_day]
room_preset = outdoor_open

[room:tunnel_a]
room_preset = tunnel_medium
reverb_gain = 0.62
late_reverb_gain = 0.65

[sound_ambient:birds]
path = Audio/birds_loop.ogg
loop = true
volume = 0.5
spatial = false

[sound_random:wind_gusts]
variant_paths = Audio/wind_gust_1.ogg, Audio/wind_gust_2.ogg, Audio/wind_gust_3.ogg
random_mode = perarea
loop = true
volume = 0.4
spatial = true
allow_hrtf = true
fade_in = 0.15
fade_out = 0.15
crossfade_seconds = 0.5
start_area = s2
end_area = s5

[segment:s1]
type = straight
surface = asphalt
noise = none
length = 300
width = 10
room = open_day
sound_sources = birds

[segment:s2]
type = easy_right
surface = asphalt
noise = crowd
length = 220
room = open_day
sound_source_ids = wind_gusts

[segment:s3]
type = hard_right
surface = asphalt
noise = none
length = 180
room = tunnel_a
weather = storm_edge
weather_transition_seconds = 2.5

[segment:s4]
type = straight
surface = asphalt
noise = jet
length = 260
room = tunnel_a

[segment:s5]
type = left
surface = asphalt
noise = none
length = 240
room = open_day
weather = clear
weather_transition_seconds = 1.5
```

<a id="sec-9-validation-and-troubleshooting"></a>
## 9. Validation and Troubleshooting

Track loading has a strict validation phase before runtime objects are built. Errors stop loading. Warnings allow loading but values may be adjusted by clamp rules.

Common hard failures and direct fixes:

| Failure | Why it happens | Fix |
|---|---|---|
| Missing default weather profile | `meta.weather` is empty or points to unknown id | Define `weather = some_id` in `meta` and add `[weather:some_id]` section |
| Duplicate section ids | Two sections in same family reuse same id | Rename ids so each `segment`, `weather`, `room`, and `sound` id is unique |
| Unknown enum token | Value text does not map to a supported enum token/int | Use valid token or numeric enum value from this guide |
| Unknown key in strict section | Misspelled key or unsupported key name | Correct spelling; keep custom tags under `meta*` / `metadata*` prefix |
| Unknown reference id | Segment or sound references id that does not exist | Add missing target section or fix reference id text |
| Invalid sound path | Absolute path, drive-letter path, or traversal path | Use track-relative path only, for example `Audio/wind.ogg` |
| Invalid vector | Position value is not exactly `x,y,z` floats | Write exactly three float values separated by commas |

Important non-fatal behavior:

- Segment lengths below 50 are warned and clamped up to 50.
- Many room and weather gain fields are clamped in runtime constructors.
- `pitch <= 0` for sound sources becomes `1.0`.
- `width <= 0` does not fail; runtime falls back to default lane width.

<a id="sec-10-practical-authoring-workflow"></a>
## 10. Practical Authoring Workflow

A stable workflow is to build in four passes and test after each pass instead of writing all systems at once.

Pass 1 is geometry only: `meta`, one weather profile, and segments with `type`, `length`, `surface`, `width`. Confirm pacing, curve flow, and lap feel.

Pass 2 is weather transitions: add extra weather profiles, then add segment-level `weather` and `weather_transition_seconds` only where transitions are intentional. Confirm transitions are smooth and occur at expected segment boundaries.

Pass 3 is acoustics: add room presets or room sections for specific areas like tunnels and enclosed structures. If needed, use segment room overrides for local tuning instead of duplicating many room sections.

Pass 4 is sound sources: start with one simple source with known good file path, then add spatial behavior, area triggers, and random variants. Validate each sound id reference from segments and area ids from sound triggers.

Keep a companion plain-text sequence that describes segment order, distance, and intent. When behavior is wrong, compare the sequence against runtime behavior to isolate whether the problem is geometry, weather ids, room selection, or sound trigger references.
