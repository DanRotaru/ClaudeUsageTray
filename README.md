# Claude Usage Tray

A lightweight Windows system tray app that shows your [Claude](https://claude.ai) usage at a glance.

![Dark Orange theme](icon/icon-128.png)

## Features

- **Tray icon** displays your current 5-hour session usage as a percentage
- **Hover tooltip** shows both session and weekly usage with time until reset
- **6 color themes** — Orange (default), Orange/Black, Dark/Orange, Transparent/Orange, Transparent/White, White/Black
- **Configurable refresh interval** — 1 min, 5 min, 30 min, or 1 hour
- Reads credentials automatically from `~\.claude\.credentials.json` (set by Claude Code CLI)

## Requirements

- Windows 10/11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (x64)
- An active Claude Pro / Max subscription logged in via [Claude Code](https://github.com/anthropics/claude-code)

## Usage

1. Install the .NET 8 Desktop Runtime if not already present
2. Run `ClaudeUsageTray.exe` — an icon appears in the system tray
3. Right-click the icon to change the theme, refresh interval, or exit

## Building

```
dotnet build -c Release
```

To produce a single distributable `.exe`:

```
dotnet publish -c Release
```

## How it works

The app polls `https://api.anthropic.com/api/oauth/usage` every minute (configurable) using the OAuth access token stored by Claude Code in `%USERPROFILE%\.claude\.credentials.json`. The tray icon is rendered dynamically as a 32×32 GDI+ bitmap showing the session utilization percentage.
