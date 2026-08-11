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
