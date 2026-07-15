[CmdletBinding()]
param(
    [switch]$SkipSetup
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Nevma.Local.ps1')

$root = Get-NevmaRepositoryRoot
Push-Location $root
try {
    if (-not $SkipSetup) {
        & (Join-Path $PSScriptRoot 'setup-local.ps1')
    }

    $values = Initialize-NevmaEnvironment
    Set-NevmaProcessEnvironment -Values $values

    $localPath = Join-Path $root '.local'
    $logsPath = Join-Path $localPath 'logs'
    New-Item -ItemType Directory -Force -Path $logsPath | Out-Null

    $pidPath = Join-Path $localPath 'backend-processes.json'
    if (Test-Path -LiteralPath $pidPath) {
        $running = @(Get-Content -Raw $pidPath | ConvertFrom-Json | Where-Object {
            Get-Process -Id $_.ProcessId -ErrorAction SilentlyContinue
        })
        if ($running.Count -gt 0) {
            throw 'Nevma backend processes are already running. Use scripts/stop-local.ps1 first.'
        }
    }

    $services = @(
        @{ Name = 'identity'; Directory = 'src/Services/Identity/Nevma.Identity.Api'; Assembly = 'Nevma.Identity.Api.dll'; Urls = 'https://localhost:7293;http://localhost:5016'; Health = 'https://localhost:7293/health' }
        @{ Name = 'planning'; Directory = 'src/Services/Planning/Nevma.Planning.Api'; Assembly = 'Nevma.Planning.Api.dll'; Urls = 'http://localhost:5276'; Health = 'http://localhost:5276/health' }
        @{ Name = 'messaging'; Directory = 'src/Services/Messaging/Nevma.Messaging.Api'; Assembly = 'Nevma.Messaging.Api.dll'; Urls = 'http://localhost:5085'; Health = 'http://localhost:5085/health' }
        @{ Name = 'notifications'; Directory = 'src/Services/Notifications/Nevma.Notifications.Api'; Assembly = 'Nevma.Notifications.Api.dll'; Urls = 'http://localhost:5095'; Health = 'http://localhost:5095/health' }
        @{ Name = 'files'; Directory = 'src/Services/Files/Nevma.Files.Api'; Assembly = 'Nevma.Files.Api.dll'; Urls = 'http://localhost:5106'; Health = 'http://localhost:5106/health' }
        @{ Name = 'commands'; Directory = 'src/Services/Commands/Nevma.Commands.Api'; Assembly = 'Nevma.Commands.Api.dll'; Urls = 'http://localhost:5116'; Health = 'http://localhost:5116/health' }
        @{ Name = 'gateway'; Directory = 'src/Gateway/Nevma.Gateway'; Assembly = 'Nevma.Gateway.dll'; Urls = 'http://localhost:5033'; Health = 'http://localhost:5033/health' }
    )

    $started = @()
    foreach ($service in $services) {
        $workingDirectory = Join-Path $root $service.Directory
        $assemblyPath = Join-Path $workingDirectory "bin/Debug/net10.0/$($service.Assembly)"
        if (-not (Test-Path -LiteralPath $assemblyPath)) {
            throw "Missing build output $assemblyPath. Run scripts/setup-local.ps1 first."
        }

        $stdout = Join-Path $logsPath "$($service.Name).out.log"
        $stderr = Join-Path $logsPath "$($service.Name).err.log"
        $processArguments = @{
            FilePath = 'dotnet'
            ArgumentList = @($assemblyPath, '--urls', $service.Urls)
            WorkingDirectory = $workingDirectory
            WindowStyle = 'Hidden'
            RedirectStandardOutput = $stdout
            RedirectStandardError = $stderr
            PassThru = $true
        }
        $process = Start-Process @processArguments
        $started += [pscustomobject]@{
            Name = $service.Name
            ProcessId = $process.Id
            Health = $service.Health
        }
    }

    $started | ConvertTo-Json | Set-Content -Encoding UTF8 $pidPath

    $deadline = [DateTime]::UtcNow.AddSeconds(120)
    $pending = @($started)
    while ($pending.Count -gt 0 -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Seconds 2
        $pending = @($pending | Where-Object {
            & curl.exe --fail --silent --insecure --output NUL --max-time 3 $_.Health 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "$($_.Name) is healthy" -ForegroundColor Green
                $false
            }
            else {
                $true
            }
        })
    }

    if ($pending.Count -gt 0) {
        $names = ($pending | ForEach-Object Name) -join ', '
        throw "Services did not become healthy: $names. Check .local/logs."
    }

    Write-Host 'Nevma backend is running at http://localhost:5033' -ForegroundColor Green
}
catch {
    Write-Error $_
    throw
}
finally {
    Pop-Location
}
