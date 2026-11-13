param(
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.0.24f1\Editor\Unity.exe',
    [string]$ValidationProject = (Join-Path $env:TEMP ('GameEnginesProject1-check-' + [guid]::NewGuid().ToString('N'))),
    [switch]$Build
)
$ErrorActionPreference = 'Stop'
$sourceProject = Split-Path -Parent $PSScriptRoot
if (Test-Path -LiteralPath $ValidationProject) {
    throw 'Bitte einen neuen, noch nicht existierenden Prüfpfad verwenden.'
}
New-Item -ItemType Directory -Path $ValidationProject | Out-Null
foreach ($part in @('Assets', 'Packages', 'ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $sourceProject $part) -Destination $ValidationProject -Recurse
}
$editorFolder = Join-Path $ValidationProject 'Assets\Editor'
New-Item -ItemType Directory -Path $editorFolder -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $ValidationProject 'Logs') | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot 'Validation\*.cs') -Destination $editorFolder
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Editor\SubmissionSetup.cs') -Destination $editorFolder

function Invoke-UnityCheck([string]$Method, [string]$LogName) {
    $logPath = Join-Path $ValidationProject ('Logs\' + $LogName)
    $arguments = @('-batchmode', '-projectPath', ('"' + $ValidationProject + '"'),
        '-executeMethod', $Method, '-logFile', ('"' + $logPath + '"'))
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(900000)) {
        Stop-Process -Id $process.Id
        throw ('Zeitlimit für ' + $Method + '. Protokoll: ' + $logPath)
    }
    if ($process.ExitCode -ne 0) { throw ('Prüfung fehlgeschlagen: ' + $logPath) }
    Write-Output ('Bestanden: ' + $Method)
}

Invoke-UnityCheck 'SubmissionChecks.Validate' 'SubmissionValidation.log'
Invoke-UnityCheck 'SubmissionChecks.ReadPersistenceProbe' 'SubmissionPersistence.log'
if ($Build) { Invoke-UnityCheck 'SubmissionSetup.BuildWindows' 'SubmissionBuild.log' }
Write-Output ('Prüfkopie und Protokolle: ' + $ValidationProject)
