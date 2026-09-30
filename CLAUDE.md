# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A third-person 3v3 arcade soccer game (player team vs COM) built in **Unity 6 (6000.3.6f1)** using URP, Cinemachine, and the new Input System. Features dribbling, passing, shooting, tackling, AI teammates/opponents, a match clock with goal limit, pause/mute, and audio mixing.

## Development Environment

- **Unity Version:** 6000.3.6f1
- **Render Pipeline:** Universal Render Pipeline (URP 17.3.0)
- **Key Packages:** Input System v1.18.0, Cinemachine v2.10.5, TextMesh Pro, AI Navigation
- **Active Input Handling:** Input System only — the legacy `Input.GetKey*` API throws at runtime. Use `Keyboard.current` / `PlayerInput`.

There are no CLI build commands — build and run through the Unity Editor (File > Build Settings, or press Play to test in editor).

### Scenes (`Assets/Scenes/`)

- **`MainMenu.unity`** — title screen (`MainMenu.cs`: Space → `Scene1`, Esc → `ControlsInfo`)
- **`ControlsInfo.unity`** — controls/instructions screen; text source is `Assets/ControlsInfo.txt`
- **`Scene1.unity`** — the match (formerly `SampleScene.unity`)

Scenes loaded by name via `SceneManager.LoadScene` must be listed in Build Settings.

## Architecture

### Script Overview

```
Game (singleton: match clock, goal limit, result, pause/mute, spawns teammates)
  ├── TeamController ×2 (Player / COM side: members, score, goals, passing, player switching)
  │     └── Player (per character: ball carry, shoot, pass, tackle, dribble audio)
  │           ├── HumanPlayer  (reads StarterAssetsInputs; enabled on the controlled character only)
  │           └── AITeammate   (drives every non-controlled character on both teams)
  ├── Ball (attachment, pickup, tackles/steals, respawn)
  └── Goal ×2 (trigger → TeamController.AddGoal, goal text animation)
```

### Core Scripts (`Assets/Scripts/`)

**`Game.cs`** — Match controller (`Game.Instance`)
- Spawns 2 extra teammates per side by cloning `playerTemplate` / `comTemplate` (clones named `<template> Left/Right`)
- Match clock (`matchDuration`) and `goalLimit`; `EndMatch()` shows result text only after any goal text has cleared + `resultDelayAfterGoal`; R restarts once the result is shown
- Pause (P / Esc): `Time.timeScale = 0`, toggles `pausePanel`, `AudioListener.pause` (sources on the `Ambiance` mixer group keep playing via `ignoreListenerPause`), deactivates `PlayerInput`
- Mute (M): toggles `AudioListener.volume` (persists across scene reloads)

**`TeamController.cs`** — One per side (`TeamSide.Player` / `TeamSide.COM`); holds members, score + score UI, goal sound, pass-target selection, and switching the human-controlled character (moves `PlayerInput`, camera, `HumanPlayer`).

**`Player.cs`** — Per-character actions
- `Shoot()` plays "Shoot" on Animator layer 1, kicks after 0.2s with `shootForce` and a 0.2y arc; `Pass()` / `PassTo()` with `passForce`
- `Tackle()` → `Ball.TryTackle`; a miss stuns for `missedTackleStun` (`IsStunned`)
- Looping dribble SFX while carrying and moving
- `DebugName` = `name [side]` for logs

**`HumanPlayer.cs`** — Shoot button shoots with the ball, tackles without it; pass button passes or calls for a pass; switch button changes controlled player. Locks `ThirdPersonController.MovementLocked` while stunned.

**`AITeammate.cs`** — Formation positioning, chasing the ball/carrier, shooting within `shootRange`, passing under pressure. Speed = human sprint × `humanSpeedMultiplier` (read once in `Start`).

**`Ball.cs`** — Ball carry and possession
- Auto-attaches to the nearest non-stunned player within `pickupRadius` (after a 0.5s reattach delay)
- Automatic steal: an opponent within `tackleRadius` takes the ball after `tackleProtection`; `retackleDelay` stops ping-pong
- Button tackle (`TryTackle`): within `tackleReach`, success chance by angle (front / side / behind)
- Follows `"Geometry/BallLocation"` on the carrier; kinematic while carried; respawns if `y < -2`
- `Carrier`, `LastTouchedBy` (for goal credit); logs `STEAL:` to the console

**`Goal.cs`** — Trigger on tag `"Ball"` → `scoringTeam.AddGoal()`, animates goal text (scale 0.5→1.5, fade over 3s), logs `GOAL` / `OWN GOAL` with the scorer, then `Game.ResetAfterGoal()` after 1.5s.

**`MainMenu.cs`** — Menu scene key handling. `StuckDebugger.cs` is a temporary diagnostic added to every Player; `FieldBoundary.cs` respawns the ball when it leaves the field trigger.

### Input Layer (`Assets/StarterAssets/InputSystem/`)

**`StarterAssetsInputs.cs`** — Wraps Input System callbacks into plain fields (`move`, `look`, `jump`, `sprint`, `shoot`, `pass`, `switchPlayer`). `shoot`, `pass`, and `switchPlayer` were added for this project.

**`StarterAssets.inputactions`** — Keyboard/mouse: WASD/arrows move, mouse look, Left Click / Space shoot, Right Click pass, Tab switch. Gamepad: sticks, South = jump, Right Trigger = pass, West = switch (no gamepad shoot binding yet).

### Movement (`Assets/StarterAssets/ThirdPersonController/Scripts/`)

**`ThirdPersonController.cs`** — `CharacterController`-based movement for the controlled character. Always runs at `SprintSpeed` (5.335 m/s); gravity -15, jump height 1.2m. `MovementLocked` (set by `HumanPlayer`) freezes movement without dropping held input.

### Mobile Support (`Assets/StarterAssets/Mobile/Scripts/`)

Virtual joystick, button, and touch zone components relay input to `StarterAssetsInputs` via `UICanvasControllerInput`. `MobileDisableAutoSwitchControls.cs` disables auto device-switching on iOS/Android for performance.

## Key Scene Setup Notes

- The soccer ball must have the tag `"Ball"` for goal detection
- Each `Goal` needs its `scoringTeam` (TeamController) and `goalText` assigned
- `Game` needs `ball`, `playerTeam`, `comTeam`, `playerTemplate`, `comTemplate`, `timerText`, `resultText`, and `pausePanel`
- Characters need an `Animator` with a second layer (index 1) for the shoot animation
- Ball respawn position is the ball's position at scene load
- AudioMixer: `Assets/Audio/NewAudioMixer.mixer` with groups Master / Music / SFX / Ambiance and exposed params `volume_master`, `volume_music`, `volume_sfx`, `volume_ambient`
