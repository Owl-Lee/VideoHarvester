# Development

## Requirements

- Windows 10 or Windows 11
- .NET Framework 4.7.2 Developer Pack
- PowerShell 5.1 or later
- Visual Studio with the .NET desktop workload (optional, but recommended)

## Build

From the repository root:

```powershell
.\scripts\build.ps1
```

The Release executable and configuration are copied to:

```text
artifacts/bin/
```

## Run core checks

```powershell
.\scripts\test.ps1
```

The checks intentionally use no third-party test framework so the project can be validated on a clean Windows machine with the .NET Framework developer tools.

## Runtime tools

The application looks for `yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe`, and `deno.exe` beside `VideoHarvester.exe`. Missing tools are prepared automatically by the application. These binaries are ignored by Git and are not part of the source tree.

## Release packaging

- **Full:** application plus yt-dlp, FFmpeg/ffprobe, and Deno.
- **Lite:** application and usage guide only.

Release binaries should be attached to GitHub Releases rather than committed to the Git repository.

---

## 简体中文

### 环境要求

- Windows 10 或 Windows 11
- .NET Framework 4.7.2 Developer Pack
- PowerShell 5.1 或更高版本
- 安装 .NET 桌面开发工作负载的 Visual Studio（可选但推荐）

### 构建与检查

在仓库根目录运行：

```powershell
.\scripts\build.ps1
.\scripts\test.ps1
```

Release 可执行文件和配置会复制到 `artifacts/bin/`。核心检查不依赖第三方测试框架，因此可以在只安装 .NET Framework 开发工具的干净 Windows 环境中运行。

### 运行组件与发布包

程序会在 `VideoHarvester.exe` 同目录寻找 `yt-dlp.exe`、`ffmpeg.exe`、`ffprobe.exe` 和 `deno.exe`，缺失组件会由软件自动准备。这些二进制文件不会进入源码仓库。

- **完整版：** 应用程序以及 yt-dlp、FFmpeg/ffprobe 和 Deno。
- **轻量版：** 应用程序与使用说明；首次使用时准备组件。

发布二进制文件应上传到 GitHub Releases，不应直接提交到 Git 仓库。
