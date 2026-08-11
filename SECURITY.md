# Security policy

## Supported version

Security fixes are applied to the latest published release of VideoHarvester.

## Reporting a vulnerability

Please avoid opening a public issue for a vulnerability that could expose browser data, execute untrusted commands, overwrite arbitrary files, or compromise a user's computer.

Use GitHub's private vulnerability reporting feature when it is available for this repository. Include the affected version, reproduction steps, impact, and any suggested mitigation. Do not include real cookies, passwords, access tokens, or copyrighted media.

For ordinary download failures, website changes, or unsupported formats, use a regular GitHub issue after removing private URLs and local information from the diagnostic log.

## Security model

- VideoHarvester processes media locally and does not operate a media-upload service.
- Browser authentication is opt-in and delegated to yt-dlp's browser-cookie integration.
- External executables are launched with explicit arguments and without a command shell.
- The application does not attempt to bypass DRM, paywalls, or account permissions.
- Users should download releases only from this repository and verify the published checksums when available.
