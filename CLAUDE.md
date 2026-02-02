# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

**Note for Claude Code:** The .NET SDK is not in PATH on this machine. Use the full path:
```
C:/Users/LaurentiuNae/.dotnet/dotnet.exe
```

```bash
# Build (from repo root)
dotnet build
# Or with full path if dotnet is not in PATH:
# "C:/Users/LaurentiuNae/.dotnet/dotnet.exe" build

# Run
dotnet run --project src/AITextVoice.Avalonia/AITextVoice.Avalonia.csproj

# Publish for release (Windows x64)
dotnet publish src/AITextVoice.Avalonia/AITextVoice.Avalonia.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Other targets: win-arm64, osx-arm64, osx-x64, linux-x64
```

## Architecture Overview

**AITextVoice** is a cross-platform speech-to-text and text-to-speech desktop app built with:
- **Avalonia UI** (.NET 8.0) for cross-platform desktop UI
- **CommunityToolkit.Mvvm** for MVVM pattern with `[ObservableProperty]` attributes
- **SharpHook** for global keyboard hooks (Ctrl+Ctrl double-tap activation)

### Key Patterns

**Speech Provider Strategy**: Multiple implementations of `ISpeechRecognitionService`:
- `OfflineSpeechRecognitionService` - Windows System.Speech (Windows only)
- `AzureSpeechRecognitionService` - Azure Cognitive Services (streaming)
- `OpenAIWhisperSpeechRecognitionService` - Batch API, re-transcribes every 2s
- `OpenAIRealtimeSpeechRecognitionService` - WebSocket streaming (Windows only, requires NAudio)
- `HybridSpeechRecognitionService` - Offline with Azure fallback

**State Machine**: `TranscriptionState` enum drives the UI:
`Idle → Initializing → Listening → Processing → CopyingToClipboard (→ Error)`

**Event-Driven Communication**: Services emit `RecognitionPartial`, `RecognitionCompleted`, `RecognitionError`, `StateChanged` events consumed by ViewModels.

### Source Layout

```
src/AITextVoice.Avalonia/
├── Core/           # Enums (TranscriptionState, SpeechProvider), Constants
├── Infrastructure/ # Global keyboard hook (SharpHookKeyboardHook, DoubleKeyTapDetector)
├── Models/         # AppSettings, data models
├── Services/       # Business logic, Speech/ subdirectory for providers
├── ViewModels/     # MainViewModel, OverlayViewModel, SystemTrayViewModel
├── Views/          # Avalonia XAML windows (OverlayWindow, SettingsWindow, AboutWindow)
└── Resources/      # Icons, styles
```

### Entry Points

- `Program.cs` - Single-instance mutex, Avalonia setup, platform detection
- `App.axaml.cs` - DI container setup, window lifecycle, tray icon management

### Platform Considerations

- **Windows-only features**: Offline recognition (System.Speech), OpenAI Realtime (NAudio for audio capture)
- **Conditional compilation**: `WINDOWS` constant defined in .csproj for platform-specific code
- **Settings location**: `%AppData%/AITextVoice/settings.json`

## Releasing

Push a version tag to trigger automated GitHub Actions release:
```bash
git tag v2.4.0
git push origin v2.4.0
```

Creates installers for Windows (Inno Setup), macOS (DMG), and Linux (AppImage).
