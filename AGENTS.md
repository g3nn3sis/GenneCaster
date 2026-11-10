# AGENTS.md

Entry point for AI agents and contributors working on **GenneCaster**, a BepInEx spectator/
casting plugin for Gorilla Tag (net48, builds to a single `GenneCaster-<version>.dll`).

Start here, then follow the right door:

| You want to… | Read |
| --- | --- |
| **Work in this repo** (build, conventions, the safety net) | **[CLAUDE.md](CLAUDE.md)** |
| Contribution policy, code style, release steps | [CONTRIBUTING.md](CONTRIBUTING.md) |
| What the mod does, install, controls | [README.md](README.md) |

## The short version

- **Build:** `dotnet build GenneCaster.csproj -c Release` — a successful build copies the DLL
  into the game's `BepInEx\plugins\GenneCaster\`.
- **The game can't be launched from here.** The compiler is the only safety net, so keep the
  build green after every change; it is warnings-as-errors.
- **Code standard: professional, production-grade, and comment-free.** Code documents itself
  through naming and structure — no explanatory comments in the mod's own source. Whitespace is
  consistent: four-space indent, one blank line between members, no trailing whitespace, single
  trailing newline. The vendored `External/` tree is exempt (left as upstream). See
  [CLAUDE.md](CLAUDE.md#code-standard).
- A feature is just a class that subclasses `ObjectComponent` — it is auto-discovered by
  reflection and attached to the persistent `_GenneCaster` object. No registry.
- Settings live in `CasterConfig`; never rename an existing section/key. Don't edit
  `External/` (vendored). Keep per-frame code allocation-free.

**Working instructions are in [CLAUDE.md](CLAUDE.md). Read it before changing code.**
