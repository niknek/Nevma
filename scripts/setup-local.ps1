[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Nevma.Local.ps1')

$root = Get-NevmaRepositoryRoot
Push-Location $root
try {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker is not installed or is not available on PATH.'
    }

    docker info *> $null
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker Desktop is installed but its engine is not running.'
    }

    $values = Initialize-NevmaEnvironment
    Set-NevmaProcessEnvironment -Values $values

    Write-Host 'Starting PostgreSQL, RabbitMQ, Redis, and Jaeger...' -ForegroundColor Cyan
    docker compose up -d --wait --wait-timeout 240
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker Compose did not become healthy.'
    }

    if (-not $SkipBuild) {
        dotnet restore Nevma.slnx
        if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

        dotnet build Nevma.slnx --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
    }

    $projects = @(
        'src/Services/Identity/Nevma.Identity.Api/Nevma.Identity.Api.csproj'
        'src/Services/Planning/Nevma.Planning.Api/Nevma.Planning.Api.csproj'
        'src/Services/Messaging/Nevma.Messaging.Api/Nevma.Messaging.Api.csproj'
        'src/Services/Notifications/Nevma.Notifications.Api/Nevma.Notifications.Api.csproj'
        'src/Services/Files/Nevma.Files.Api/Nevma.Files.Api.csproj'
        'src/Services/Commands/Nevma.Commands.Api/Nevma.Commands.Api.csproj'
    )

    foreach ($project in $projects) {
        Write-Host "Migrating $project" -ForegroundColor Cyan
        dotnet ef database update --project $project --startup-project $project --no-build
        if ($LASTEXITCODE -ne 0) {
            throw "Database migration failed for $project"
        }
    }

    Write-Host 'Local infrastructure and databases are ready.' -ForegroundColor Green
    Write-Host 'RabbitMQ UI: http://localhost:15672'
    Write-Host 'Jaeger UI:   http://localhost:16686'
}
finally {
    Pop-Location
}
