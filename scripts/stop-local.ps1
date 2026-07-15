[CmdletBinding()]
param(
    [switch]$Infrastructure
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Nevma.Local.ps1')

$root = Get-NevmaRepositoryRoot
$pidPath = Join-Path $root '.local/backend-processes.json'

if (Test-Path -LiteralPath $pidPath) {
    $entries = @(Get-Content -Raw $pidPath | ConvertFrom-Json)
    foreach ($entry in $entries) {
        $process = Get-Process -Id $entry.ProcessId -ErrorAction SilentlyContinue
        if ($process -and $process.ProcessName -eq 'dotnet') {
            Stop-Process -Id $entry.ProcessId -ErrorAction SilentlyContinue
            Write-Host "Stopped $($entry.Name)" -ForegroundColor Yellow
        }
    }
    Remove-Item -LiteralPath $pidPath -Force
}

if ($Infrastructure) {
    Push-Location $root
    try {
        docker compose down
        if ($LASTEXITCODE -ne 0) { throw 'Docker Compose shutdown failed.' }
    }
    finally {
        Pop-Location
    }
}
