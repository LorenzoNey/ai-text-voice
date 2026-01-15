# Download and install .NET 8 SDK
$ErrorActionPreference = "Stop"

$sdkUrl = "https://download.visualstudio.microsoft.com/download/pr/f18288c6-1732-415b-b577-e1b2f4a3a93d/64e86c30b9e505a46673edc5aabfbed3/dotnet-sdk-8.0.404-win-x64.exe"
$installerPath = Join-Path $env:TEMP "dotnet-sdk-8.0-win-x64.exe"

Write-Host "Downloading .NET 8 SDK..." -ForegroundColor Cyan
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $sdkUrl -OutFile $installerPath -UseBasicParsing

if (Test-Path $installerPath) {
    $size = [math]::Round((Get-Item $installerPath).Length / 1MB, 2)
    Write-Host "Downloaded: $installerPath ($size MB)" -ForegroundColor Green

    Write-Host "Installing .NET 8 SDK (this may take a few minutes)..." -ForegroundColor Cyan
    Start-Process -FilePath $installerPath -ArgumentList "/install", "/quiet", "/norestart" -Wait

    # Verify installation
    $dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
    if (Test-Path $dotnetPath) {
        $version = & $dotnetPath --version 2>&1
        Write-Host ".NET SDK installed successfully! Version: $version" -ForegroundColor Green
    } else {
        Write-Host "Installation may have failed. Please check manually." -ForegroundColor Yellow
    }
} else {
    Write-Host "Download failed!" -ForegroundColor Red
}
