# Soccer Game

A third-person **3v3 arcade soccer game** built in Unity 6. You play a team of three against a computer-controlled (COM) team, with dribbling, passing, shooting, tackling, AI teammates and opponents, a match clock, and a goal limit.

## Features

- **3v3 matches:** you control one player; AI drives your two teammates and the whole COM team
- **Player switching:** hand control to any teammate at any time
- **Ball control:** dribble, pass to the best-placed teammate, shoot, or call for the ball
- **Tackling:** your chance of winning the ball depends on the angle (front beats behind). A missed tackle stuns you briefly
- **Match rules:** matches last 3 minutes, and the first team to 5 goals wins
- **Pause and mute:** crowd ambience keeps playing while the game is paused
- **Audio mixing:** separate Master, Music, SFX, and Ambiance mixer groups
- **Menus:** a main menu and a controls screen

## Controls

| Action | Keyboard / Mouse |
|---|---|
| Move | W A S D / Arrow keys |
| Camera | Mouse |
| Shoot (with the ball) | Left Click / Space |
| Tackle (without the ball) | Left Click / Space |
| Pass / Call for pass | Right Click |
| Switch player | Tab |
| Pause / Resume | P / Esc |
| Mute / Unmute | M |
| Restart (after the match) | R |

**Main menu:** Space starts a match, and Esc opens the controls screen.

Gamepad input partly works (sticks to move, Right Trigger to pass, West button to switch player). Shooting doesn't have a gamepad binding yet.

### Tips

- Tackle from the front. Tackles from behind usually fail.
- A missed tackle leaves you frozen for a moment, so time it well.

## Getting Started

### Requirements

- **Unity 6000.3.6f1** (Unity 6)
- Packages resolve automatically from `Packages/manifest.json`:
  - Universal Render Pipeline 17.3.0
  - Input System 1.18.0
  - Cinemachine 2.10.5
  - TextMesh Pro
  - AI Navigation

### Running the game

1. Clone the repository:
   ```bash
   git clone https://github.com/ChrisOspina/soccergame.git
   ```
2. Open the project folder in Unity Hub with Unity 6000.3.6f1.
3. Open `Assets/Scenes/MainMenu.unity` and press **Play**.

### Building

Use **File > Build Profiles** in the Unity Editor. These scenes must be in the build, in this order:

1. `Assets/Scenes/MainMenu.unity`
2. `Assets/Scenes/ControlsInfo.unity`
3. `Assets/Scenes/Scene1.unity`

## Project Structure

```
Assets/
├── Scenes/          MainMenu, ControlsInfo, Scene1 (the match)
├── Scripts/         Gameplay code (see below)
├── StarterAssets/   Third-person controller, input actions, mobile controls
├── Character/       Player models, animations, materials
├── Audio/           Music, SFX, crowd ambience, NewAudioMixer
├── Prefabs/  Materials/  Textures/  Skybox/
└── ControlsInfo.txt Text shown on the controls screen
```

### Scripts

| Script | Role |
|---|---|
| `Game.cs` | Match controller singleton: clock, goal limit, result, pause/mute, and teammate spawning |
| `TeamController.cs` | One per side: members, score, choosing pass targets, switching the controlled player |
| `Player.cs` | Per-character actions: carrying the ball, shooting, passing, tackling, dribble audio |
| `HumanPlayer.cs` | Turns input into actions for the controlled character |
| `AITeammate.cs` | AI for every character you're not controlling: formation, chasing, shooting, passing |
| `Ball.cs` | Possession, pickup, steals, tackle resolution, and respawning |
| `Goal.cs` | Goal detection, scoring, and the goal text animation |
| `FieldBoundary.cs` | Respawns the ball when it leaves the field |
| `MainMenu.cs` / `ControlsInfo.cs` | Menu scene input |

## Credits

- Movement and input are built on Unity's [Starter Assets – Third Person Controller](https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-updates-in-new-charactercontroller-pa-196526)
- Track environment: BEDRILL *Track Environment Free*
- Sound effects and ambience: [freesound community](https://pixabay.com/) via Pixabay
