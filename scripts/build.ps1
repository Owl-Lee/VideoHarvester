[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot "VideoHarvester.sln"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

if (-not (Test-Path -LiteralPath $msbuild))
{
    throw "MSBuild for .NET Framework was not found. Install Visual Studio Build Tools with the .NET desktop workload."
}

& $msbuild $solution /nologo /m /t:Build /p:Configuration=$Configuration /p:Platform="Any CPU"
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}

$output = Join-Path $repositoryRoot "src\VideoHarvester.App\bin\$Configuration\VideoHarvester.exe"
$artifactDirectory = Join-Path $repositoryRoot "artifacts\bin"
$artifact = Join-Path $artifactDirectory "VideoHarvester.exe"

New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null
Copy-Item -LiteralPath $output -Destination $artifact -Force

$configurationFile = "$output.config"
if (Test-Path -LiteralPath $configurationFile)
{
    Copy-Item -LiteralPath $configurationFile -Destination "$artifact.config" -Force
}

Write-Host "Built: $artifact"
