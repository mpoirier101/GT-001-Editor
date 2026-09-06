[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot 'src\GT001.Editor.App\GT001.Editor.App.csproj'
$installerScript = Join-Path $repoRoot 'installer\GT001.Editor.iss'

[xml]$project = Get-Content -Raw $appProject
$version = @($project.Project.PropertyGroup.Version | Where-Object { $_ })[0]
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "No <Version> was found in $appProject."
}

$publishDirectory = Join-Path $repoRoot "artifacts\release\GT001.Editor-v$version-win-x64-framework-dependent"
dotnet restore (Join-Path $repoRoot 'GT001.Editor.sln') --configfile (Join-Path $repoRoot 'NuGet.Config') -r win-x64
if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }

dotnet publish $appProject -c $Configuration -r win-x64 --self-contained false --no-restore -p:DebugType=none -p:DebugSymbols=false -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

$iscc = Get-Command iscc.exe, iscc -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $iscc) {
    $perUserIscc = Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'
    if (Test-Path -LiteralPath $perUserIscc) {
        $isccPath = $perUserIscc
    }
    else {
        throw 'Inno Setup Compiler was not found. Install Inno Setup, then rerun this script.'
    }
}
else {
    $isccPath = $iscc.Source
}

if ([string]::IsNullOrWhiteSpace($isccPath)) {
    throw 'Inno Setup Compiler was not found. Install Inno Setup, then rerun this script.'
}

& $isccPath "/DAppVersion=$version" "/DSourceDir=$publishDirectory" $installerScript
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }

Write-Host "Installer created in $(Join-Path $repoRoot 'artifacts\installer')."
