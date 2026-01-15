$ErrorActionPreference = "Continue"

# Find MSBuild
$msbuildPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Preview\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
)

$msbuild = $null
foreach ($path in $msbuildPaths) {
    if (Test-Path $path) {
        $msbuild = $path
        Write-Host "Found MSBuild at: $path"
        break
    }
}

# Try dotnet if no MSBuild
if (-not $msbuild) {
    $dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
    if (Test-Path $dotnetPath) {
        Write-Host "No MSBuild found, trying dotnet..."
        & $dotnetPath build "D:\BA Work\wispr-clone\WisprClone.sln" --configuration Debug
        exit $LASTEXITCODE
    }
    Write-Host "ERROR: Neither MSBuild nor dotnet SDK found"
    exit 1
}

# Build with MSBuild
& $msbuild "D:\BA Work\wispr-clone\WisprClone.sln" /t:Rebuild /p:Configuration=Debug /v:minimal
