[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot

& (Join-Path $PSScriptRoot "build.ps1") -Configuration Release
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}

$testExecutable = Join-Path $repositoryRoot "tests\VideoHarvester.Core.Tests\bin\Release\VideoHarvester.Core.Tests.exe"
& $testExecutable
exit $LASTEXITCODE
