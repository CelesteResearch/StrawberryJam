[English](README.md) | [简体中文](README.zh-CN.md)

# StrawberryJam

Shared 运行时的草莓酱移植。工作区位置：`Mods/StrawberryJam/`。

## 准备与运行

从完整工作区检出全部组件：

```sh
git submodule update --init --recursive
```

提供已安装 Everest 6580 稳定版的原游戏，将草莓酱、素材、音频及所需 Helper 的原始 ZIP
放在原游戏的 `Mods/` 目录，其中也需要 `ColoredLights.zip` 和 `TwigHelper.zip`。
9 个本地重建 Helper 的支持版本和文件名见
[Tools README][tools-readme-zh-cn]。

按 Tools README 提供音频 banks、原生库及所选 Unity／Godot 宿主的依赖，
在统一准备命令中加上 `-IncludeHelpers`：

```powershell
pwsh ./Tools/Prepare.ps1 `
  -CelesteDirectory '/path/to/Celeste' `
  -AudioBankDirectory '/path/to/Desktop' `
  -Targets Godot `
  -IncludeHelpers
```

准备流程构建源码 Helper，从原始 ZIP 重建其余 9 个 Mod，并将运行内容安装到
`Content/Celeste/Mods/`。随后按所选宿主 README 启动游戏。

## 单独构建程序集

准备 Shared 资源并检出项目引用的依赖后：

```powershell
dotnet build ./Mods/StrawberryJam/Celeste.Mods.StrawberryJam.csproj -c Release
```

代码及移植修改许可：**MIT**。许可条款及第三方声明见 [LICENSE](LICENSE) 和 [NOTICE.md](NOTICE.md)。

[tools-readme-zh-cn]: https://github.com/CelesteResearch/Tools/blob/main/README.zh-CN.md#helper-与草莓酱
