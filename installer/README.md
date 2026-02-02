# Installer Resources

Platform-specific installer resources for AITextVoice.

> **Note**: Releases are automatically built by GitHub Actions when a version tag is pushed. These resources are for manual/custom installer creation.

## Structure

```
installer/
├── windows/          # Windows installer (Inno Setup)
├── macos/            # macOS app bundle resources
└── linux/            # Linux desktop integration
```

## Windows

Uses [Inno Setup](https://jrsoftware.org/isinfo.php) to create a Windows installer.

### Files
- `AITextVoice.iss` - Inno Setup script
- `build-installer.ps1` - Build script
- `download-innosetup.ps1` - Downloads Inno Setup

### Build manually
```powershell
cd installer/windows
.\download-innosetup.ps1   # If Inno Setup not installed
.\build-installer.ps1
```

## macOS

Resources for creating a macOS `.app` bundle.

### Files
- `Info.plist` - App bundle manifest with permission descriptions

### Required Permissions
- **Microphone**: For speech-to-text
- **Accessibility**: For global keyboard shortcuts (Ctrl+Ctrl)

## Linux

Resources for Linux desktop integration.

### Files
- `AITextVoice.desktop` - Desktop entry for application menus

### Installation
Copy to `~/.local/share/applications/` or `/usr/share/applications/`
