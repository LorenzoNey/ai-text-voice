# Install .NET 8 SDK using official dotnet-install script
$ErrorActionPreference = "Stop"

Write-Host "Downloading dotnet-install script..." -ForegroundColor Cyan

$installScript = Join-Path $env:TEMP "dotnet-install.ps1"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

try {
    Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing
} catch {
    Write-Host "Failed to download install script. Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install .NET 8 SDK manually from:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
    exit 1
}

if (Test-Path $installScript) {
    Write-Host "Installing .NET 8 SDK..." -ForegroundColor Cyan
    & $installScript -Channel 8.0 -InstallDir "C:\Program Files\dotnet"

    # Refresh PATH
    $env:PATH = "C:\Program Files\dotnet;$env:PATH"

    # Verify
    $dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
    if (Test-Path $dotnetPath) {
        $version = & $dotnetPath --version 2>&1
        Write-Host ".NET SDK installed! Version: $version" -ForegroundColor Green
    }
}
