# Download and install Inno Setup 6
$ErrorActionPreference = "Stop"

$url = "https://jrsoftware.org/download.php/is.exe"
$installerPath = Join-Path $env:TEMP "innosetup_installer.exe"

Write-Host "Downloading Inno Setup 6..." -ForegroundColor Cyan
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $url -OutFile $installerPath -UseBasicParsing

if (Test-Path $installerPath) {
    $size = [math]::Round((Get-Item $installerPath).Length / 1MB, 2)
    Write-Host "Downloaded: $installerPath ($size MB)" -ForegroundColor Green

    Write-Host "Installing Inno Setup 6 (silent mode)..." -ForegroundColor Cyan
    Start-Process -FilePath $installerPath -ArgumentList "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART" -Wait

    if (Test-Path "C:\Program Files (x86)\Inno Setup 6\ISCC.exe") {
        Write-Host "Inno Setup 6 installed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Installation may have failed. Please install manually from https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    }
} else {
    Write-Host "Download failed!" -ForegroundColor Red
}
