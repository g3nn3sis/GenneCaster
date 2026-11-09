# Contributing to GenneCaster

Thanks for helping. Bug reports, fixes and new caster features are all welcome.

## Before you start

- Check the [open issues](https://github.com/g3nn3sis/GenneCaster/issues) so work is not duplicated. For a larger feature, open an issue first to agree on the approach.
- Panels only configure features. A feature that draws on screen (an overlay, a scoreboard, a HUD) belongs in its own class, not inside a panel.
- On-screen UI is laid out at 1080p in `UiScale` units and must look right from 720p to 4K.

## Building

Follow **Build from source** in the [README](README.md). Test every change in game before opening a pull request, and say in the pull request what you tested.

## Code style

- Match the surrounding code: four-space indents, braces on their own line, `internal` unless something needs to be public.
- Log through `Log` (BepInEx), never `Console.WriteLine` or `Debug.Log`. Code that runs every frame reports failures with `Log.WarningOnce` or `Log.ExceptionOnce`.
- Never leave a `catch` block empty.
- Every user-facing setting goes in `CasterConfig` so it is saved. Only UI settings belong there — internal tuning values (animation timing, lerp damping, layout spacing, thresholds) stay as `const` in their class, not in the config.
- Write self-documenting, comment-free code: make it readable through naming and structure
  rather than comments. Consistent whitespace — four-space indent, one blank line between
  members, no trailing whitespace. (The vendored `External/` tree is left as upstream ships it.)

## Releases

1. Update `<Version>` in `Directory.Build.props` and `Version` in `Base/PluginInfo.cs`.
2. Add the release to `CHANGELOG.md`.
3. Build with `dotnet build -c Release`. The build drops `GenneCaster-<version>.dll` into your game's `BepInEx\plugins\GenneCaster\` folder; zip that folder (alongside the licenses) as `GenneCaster-v<version>.zip` and attach it to the GitHub release.
