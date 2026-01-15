# Find dotnet SDK and build the project
$ErrorActionPreference = "Stop"

Write-Host "Searching for .NET SDK..." -ForegroundColor Cyan

# Try common paths (user directory first since that's where we just installed)
$dotnetPaths = @(
    "$env:USERPROFILE\.dotnet\dotnet.exe",
    "C:\Users\LaurentiuNae\.dotnet\dotnet.exe",
    "C:\Program Files\dotnet\dotnet.exe",
    "$env:ProgramFiles\dotnet\dotnet.exe",
    "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"
)

$dotnetExe = $null
foreach ($path in $dotnetPaths) {
    if (Test-Path $path) {
        try {
            $version = & $path --version 2>&1
            if ($LASTEXITCODE -eq 0) {
                $dotnetExe = $path
                Write-Host "Found .NET SDK at: $path (version: $version)" -ForegroundColor Green
                break
            }
        } catch {
            continue
        }
    }
}

# Also try just 'dotnet' in case it's in PATH
if (-not $dotnetExe) {
    try {
        $version = & dotnet --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            $dotnetExe = "dotnet"
            Write-Host "Found .NET SDK in PATH (version: $version)" -ForegroundColor Green
        }
    } catch { }
}

if (-not $dotnetExe) {
    Write-Host "ERROR: .NET SDK not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please either:" -ForegroundColor Yellow
    Write-Host "1. Install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
    Write-Host "2. Or run from Developer PowerShell for VS 2022" -ForegroundColor White
    exit 1
}

# Build the project
$projectPath = "D:\BA Work\wispr-clone\src\WisprClone\WisprClone.csproj"
Write-Host ""
Write-Host "Building Release..." -ForegroundColor Cyan

& $dotnetExe publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=none -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green

# Now run Inno Setup
$issPath = "D:\BA Work\wispr-clone\installer\WisprClone.iss"
$isccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if (Test-Path $isccPath) {
    Write-Host ""
    Write-Host "Creating installer with Inno Setup..." -ForegroundColor Cyan
    & $isccPath $issPath

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  Installer created successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green

        $outputDir = "D:\BA Work\wispr-clone\installer\output"
        $installer = Get-ChildItem -Path $outputDir -Filter "WisprClone-Setup-*.exe" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($installer) {
            Write-Host "Output: $($installer.FullName)" -ForegroundColor White
            $sizeMB = [math]::Round($installer.Length / 1MB, 2)
            Write-Host "Size: $sizeMB MB" -ForegroundColor White
        }
    } else {
        Write-Host "Installer creation failed!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Inno Setup not found at: $isccPath" -ForegroundColor Red
    exit 1
}
