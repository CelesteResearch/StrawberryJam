[English](README.md) | [简体中文](README.zh-CN.md)

# StrawberryJam

Strawberry Jam port for the Shared runtime. Workspace location:
`Mods/StrawberryJam/`.

## Prepare and run

Check out every component from the workspace directory:

```sh
git submodule update --init --recursive
```

Supply the original game with Everest 6580 stable, and keep the original
Strawberry Jam, asset, audio and required Helper ZIPs in its `Mods/` directory.
Keep the required `ColoredLights.zip` and `TwigHelper.zip` there too.
Supported versions and filenames for the nine local rebuild Helpers are listed
in the [Tools README][tools-readme].

Follow the Tools README for audio banks, native libraries and the chosen
Unity/Godot host dependencies, then add `-IncludeHelpers` to the same entry:

```powershell
pwsh ./Tools/Prepare.ps1 `
  -CelesteDirectory '/path/to/Celeste' `
  -AudioBankDirectory '/path/to/Desktop' `
  -Targets Godot `
  -IncludeHelpers
```

Preparation builds the source Helpers, reconstructs the other nine Mods from
original ZIPs and installs runtime content in `Content/Celeste/Mods/`. Start the
game using the selected host's README.

## Build the assembly separately

After preparing Shared resources and checking out the project's dependencies:

```powershell
dotnet build ./Mods/StrawberryJam/Celeste.Mods.StrawberryJam.csproj -c Release
```

License for the code and port-specific changes: **MIT**. See [LICENSE](LICENSE) and [NOTICE.md](NOTICE.md) for terms and third-party notices.

[tools-readme]: https://github.com/CelesteResearch/Tools/blob/main/README.md#helpers-and-strawberry-jam
