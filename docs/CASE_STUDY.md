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
