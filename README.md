# GenneCaster

GenneCaster is a BepInEx mod for Gorilla Tag with camera, freecam, player, map, theme, minimap, and nametag controls.

## Build Requirements

- Windows with a local Gorilla Tag installation
- BepInEx installed for that game
- .NET SDK able to build `net48`

The project compiles against the assemblies in your own Gorilla Tag installation. Game assemblies and generated build output are excluded from this repository and from the release archive.

## Build

Set `GORILLA_TAG_PATH` to the game folder if it is outside the default Steam location, or pass `-p:GamePath="C:\path\to\Gorilla Tag"` to `dotnet build`.

```powershell
dotnet build .\GenneCaster.csproj -c Release --nologo
```

The output mod is `bin/Release/net48/GenneCaster.dll`. Release builds do not generate a PDB and do not install into the game automatically. For a local test, copy that DLL to `Gorilla Tag\BepInEx\plugins\GenneCaster\GenneCaster.dll`. To opt into the build's copy step, pass `-p:DeployToGame=true`.

You may also find a downloadable .dll file in the releases page of this GitHub repository.

## License

GenneCaster is licensed under [MIT](LICENSE). The bundled Discord RPC source carries its own [MIT license](External/DiscordRPC/LICENSE); see [third-party notices](THIRD_PARTY_NOTICES.md).
