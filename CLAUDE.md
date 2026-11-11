# Working in GenneCaster

Instructions for an AI agent (or any new contributor) working in this repository. Read this
before changing code.

## What this is

GenneCaster is a **BepInEx plugin for Gorilla Tag** (a Unity VR game) that turns the game into
a spectator/casting tool: free/orbit camera, nametags, minimap, leaderboard, scoreboard,
closest-lava readout, Discord Rich Presence, and an in-game IMGUI control panel. Target
framework is **net48**; it builds to a single `GenneCaster-<version>.dll`.

## The one rule that shapes everything

**The game cannot be launched from here.** The compiler is the entire safety net. Therefore:

- After **every** code change, run the Release build and keep it green.
- The build is configured **warnings-as-errors** — a new warning fails the build. Keep it at
  zero warnings.
- Behaviour you cannot verify in-game must be preserved exactly. Prefer compiler-checkable,
  behaviour-preserving changes over speculative rewrites.

## Build

```bash
dotnet build GenneCaster.csproj -c Release
```

A successful plugin build **copies `GenneCaster-<version>.dll` into the game's
`BepInEx\plugins\GenneCaster\`** folder automatically (see `Directory.Build.targets`). Paths
to the game and BepInEx are in `Directory.Build.props` (`GamePath`).

## Layout

```
Base/            Plugin bootstrap: BaseLoader, ObjectComponent, HarmonyPatches, CasterConfig, Log, PluginInfo
Game/            On-screen/game features: NameTag, MiniMap, Leaderboard, Scoreboard, ClosestLava, KillFeed, TimeManager, assets
Player/          Local + networked player: controllers, rig list, platform detection
GameCamera/      Spectator/free camera: CameraManager, wrapper, smoothing, Modes/
UI/              IMGUI panel system: CoreUI, PanelOrchestrator, Panel, PanelContent, theming, scaling
DiscordRichPresence/  Mod-side Discord integration
Patches/         Harmony patches
External/        Vendored third-party source (DiscordRPC) — do not modify
Resources/       Embedded icons + HUD asset bundle
```

Namespaces mirror folders: `GenneCaster.<Folder>` (e.g. `GenneCaster.Game`). There is no
`Classes.` segment.

## Code standard

Professional, production-grade C#. **No comments** — the code must document itself through
precise naming, small cohesive methods, and clear structure. If a piece of logic needs a
comment to be understood, rename it or extract it until it doesn't. (This applies to the mod's
own code under `Base, Game, Player, UI, GameCamera, DiscordRichPresence, Patches`; the vendored
`External/` tree is left exactly as upstream ships it.)

Whitespace is consistent and deliberate: four-space indentation, braces on their own line, a
single blank line between members (never two or more), no trailing whitespace, exactly one
newline at end of file. Keep related code grouped and ordered the same way across features.

## Conventions

- Block-scoped namespaces mirroring folders (`GenneCaster.<Folder>`).
- `internal` and `sealed` by default; `public` only when required.
- Nullable reference types are **on**; honour the annotations.
- Log through `Log`; per-frame failures use `Log.*Once`. Never an empty `catch`.
- Every user-facing setting goes in `CasterConfig`. **Never rename an existing config
  section/key** — it is a saved-data contract. Config is for settings the user changes in the
  UI and presets snapshot; internal tuning values (animation timing, lerp/damping factors,
  layout spacing, thresholds) stay as `private const` in the class that uses them, never in
  config.
- `PluginInfo.Version` and `<Version>` in `Directory.Build.props` must match (build enforces it).
- `GenneCaster.csproj` references only the assemblies actually used; add a `<Reference>` when a
  new one is needed (the build error names it).

## Gotchas

- `NetworkedPlayerManager.activeRigs` can be null; the camera/rig may not exist yet.
- UI is authored at 1080p through `UiScale`; it must look right from 720p to 4K.
- Input uses the new Input System via `CasterConfig.KeyPressed/KeyHeld`, not legacy `Input`.
- Features are auto-discovered by reflection — a new `ObjectComponent` subclass runs with no
  registration, so an incomplete one still loads. Guard with `enabled`.

## Also see

- [AGENTS.md](AGENTS.md) — entry point / map of these docs
- [CONTRIBUTING.md](CONTRIBUTING.md) — contribution policy, code style, release steps
- [README.md](README.md) — what the mod does, install, controls, build-from-source
