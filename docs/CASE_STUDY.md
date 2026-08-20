# VideoHarvester case study

## The problem

Command-line video tools are powerful, but their normal workflow exposes implementation details to users: format selectors, browser-cookie arguments, FFmpeg paths, extractor warnings, and long streams of technical output. The underlying download may be working while the interface still appears frozen or failed.

VideoHarvester explores a product question: how can that capability become a predictable Windows workflow for a non-technical user without hiding important limitations?

## Product principles

1. **Explain before acting.** A preflight step identifies a single video versus a collection, estimates quality and size, shows the destination, and asks for confirmation.
2. **Show evidence of life.** Stage text, animated activity, percentage, speed, remaining time, and per-item states distinguish slow work from a stalled process.
3. **Translate, do not erase.** The default panel explains failures in plain language; the raw diagnostic log remains one click away for troubleshooting.
4. **Design for collections.** Playlist decisions happen once, files are placed in a dedicated folder, and numbering preserves the source order.
5. **Recover from interruption.** The queue and download history survive an accidental close, while yt-dlp's partial files support continuation.
6. **Keep control local.** Media processing happens on the user's computer and browser authentication remains opt-in.

## Engineering decisions

- C# Windows Forms keeps the application native to the target platform and allows a single portable executable.
- The UI launches child processes asynchronously so extraction and FFmpeg work do not freeze the window.
- Reusable URL, naming, duplicate-key, size-formatting, and error-translation rules are separated from the form and covered by executable checks.
- Platform video IDs provide more reliable duplicate detection than filenames alone.
- Settings, task history, and the recoverable queue are persisted under the current user's local application data directory.
- GitHub Actions rebuilds the solution and runs the core checks on Windows for every push and pull request.

## What changed through iteration

The earliest interface exposed too much terminal output and provided too little feedback during slow Bilibili extraction. User testing led to a compact task table, friendly status summaries, copyable diagnostics, playlist-aware confirmation, per-file completion behavior, record clearing, automatic collection folders, and queue recovery.

This distinction matters: the project is not only a wrapper around yt-dlp. Its primary work is state management, failure handling, product language, and turning an uncertain multi-process operation into a workflow users can understand.

## Current boundaries

- Website support ultimately depends on the upstream extractor and can change without an application release.
- Actual quality depends on region, source availability, membership, and account permissions; the app reports a downgrade instead of silently promising unavailable quality.
- The project intentionally does not bypass DRM, paywalls, private-video permissions, or membership restrictions.
- The current release is Windows-only and unsigned.

## Next steps

- Add automated UI coverage for preflight, queue recovery, and completion dialogs.
- Improve keyboard navigation, screen-reader labels, and high-DPI testing.
- Make Full and Lite packages reproducible from a tagged workflow.
- Add code signing when distribution volume justifies the cost.

---

## 简体中文

### 问题

命令行视频工具功能强大，但会把格式选择、浏览器 Cookie 参数、FFmpeg 路径、解析警告和大量技术输出直接暴露给普通用户。实际下载可能仍在运行，界面却很容易让人误以为已经卡死或失败。VideoHarvester 要解决的问题是：如何在不隐瞒真实限制的前提下，把这些能力做成非技术用户也能预测和理解的 Windows 工作流。

### 产品原则

1. **执行前先解释。** 预检阶段区分单个视频与合集，估算画质和大小，展示保存位置并要求确认。
2. **持续显示工作证据。** 阶段文本、活动动画、百分比、速度、剩余时间和单项状态帮助用户区分“处理较慢”与“进程卡死”。
3. **翻译错误，而不是删除细节。** 默认界面提供易懂说明，完整诊断日志仍可一键复制。
4. **为合集设计。** 合集只询问一次，文件进入独立目录并按来源顺序编号。
5. **可从中断恢复。** 队列和历史记录在意外关闭后保留，并利用 yt-dlp 的临时文件继续任务。
6. **控制权留在本地。** 媒体处理在用户电脑上完成，浏览器登录信息仅在用户主动开启时使用。

### 工程决策

- C# Windows Forms 与目标平台原生一致，并支持便携式程序分发。
- 界面异步启动子进程，避免解析和 FFmpeg 工作冻结窗口。
- URL、命名、去重、大小格式化和错误翻译规则与窗体分离，并由可执行检查覆盖。
- 使用平台视频 ID 进行去重，比只比较文件名更可靠。
- 设置、任务历史与可恢复队列保存在当前用户的本地应用数据目录。
- GitHub Actions 在每次推送和拉取请求时，于 Windows 上重新构建并运行核心检查。

### 迭代结果与边界

早期界面暴露了过多终端输出，也无法在 Bilibili 解析较慢时提供足够反馈。用户测试推动了紧凑任务表、友好状态摘要、可复制诊断、合集确认、单文件完成操作、记录清理、自动合集目录和队列恢复。项目的主要价值不只是包装 yt-dlp，而是处理状态、失败和产品语言，把不确定的多进程操作变成可以理解的流程。

网站支持范围最终取决于上游解析器；实际画质受地区、来源、会员和账号权限影响。项目不绕过 DRM、付费墙、私密视频权限或会员限制，当前版本仅支持 Windows 且尚未进行代码签名。

### 下一步

- 为预检、队列恢复和完成提示增加自动化界面测试。
- 改进键盘操作、屏幕阅读器标签和高 DPI 测试。
- 让完整版与轻量版可以从带标签的工作流中稳定复现。
- 在分发规模需要时加入代码签名。
