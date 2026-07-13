param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$localDotnet = Join-Path $root '.dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) {
    $localDotnet
} else {
    (Get-Command dotnet -ErrorAction Stop).Source
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml] $props = Get-Content (Join-Path $root 'Directory.Build.props')
    $Version = ([string] $props.Project.PropertyGroup.Version).Trim()
}
if ($Version -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$') {
    throw "Invalid package version: $Version"
}

$packageName = "ForzaOSD-$Version-win64.zip"
$packageBase = [IO.Path]::GetFileNameWithoutExtension($packageName)
$staging = Join-Path $root 'artifacts\staging-release'
$payload = Join-Path $staging $packageBase
$package = Join-Path $root "dist\$packageName"

if (Test-Path -LiteralPath $staging) {
    $resolved = [IO.Path]::GetFullPath($staging)
    $artifacts = [IO.Path]::GetFullPath((Join-Path $root 'artifacts'))
    if (-not $resolved.StartsWith($artifacts, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove staging path outside artifacts: $resolved"
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}

& $dotnet publish (Join-Path $root 'src\ForzaOSD.App\ForzaOSD.App.csproj') `
    -c Release -r win-x64 --self-contained true -o $payload -p:Version=$Version
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

foreach ($file in 'README.md', 'LICENSE', 'Lua.md', 'credits.txt', 'config.example.json', 'FOSD_logo.png') {
    Copy-Item -LiteralPath (Join-Path $root $file) -Destination $payload
}

$profileSource = Join-Path $root 'hud_profiles'
$profileDestination = Join-Path $payload 'hud_profiles'
New-Item -ItemType Directory -Force -Path $profileDestination | Out-Null
Get-ChildItem -LiteralPath $profileSource -Directory | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $profileDestination -Recurse
}

$sourceProfiles = @(Get-ChildItem -LiteralPath $profileSource -Recurse -Filter profile.lua)
$packagedProfiles = @(Get-ChildItem -LiteralPath $profileDestination -Recurse -Filter profile.lua)
if ($sourceProfiles.Count -eq 0 -or $packagedProfiles.Count -ne $sourceProfiles.Count) {
    throw "Profile packaging audit failed: source=$($sourceProfiles.Count), package=$($packagedProfiles.Count)"
}
if (-not (Test-Path -LiteralPath (Join-Path $payload 'credits.txt'))) {
    throw 'credits.txt is missing from the package'
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $package) | Out-Null
if (Test-Path -LiteralPath $package) {
    Remove-Item -LiteralPath $package -Force
}
Compress-Archive -Path $payload -DestinationPath $package -CompressionLevel Optimal

Write-Host "Created $package"
