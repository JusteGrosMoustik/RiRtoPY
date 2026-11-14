param([Parameter(Mandatory=$true)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$old = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
foreach ($relative in @('src\Models.cs','src\Plugin.cs','src\UpdateService.cs','installer\Setup.cs','README.md','docs\RELEASE_NOTES.md')) {
    $path = Join-Path $root $relative
    $text = [IO.File]::ReadAllText($path).Replace($old,$Version)
    [IO.File]::WriteAllText($path,$text,(New-Object Text.UTF8Encoding($false)))
}
[IO.File]::WriteAllText((Join-Path $root 'VERSION'),$Version + "`n",(New-Object Text.UTF8Encoding($false)))
Write-Output "Version $Version. Mettre a jour les notes, executer build/test, puis publier le commit sur main."
