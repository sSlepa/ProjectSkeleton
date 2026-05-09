# AI Usage Disclosure

## Tools used

- **Claude Opus 4.7** via Claude Code (CLI / VSCode extension) — agentic coding: ran the code edits, file moves, and git operations directly.

## How it was used

- **Lab 7 + Lab 8 refactor (transcription from lab handouts):** the lab documents provided the target code in code blocks. Claude transcribed and adapted that code into the project, renaming files and wiring the pieces together.
- **Migration to the skeleton fork:** clone, branch, file copy, csproj adjustments, and this disclosure file.
- **Game (Bomberman) — TBD:** sections will be marked inline with `// AI-generated` / `// end AI-generated` as code is added. This file will be updated to list which regions are fully AI-generated vs hand-written or hand-tweaked.

## Files / regions fully AI-generated (current state)

The following files were produced by Claude transcribing from the provided Lab 7 / Lab 8 handouts (the labs publish the target code in code blocks; Claude assembled the files and resolved namespace conflicts):

- `Camera.cs`
- `Engine.cs`
- `Input.cs`
- `GameRenderer.cs`
- `GameWindow.cs`
- `Program.cs`
- `Models/GameObject.cs`
- `Models/RenderableGameObject.cs`
- `Models/SpriteSheet.cs`
- `Models/TemporaryGameObject.cs`
- `Models/PlayerObject.cs`

Files retained from the upstream skeleton (not AI-generated):

- `KeyCodes.cs`, `MouseButton.cs`, `SdlContext.cs`, `TheAdventure.sln`
- `.gitignore` (standard Visual Studio template, extended)

Game-specific code added on top of the lab framework will be tracked here as it is written, with inline markers in source.
