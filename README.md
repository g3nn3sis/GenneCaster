# Zerori

Zerori is a free, open-source spectating mod for Gorilla Tag, built for casters. It turns the desktop view into a broadcast camera with first-person and orbit spectating, a freecam, nametags, a minimap, a player leaderboard, a closest-lava readout, a two-team scoreboard with a match timer, and Discord Rich Presence. Every feature is configured from in-game panels and saved between sessions, and whole setups can be stored as named presets.

Zerori is a fan-made mod. It is not affiliated with or endorsed by Another Axiom.

## Install

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (x64) into your Gorilla Tag folder and start the game once so BepInEx creates its folders.
2. Download `Zerori-<version>.dll` from the [releases page](https://github.com/g3nn3sis/GenneCaster/releases).
3. Install it into the BepInEx plugins folder, it should end up at `Gorilla Tag\BepInEx\plugins\Zerori-<version>.dll`.
4. Start the game on PC. The Zerori panel opens in the top-left corner.

Each release is built against the Gorilla Tag version that was current when it was published. If a game update breaks the mod, check for a newer release.

## Features

| Panel | What it configures |
| --- | --- |
| Zerori | Join a room by code, join a public room, leave the room, see your connection status, and reset the panel layout |
| WASD | Fly your own player with WASD, mouse click, movement speed, mouse sensitivity |
| Replay | Record the match — every player's body and hand movement, time of day and rain, joins, leaves and tags, and other players' voice chat — for up to 60 minutes; stopping saves it to a file, and saved replays are listed to play or delete. In replay mode every panel stays available so you can change camera, nametag, theme and other casting settings while you watch; the panels move down under a timeline bar across the top, with the Replay panel first in its column. The bar has play/pause, drag to scrub, event markers (tags orange, joins green, leaves grey, time/weather blue) with hover details, and the spectated player's voice activity under the track. Spectate any recorded player (orbit or first person), fly a FreeCam, or watch through the Web camera while they move. Recording never changes what you see live: alongside each player's body it stores their raw networked position, head and hands (the highest rig lerp), so you can keep adjusting Rig Interpolation while casting. In replay mode the Replay panel's Rig Lerp slider (0.01 to 1, starting at your live interpolation) re-applies the lerp locally to those raw targets, where 1 is raw and lower values are smoother, and the head and arms are re-posed to follow them. The Leaderboard, Closest Lava and Kill Feed overlays and the Scoreboard keep working in replay mode, showing the recorded players, distances and tags at the current point in the replay (the MiniMap is hidden). Recorded players get the same nametags as live casting (font, player or tag color, FPS and platform icon as they were at that moment); Stop leaves replay mode and restores the live game |
| Camera | Camera mode (Default, Spectator, Freecam, Web), field of view, near clip, first person or orbit distance, orbit speed, position and rotation smoothing, auto orbit, and one-tap FOV and view presets |
| Camera Web | A web of fixed camera positions (nodes) for Web camera mode: the camera sits on a node and turns to watch the best action near it — a chase first, then taggers, then lone runners — without locking onto one player. **Selection** is Dynamic (the node is picked from how close players are to it, whether a runner is being chased, and who is a tagger) or Manual (step through nodes with the arrow keys or the Previous/Next buttons). **Edit Nodes** flies the camera like Freecam and shows every node as a numbered marker, with the one nearest the camera in white; add a node where the camera is, remove the nearest, or clear them all. Save the web under a name, and load or delete saved webs from the list. Web mode also works in replay mode, where it watches the recorded players, and nodes can be edited there too |
| MiniMap | Top-down minimap in the top-right (default), bottom-left or bottom-right corner; corner position, size, camera height, dragging, position reset |
| NameTags | Nametags, font, player color, FPS, platform icon (Steam or Meta) |
| Leaderboard | Stack of rounded pills in the bottom-left (default), bottom-right or top-right corner, each showing the keyboard number that spectates that player, their name, an optional FPS readout and their Steam/Meta icon; tagged names are orange, others white, and the spectated player is bold. Enable, position, scale, position offset, platform icons, FPS, font |
| Scoreboard | Two-team scoreboard with a central match timer, all under the same theme, shown top-center (default) or in the bottom-left, bottom-right or top-right corner. You set the team names and scores from its panel; the timer runs a 10 second pre-roll (orange, shown negative) then counts up in white and stops at 3:00, with start/pause and reset. Enable, position, scale, position offset, font |
| Closest Lava | Bottom-center rounded box with the distance from the spectated player to the nearest tagged (lava) player; 0 when spectating a tagged player. Enable, scale, position offset, font |
| Kill Feed | Stack of rounded pills in the top-right (default), bottom-left or bottom-right corner that announces each player the moment they are tagged — their name in orange next to a `TAGGED` label — with the newest nearest the screen edge, up to five at once, each fading in and out after a few seconds. Enable, position, scale, position offset, font |
| Time | Morning, day, evening, night, rain |
| Maps | Load any map |
| Player | Your name and color, with a preview |
| Settings | Microphone mode, audio from the spectator camera, player list updates, Discord presence, VSync, AFK kick, rig interpolation |
| Keybinds | Every key the mod uses: click a row, then press the new key (click the row again to cancel); **Reset Keys** restores the defaults. The number keys that pick a player are fixed |
| Theme | Accent color of every panel and the minimap |
| Presets | Save the current settings as named presets and load or delete them later; toggle autosave and save settings on demand |

The Leaderboard, Closest Lava, Scoreboard and Kill Feed overlays are Unity canvases loaded from an asset bundle embedded in the DLL, so they render at the same size on any resolution. They all share one font, picked from the game's fonts with the Previous/Next Font buttons in any of their panels. The Leaderboard, Scoreboard, Kill Feed and MiniMap each have a **Position** button that steps through their spots. Only one of them can use a corner at a time: the button skips corners that are in use, and turning one on in a corner that is already used moves it to a free corner. If all three corners are needed, the Scoreboard steps back to top-center.

Every panel is a thin title bar with an arrow. Click the bar or its arrow to slide the panel open or closed. The bars stack themselves into columns grouped by purpose and slide to make room as panels open. Drag a bar to move it: the other panels slide apart as you go, and it settles into the column and place where you let go, so panels never overlap. The arrangement is saved, and the **Reset Layout** button in the Zerori panel puts every panel back. The interface scales with your resolution, and `UI Scale` in the config adds your own multiplier on top.

## Controls

| Input | Action |
| --- | --- |
| `Esc` | Show or hide the panels (configurable) |
| `0` | Spectate the local player (yourself); in replay mode, your recorded player |
| `1` to `9` | Spectate the first nine other players; in replay mode, the recorded players with those leaderboard numbers |
| `]` and `[` | Spectate the next or previous player (configurable); in replay mode, the next or previous recorded player |
| `P` | Pause and unpause the replay in replay mode (configurable) |
| `F1` | Show or hide the replay timeline in replay mode; `Esc` hides only the panels (configurable) |
| `V` (held) | Hold to talk in all chat; release returns to the Microphone setting (configurable) |
| `B` `N` `M` | Scoreboard timer: start, pause, reset (configurable) |
| `H` `J` / `K` `L` | Scoreboard: team A score up/down, team B score up/down (configurable) |
| Arrow keys | Orbit the spectated player when Head Distance is above 0 |
| `←` and `→` | Web camera mode with Manual selection: previous or next node (configurable) |
| `F2` and `F3` | While editing a camera web: add a node where the camera is, remove the nearest node (configurable) |
| Hold right mouse | Fly the camera in Freecam mode or while editing a camera web, or your player when WASD is on (WASD turns off in Freecam mode and while editing a web) |
| `W` `A` `S` `D` | Move while flying |
| `Space` and `Left Shift` | Move up and down while flying |
| `E` (held) | Noclip while flying |
| Left click | Press what is under the cursor with your right hand (Mouse Click must be on) |

## Configuration

Settings are saved to `Gorilla Tag\Zerori\Configs\Default.cfg` a second after you change them, and when the game closes. Turn **Autosave** off in the Presets panel to stop that; settings are then written only when you press **Save Settings** or save a preset. The file holds only section headers and `Key = value` lines, with no comments. You can edit it while the game is closed; a value outside a setting's allowed range is clamped when the game loads it.

Presets are stored as `.cfg` files next to `Default.cfg` in `Gorilla Tag\Zerori\Configs` (`Default` is reserved for the live settings). Loading one applies it immediately, including the panel theme and layout. On first launch, an existing config and presets from `BepInEx\config` are moved there automatically.

Replays are saved as compressed `.gcreplay` files in `Gorilla Tag\Zerori\Replays`, named by the date and time recording stopped. The Replay panel lists them to play or delete.

Camera webs are saved as `.gcweb` text files in `Gorilla Tag\Zerori\Webs`: a header line, then one node per line as `x y z`. The Camera Web panel lists them to load or delete, and the web in use is remembered in the settings (and in presets).

## Troubleshooting

- **No panel appears.** Open `BepInEx\LogOutput.log` and search for `Zerori`. If it is missing, BepInEx is not installed correctly or the DLL is not under `BepInEx\plugins`.
- **The camera does not move.** The mod takes over the game's desktop shoulder camera. The log says `The game's shoulder camera was not found` when a game update has moved it.
- **No Discord presence.** Discord must be running before the game starts, and Rich Presence must be on in Player Settings.
- **Something else.** Open an [issue](https://github.com/g3nn3sis/GenneCaster/issues/new/choose) and attach `LogOutput.log`.

## Build from source

You need Windows, a local Gorilla Tag install with BepInEx 5, and the .NET SDK (any version that can build `net48`).

The project compiles against the assemblies in your own game install. The game path is set in `Directory.Build.props` and defaults to the standard Steam location; edit it there if your game is elsewhere.

```powershell
dotnet build .\Zerori.csproj -c Release --nologo
```

Every build drops `Zerori-<version>.dll` straight into `Gorilla Tag\BepInEx\plugins\Zerori\`. Game and BepInEx assemblies are never copied into the output.

The version lives in `Directory.Build.props` and `Base/PluginInfo.cs`; the build stops with an error if they differ. See [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## License

Zerori is released under the [Zerori Source-Available License](LICENSE): the source is public to view, but it may not be used, run, modified, or redistributed without permission. This is **not** an OSI-approved open-source license. The bundled Discord RPC source carries its own [MIT license](External/DiscordRPC/LICENSE); see [third-party notices](THIRD_PARTY_NOTICES.md).
