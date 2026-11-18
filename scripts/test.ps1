param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$work = Join-Path $root 'work\tests'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe = Join-Path $work 'GraphReportTests.exe'
& $compiler /nologo /codepage:65001 "/out:$exe" /reference:System.Runtime.Serialization.dll (Join-Path $root 'src\Models.cs') (Join-Path $root 'src\GraphReportEmitter.cs') (Join-Path $root 'src\GraphAnalysis.cs') (Join-Path $root 'src\CompactReport.cs') (Join-Path $root 'src\ExportPrivacy.cs') (Join-Path $root 'src\ScriptExtractor.cs') (Join-Path $root 'tests\GraphReportTests.cs') (Join-Path $root 'tests\AdvancedTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compilation des tests impossible.' }
& $exe $work
if ($LASTEXITCODE -ne 0) { throw 'Tests du releve en echec.' }
