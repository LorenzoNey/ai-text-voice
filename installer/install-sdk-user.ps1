# Install .NET 8 SDK to user directory
$ErrorActionPreference = "Stop"

$installDir = "$env:USERPROFILE\.dotnet"

Write-Host "Downloading dotnet-install script..." -ForegroundColor Cyan

$installScript = Join-Path $env:TEMP "dotnet-install.ps1"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing

if (Test-Path $installScript) {
    Write-Host "Installing .NET 8 SDK to $installDir..." -ForegroundColor Cyan
    & $installScript -Channel 8.0 -InstallDir $installDir

    # Verify
    $dotnetPath = Join-Path $installDir "dotnet.exe"
    if (Test-Path $dotnetPath) {
        $version = & $dotnetPath --version 2>&1
        Write-Host ""
        Write-Host ".NET SDK installed successfully!" -ForegroundColor Green
        Write-Host "Location: $dotnetPath" -ForegroundColor White
        Write-Host "Version: $version" -ForegroundColor White
    }
}
