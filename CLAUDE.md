# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A third-person soccer/FIFA game prototype built in **Unity 6 (6000.3.6f1)** using URP, Cinemachine, and the new Input System. The game features ball dribbling, shooting, goal detection, scoring, and audio mixing.

## Development Environment

- **Unity Version:** 6000.3.6f1
- **Render Pipeline:** Universal Render Pipeline (URP 17.3.0)
- **Key Packages:** Input System v1.18.0, Cinemachine v2.10.5, TextMesh Pro, AI Navigation
- **Main Scene:** `Assets/Scenes/SampleScene.unity`

There are no CLI build commands — build and run through the Unity Editor (File > Build Settings, or press Play to test in editor).

## Architecture

### Script Dependency Chain

```
StarterAssetsInputs (input abstraction)
    └── ThirdPersonController (movement, camera, animation)
    └── Player (shooting, scoring, audio, UI)
            └── Ball (physics, attachment, respawn)
            └── Goal (trigger detection, score callbacks)
```

### Core Scripts (`Assets/Scripts/`)

**`Ball.cs`** — Ball physics and player attachment
- Auto-attaches to player when within 2.0 units
- Ball follows transform child `"Geometry/BallLocation"` on the player
- Rotates based on movement speed; respawns if `y < -2`
- Exposes `StickToPlayer` bool; notifies `Player` script when attached

**`Player.cs`** — Central game controller
- Reads `StarterAssetsInputs.shoot` (left mouse button) each frame
- Shooting: plays "Shoot" animation on Animator layer 1, applies 20-unit impulse force at 0.2s delay with a 0.2y arc offset
- Tracks both player score and COM (opponent) score; updates TextMeshPro UI
- Manages 4-channel AudioMixer (master, music, SFX, ambient)
- Plays dribble SFX every 1 unit of distance traveled with ball attached
- `IncreaseMyScore()` / `IncreaseOtherScore()` are called by Goal.cs

**`Goal.cs`** — Goal zone trigger and celebration
- Uses `OnTriggerEnter` with tag `"Ball"` to detect goals
- Two goals in scene: `"Goal1"` (player scores) and `"Goal2"` (COM scores)
- Runs a coroutine that scales goal text from 0.5→1.5 and fades it over 3 seconds
- Calls `Player.IncreaseMyScore()` or `Player.IncreaseOtherScore()` accordingly

### Input Layer (`Assets/StarterAssets/InputSystem/`)

**`StarterAssetsInputs.cs`** — Wraps New Input System callbacks into plain C# properties (`move`, `look`, `jump`, `sprint`, `shoot`). The `shoot` action was added for this project and is not part of the original Starter Assets.

**`StarterAssets.inputactions`** — Input binding asset. Keyboard: WASD/arrows, mouse delta, Space, Left Shift, Left Mouse. Gamepad: left stick, right stick, South button, left trigger.

### Movement (`Assets/StarterAssets/ThirdPersonController/Scripts/`)

**`ThirdPersonController.cs`** — Uses `CharacterController` (not Rigidbody) for movement. Walk speed: 2.0 m/s, sprint: 5.335 m/s. Custom gravity: -15.0. Jump height: 1.2m. Grounded check via sphere cast.

### Mobile Support (`Assets/StarterAssets/Mobile/Scripts/`)

Virtual joystick, button, and touch zone components relay input to `StarterAssetsInputs` via `UICanvasControllerInput`. `MobileDisableAutoSwitchControls.cs` disables auto device-switching on iOS/Android for performance.

## Key Scene Setup Notes

- The soccer ball must have the tag `"Ball"` for goal detection to work
- Goal trigger colliders must reference the `Player` component via `Goal.cs` inspector fields
- The `Player` script expects an `Animator` with a second layer (index 1) for the shoot animation
- Ball respawn position is the ball's initial position at scene load (`startPos` captured in `Start()`)
- The AudioMixer must be assigned in the Player inspector for sound to work (known issue: mixer was broken as of commit `6544d5a`)
