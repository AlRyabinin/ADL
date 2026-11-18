[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'release\ADL-Win10-x64')
)

$ErrorActionPreference = 'Stop'

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$StepName
    )

    Write-Host ("==> " + $StepName) -ForegroundColor Cyan
    & $script:DotNetPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw ("Command failed: dotnet " + ($Arguments -join ' '))
    }
}

if ($env:OS -ne 'Windows_NT') {
    throw 'This release script must be run on Windows 10/11.'
}

$dotnetCommand = Get-Command 'dotnet.exe' -ErrorAction SilentlyContinue
if (-not $dotnetCommand) {
    $dotnetCommand = Get-Command 'dotnet' -ErrorAction SilentlyContinue
}
if (-not $dotnetCommand) {
    throw 'dotnet.exe was not found in PATH. Install the .NET 8 SDK or add its folder to PATH.'
}
$script:DotNetPath = $dotnetCommand.Source

$sdkList = & $script:DotNetPath --list-sdks
if ($LASTEXITCODE -ne 0 -or -not ($sdkList | Where-Object { $_ -match '^8\.' })) {
    throw 'The .NET 8 SDK is required. A .NET Runtime or Desktop Runtime is not sufficient.'
}

$projectRoot = $PSScriptRoot
$libraryProject = Join-Path $projectRoot 'AdlpLib\AdlpLib.csproj'
$mainProject = Join-Path $projectRoot 'AdlpdsConfig\AdlpdsConfig.csproj'
$dbProject = Join-Path $projectRoot 'AdlpDBConfigurator\AdlpDBConfigurator.csproj'
$emulatorProject = Join-Path $projectRoot 'ADLDeviceEmulator\ADLDeviceEmulator.csproj'

$projects = @(
    @{ Name = 'AdlpLib'; Path = $libraryProject; Runtime = $false },
    @{ Name = 'AdlpdsConfig'; Path = $mainProject; Runtime = $true },
    @{ Name = 'AdlpDBConfigurator'; Path = $dbProject; Runtime = $true },
    @{ Name = 'ADLDeviceEmulator'; Path = $emulatorProject; Runtime = $true }
)

foreach ($project in $projects) {
    if (-not (Test-Path -LiteralPath $project.Path)) {
        throw ("Project file not found: " + $project.Path + ". Place this script in the ADL solution root.")
    }
}

$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) {
    Remove-Item -LiteralPath $output -Recurse -Force
}

$mainOutput = Join-Path $output 'Application'
$dbOutput = Join-Path $output 'DatabaseSetup'
$emulatorOutput = Join-Path $output 'DeviceEmulator'
New-Item -ItemType Directory -Path $mainOutput -Force | Out-Null
New-Item -ItemType Directory -Path $dbOutput -Force | Out-Null
New-Item -ItemType Directory -Path $emulatorOutput -Force | Out-Null

# Restore the shared library and all three executable applications.
Invoke-DotNet -Arguments @('restore', $libraryProject) -StepName 'Restore AdlpLib'
Invoke-DotNet -Arguments @('restore', $mainProject, '-r', 'win-x64') -StepName 'Restore AdlpdsConfig for win-x64'
Invoke-DotNet -Arguments @('restore', $dbProject, '-r', 'win-x64') -StepName 'Restore AdlpDBConfigurator for win-x64'
Invoke-DotNet -Arguments @('restore', $emulatorProject, '-r', 'win-x64') -StepName 'Restore ADLDeviceEmulator for win-x64'

# Build the library explicitly. It is also copied into the applications that reference it.
Invoke-DotNet -Arguments @('build', $libraryProject, '-c', 'Release', '--no-restore') -StepName 'Build shared library AdlpLib'

$publishCommon = @(
    '-c', 'Release',
    '-r', 'win-x64',
    '--self-contained', 'true',
    '--no-restore',
    '-p:UseAppHost=true',
    '-p:PublishSingleFile=false',
    '-p:PublishTrimmed=false',
    '-p:DebugType=None',
    '-p:DebugSymbols=false'
)

Invoke-DotNet -Arguments (@('publish', $mainProject) + $publishCommon + @('-o', $mainOutput)) -StepName 'Publish main application'
Invoke-DotNet -Arguments (@('publish', $dbProject) + $publishCommon + @('-o', $dbOutput)) -StepName 'Publish database configurator'
Invoke-DotNet -Arguments (@('publish', $emulatorProject) + $publishCommon + @('-o', $emulatorOutput)) -StepName 'Publish device emulator'

$expectedExecutables = @(
    (Join-Path $mainOutput 'AdlpdsConfig.exe'),
    (Join-Path $dbOutput 'AdlpDBConfigurator.exe'),
    (Join-Path $emulatorOutput 'ADLDeviceEmulator.exe')
)
foreach ($exe in $expectedExecutables) {
    if (-not (Test-Path -LiteralPath $exe)) {
        throw ("Expected executable was not produced: " + $exe)
    }
}

Write-Host ''
Write-Host 'All four projects were built; all three applications were published.' -ForegroundColor Green
Write-Host ("Release directory: " + $output) -ForegroundColor Green
Write-Host 'The emulator is included in DeviceEmulator. Test on a clean Windows 10 x64 machine before delivery.' -ForegroundColor Yellow
