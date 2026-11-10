# GenneCaster

GenneCaster is a free, open-source spectating mod for Gorilla Tag, built for casters. It turns the desktop view into a broadcast camera with first-person and orbit spectating, a freecam, nametags, a minimap, a player leaderboard, a closest-lava readout, a two-team scoreboard with a match timer, and Discord Rich Presence. Every feature is configured from in-game panels and saved between sessions, and whole setups can be stored as named presets.

GenneCaster is a fan-made mod. It is not affiliated with or endorsed by Another Axiom.

## Install

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64) into your Gorilla Tag folder and start the game once so BepInEx creates its folders.
2. Download `GenneCaster-<version>.dll` from the [releases page](https://github.com/g3nn3sis/GenneCaster/releases).
3. Install it into the BepInEx plugins folder, it should end up at `Gorilla Tag\BepInEx\plugins\GenneCaster-<version>.dll`.
4. Start the game on PC. The GenneCaster panel opens in the top-left corner.

Each release is built against the Gorilla Tag version that was current when it was published. If a game update breaks the mod, check for a newer release.

## Features

| Panel | What it configures |
| --- | --- |
| GenneCaster | Show the other panels, join a room by code, join a public room, leave the room, and see your connection status |
| Camera Manager | Field of view, near clip, first person or orbit distance, orbit speed, position and rotation smoothing, auto orbit, and one-tap FOV and view presets |
| Player Settings | Microphone mode, audio from the spectator camera, player list updates, Discord presence, VSync, AFK kick, rig interpolation |
| FreeCam | Freecam, mouse click, movement speed, mouse sensitivity |
| Time Manager | Morning, day, evening, night, rain |
| Map Loader | Load any map |
| Player Manager | Your name and color, with a preview |
| NameTag Manager | Nametags, font, player color, FPS, platform icon (Steam or Meta) |
| Theme Loader | Accent color of every panel and the minimap |
| MiniMap Manager | Top-down minimap, size, camera height, dragging, position reset |
| Leaderboard | Bottom-left stack of rounded pills, each showing the keyboard number that spectates that player, their name, an optional FPS readout and their Steam/Meta icon; tagged names are orange, others white, and the spectated player is bold. Enable, scale, position offset, platform icons, FPS, font |
| Closest Lava | Bottom-center rounded box with the distance from the spectated player to the nearest tagged (lava) player; 0 when spectating a tagged player. Enable, scale, position offset, font |
| Scoreboard | Top-center two-team scoreboard with a central match timer, all under the same theme. You set the team names and scores from its panel; the timer runs a 10 second pre-roll (orange, shown negative) then counts up in white and stops at 3:00, with start/pause and reset. Enable, scale, position offset, font |
| Presets | Save the current settings as named presets and load or delete them later; toggle autosave and save settings on demand |

The Leaderboard, Closest Lava and Scoreboard overlays are Unity canvases loaded from an asset bundle embedded in the DLL, so they render at the same size on any resolution. All three share one font, picked from the game's fonts with the Previous/Next Font buttons in any of their panels.

Panels stack themselves into columns until you drag one. Dragged positions are saved, and the **Reset Layout** button in the Universal panel puts every panel back. The interface scales with your resolution, and `UI Scale` in the config adds your own multiplier on top.

## Controls

| Input | Action |
| --- | --- |
| `Esc` | Show or hide the panels (configurable) |
| `0` | Spectate the local player (yourself) |
| `1` to `9` | Spectate the first nine other players |
| `]` and `[` | Spectate the next or previous player (configurable) |
| `V` (held) | Hold to talk in all chat; release returns to the Microphone setting (configurable) |
| `B` `N` `M` | Scoreboard timer: start, pause, reset (configurable) |
| `H` `J` / `K` `L` | Scoreboard: team A score up/down, team B score up/down (configurable) |
| Arrow keys | Orbit the spectated player when Head Distance is above 0 |
| Hold right mouse | Fly the freecam (FreeCam must be on) |
| `W` `A` `S` `D` | Move while flying |
| `Space` and `Left Shift` | Move up and down while flying |
| `E` (held) | Noclip while flying |
| Left click | Press what is under the cursor with your right hand (Mouse Click must be on) |

## Configuration

Settings are saved to `Gorilla Tag\BepInEx\config\com.gennesis.gennecaster.cfg` a second after you change them, and when the game closes. Turn **Autosave** off in the Presets panel to stop that; settings are then written only when you press **Save Settings** or save a preset. The file holds only section headers and `Key = value` lines, with no comments. You can edit it while the game is closed; a value outside a setting's allowed range is clamped when the game loads it.

Presets are stored as `.cfg` files in a `GenneCaster-presets` folder next to the config. Loading one applies it immediately, including the panel theme and layout.

## Troubleshooting

- **No panel appears.** Open `BepInEx\LogOutput.log` and search for `GenneCaster`. If it is missing, BepInEx is not installed correctly or the DLL is not under `BepInEx\plugins`.
- **The camera does not move.** The mod takes over the game's desktop shoulder camera. The log says `The game's shoulder camera was not found` when a game update has moved it.
- **No Discord presence.** Discord must be running before the game starts, and Rich Presence must be on in Player Settings.
- **Something else.** Open an [issue](https://github.com/g3nn3sis/GenneCaster/issues/new/choose) and attach `LogOutput.log`.

## Build from source

You need Windows, a local Gorilla Tag install with BepInEx 5, and the .NET SDK (any version that can build `net48`).

The project compiles against the assemblies in your own game install. The game path is set in `Directory.Build.props` and defaults to the standard Steam location; edit it there if your game is elsewhere.

```powershell
dotnet build .\GenneCaster.csproj -c Release --nologo
```

Every build drops `GenneCaster-<version>.dll` straight into `Gorilla Tag\BepInEx\plugins\GenneCaster\`. Game and BepInEx assemblies are never copied into the output.

The version lives in `Directory.Build.props` and `Base/PluginInfo.cs`; the build stops with an error if they differ. See [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## License

GenneCaster is licensed under [MIT](LICENSE). The bundled Discord RPC source carries its own [MIT license](External/DiscordRPC/LICENSE); see [third-party notices](THIRD_PARTY_NOTICES.md).
