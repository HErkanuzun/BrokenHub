# Changelog

All notable changes to Broken Hub. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.2] - 2026-11-06

### Changed
- New design, the same across the whole Broken family: a soft colour background, glass cards and buttons, and the window frame in the same style (no white Windows title bar; minimize / maximize / close are drawn by the app). Snapping, resizing from the edges, shadow and rounded corners still work. Dialogs use the same frame.

## [1.1] - 2026-11-06

### Added
- Broken Microphone: use a phone or tablet as the computer's microphone.

### Fixed
- The setup's last page showed an unrelated warning about deleting files.

## [1.0] - 2026-11-05

First release.

### Added
- One window for every Broken tool (Screen Resizer, Disk Resizer, Space Finder): installed version, latest version on GitHub, running state and whether it needs administrator rights.
- **Install / Update**: downloads the setup from the GitHub release, verifies it against `SHA256SUMS.txt`, runs it silently and deletes it. **Update all** when more than one update is available.
- **Open**, **Uninstall** (the tool's own uninstaller) and **GitHub** for each tool.
- **Preview versions** toggle to include pre-releases.
- Taskbar progress while downloading, Turkish and English, dark theme, one running copy, installer and portable ZIP — same as the rest of the Broken family.
[1.1]: https://github.com/HErkanuzun/BrokenHub/releases/tag/v1.1
