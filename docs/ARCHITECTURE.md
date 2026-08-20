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

---

## 简体中文

VideoHarvester 是一个 Windows Forms 应用，用来协调本地解析与媒体处理工具。界面层与可复用的下载规则、面向用户的错误翻译保持分离。

### 源码结构

```text
src/VideoHarvester.App/
├── Core/                    可独立测试的规则与文本转换
├── Models/                  下载任务状态
├── MainForm.cs              窗口状态与初始化
├── MainForm.Layout.cs       界面构建与布局
├── MainForm.Queue.cs        URL 展开与队列执行
├── MainForm.Preflight.cs    元数据、大小、画质与磁盘检查
├── MainForm.Progress.cs     进程输出解析与状态映射
├── MainForm.Persistence.cs  设置、历史记录与队列恢复
├── MainForm.Commands.cs     用户命令与完成提示
└── MainForm.Tools.cs        本地工具准备与更新
```

### 运行流程

1. 用户输入一个或多个视频页面 URL。
2. 系统识别播放列表或合集，并按用户选择展开。
3. 预检阶段通过 yt-dlp 读取元数据并估算存储空间。
4. 用户确认解析后的任务计划。
5. yt-dlp 作为子进程运行；标准输出和错误输出被异步解析。
6. 必要时由 FFmpeg 合并或转换媒体。
7. 任务状态、下载历史与未完成队列保存到本机。

### 本地数据与外部工具

设置、历史记录和可恢复队列保存在当前用户的本地应用数据目录；媒体只写入用户选择的位置。yt-dlp 负责解析与格式选择，FFmpeg / ffprobe 负责合并、转换与检查，Deno 提供部分现代解析流程所需的 JavaScript 运行时。完整版包含这些工具，轻量版会在首次使用时准备缺失组件。
