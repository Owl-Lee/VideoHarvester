# Contributing

Thanks for helping improve VideoHarvester.

## Before opening an issue

- Confirm that the problem still occurs with the latest GitHub Release.
- Try **Update parser** in the application when a website has recently changed.
- Copy the diagnostic log from the app and remove private URLs, cookies, usernames, and local paths before posting it.
- Do not share copyrighted media, browser-cookie databases, or account credentials.

For bugs, include the Windows version, VideoHarvester version, affected website, whether browser login was enabled, expected behavior, actual behavior, and the sanitized diagnostic log.

## Local development

Read [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md), then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

The command builds the Release configuration and runs the core-check executable. Please keep the checks passing and add a focused check when changing reusable rules or error translation.

## Pull requests

- Keep changes focused and explain the user-facing reason.
- Preserve the local-first privacy model.
- Do not add DRM, paywall, authentication, or account-permission bypasses.
- Avoid committing generated binaries, downloaded media, cookies, or build output.
- Update user-facing documentation when behavior changes.

By submitting a contribution, you confirm that you have the right to provide it. No license is granted beyond GitHub's Terms of Service unless the repository owner later adds an explicit project license.
