$ErrorActionPreference = 'Stop'
$viewerUrl = 'http://127.0.0.1:8768'
$viewerReady = $false
try {
    $viewerHealth = Invoke-WebRequest "$viewerUrl/health" -UseBasicParsing -TimeoutSec 2
    $viewerReady = $viewerHealth.Content -eq 'EmergencyShiftPlanViewer'
} catch {
    $viewerReady = $false
}

if (-not $viewerReady) {
    $viewerCandidates = @(
        "$env:USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe",
        "$env:LOCALAPPDATA/OpenAI/Codex/runtimes/python/python.exe"
    )
    $viewerPython = $viewerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $viewerPython) {
        $viewerCommand = Get-Command python -ErrorAction SilentlyContinue
        if ($viewerCommand -and $viewerCommand.Source -notlike '*WindowsApps*') {
            $viewerPython = $viewerCommand.Source
        }
    }
    if (-not $viewerPython) {
        throw 'Python 3 не найден. Укажите установленный Python для Tools/PlanViewer/server.py.'
    }
    $viewerServer = Join-Path $PSScriptRoot 'server.py'
    $viewerProcess = Start-Process -FilePath $viewerPython -ArgumentList @(('"' + $viewerServer + '"'), '--port', '8768') -WindowStyle Hidden -PassThru
    for ($viewerAttempt = 0; $viewerAttempt -lt 30; $viewerAttempt++) {
        Start-Sleep -Milliseconds 200
        try {
            $viewerHealth = Invoke-WebRequest "$viewerUrl/health" -UseBasicParsing -TimeoutSec 1
            if ($viewerHealth.Content -eq 'EmergencyShiftPlanViewer') {
                $viewerReady = $true
                break
            }
        } catch {
            if ($viewerProcess.HasExited) {
                break
            }
        }
    }
    if (-not $viewerReady) {
        throw 'Не удалось открыть просмотрщик. Возможно, порт 8768 занят. Запустите server.py вручную для диагностики.'
    }
}
Start-Process "$viewerUrl/?doc=Docs/MVP_PLAN.md"
