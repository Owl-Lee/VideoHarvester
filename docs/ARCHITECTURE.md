# Architecture

VideoHarvester is a Windows Forms application that coordinates local extraction and media-processing tools. The UI is intentionally kept separate from reusable download rules and user-facing error translation.

## Source layout

```text
src/VideoHarvester.App/
├── Core/                    Pure, testable rules and translations
├── Models/                  Download task state
├── MainForm.cs              Window state and initialization
├── MainForm.Layout.cs       UI construction and layout
├── MainForm.Queue.cs        URL expansion and queue execution
├── MainForm.Preflight.cs    Metadata, size, quality, and disk checks
├── MainForm.Progress.cs     Process-output parsing and status projection
├── MainForm.Persistence.cs  Settings, history, and queue recovery
├── MainForm.Commands.cs     User commands and completion dialogs
└── MainForm.Tools.cs        Local tool preparation and updates
```

## Runtime flow

1. The user supplies one or more page URLs.
2. Playlist and collection links are detected and expanded when requested.
3. Preflight analysis asks yt-dlp for metadata and estimates storage requirements.
4. The user confirms the resolved plan.
5. yt-dlp runs as a child process; stdout and stderr are parsed asynchronously.
6. FFmpeg merges or converts media when needed.
7. Task state, download history, and unfinished queues are persisted locally.

## Local data

VideoHarvester stores settings, history, and the recoverable queue under the current user's local application data directory. Media is written only to the destination selected by the user.

## External tools

- yt-dlp: extraction and format selection
- FFmpeg / ffprobe: media merge, conversion, and inspection
- Deno: JavaScript runtime used by modern extraction workflows

The Full release bundles these tools. The Lite release prepares missing tools on first use.
