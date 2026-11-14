# Playroom for Windows

Playroom is a standalone WPF desktop app with five offline games. The desktop edition has a keyboard-friendly launcher, a daily challenge and local streak, individual records, small milestones, and a privacy page with a clear-data control.

## Run from source

Install the .NET 10 SDK, then run:

```powershell
dotnet run --project .\windows\Playroom.Windows.csproj
```

## Publish a portable Windows build

From the project root:

```powershell
dotnet publish .\windows\Playroom.Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\windows\publish
```

The published folder contains a self-contained `Playroom.exe`; the target PC does not need .NET installed.

## Controls

- **Connect Four:** Click a column or press `1`–`7` on the number row or keypad; `R` starts a new match.
- **Rock Paper Scissors:** Click a move or press `1`, `2`, or `3` on the number row or keypad.
- **Memory Match:** Use the mouse or `Tab` and `Enter`; `R` shuffles a new round.
- **2048:** Use the arrow keys, `WASD`, or on-screen arrows. `Undo` reverses one move; `R` starts over.
- **Whack-a-Mole:** Click a hole or press `1`–`9` on the number row or keypad; `Space` starts a round.
- **All games:** Press `Escape` to return to the collection.

## Local data and safety

Scores and daily progress live in `%LOCALAPPDATA%\Playroom\scores.json`. They are not uploaded or shared. The app has no network, ad, analytics, account, or third-party package dependencies; it runs as the current Windows user and does not request administrator privileges. The Privacy & data page can clear this file after confirmation.

The portable build is not signed with a trusted publisher certificate, so Windows SmartScreen may show a reputation warning. This project has no signing identity; do not treat the absence of a warning as a substitute for checking where a download came from.
