# BlockBreaker

BlockBreaker is a Unity 2D arcade game inspired by classic brick-breaker gameplay.

## Features

- Paddle movement with keyboard and gamepad support
- Ball launch, increasing speed, and paddle bounce influence
- Lives system with respawn flow
- Win/lose states with restart support
- Special blocks and falling power-ups:
  - Expand Paddle
  - Shrink Paddle
  - Extra Ball
  - Slow Ball
  - Fast Ball
  - Extra Life
- WebGL build included in `docs/`

## Controls

- Move paddle: `A/D` or `Left/Right Arrow` (also supports gamepad left stick)
- Start launch: `Space` (also gamepad south button)
- Restart after win/lose: `R`

## Project Structure

- `Assets/Scenes/MainScene.unity`: main playable scene
- `Assets/Scripts/`: gameplay scripts (`GameManager`, `BallController`, `PaddleMovement`, etc.)
- `docs/`: generated WebGL build output
- `ProjectSettings/`: Unity project configuration

## Requirements

- Unity Editor with support for this project version (open the project and let Unity resolve packages)
- Input System package enabled (already used by scripts)

## Run Locally (Unity Editor)

1. Open the folder in Unity Hub.
2. Open `Assets/Scenes/MainScene.unity`.
3. Press Play in the Editor.

## Web Build (docs)

This repository includes a WebGL build under `docs/`.

- Live GitHub Pages build for testing:
  - https://estebanr11.github.io/BlockBreaker/

- To serve it locally, use any static server from the project root, for example:

```powershell
python -m http.server 8080
```

Then open `http://localhost:8080/docs/`.

## Gameplay Rules

- Destroy all blocks to win.
- If all balls are lost, one life is consumed.
- The game ends when lives reach zero.
- Press `R` to restart after game over or level completion.

## Notes

- `Library/`, `Temp/`, and `Logs/` are Unity-generated folders and do not contain hand-authored gameplay code.
