param(
    [Parameter(Mandatory=$true)][string]$ReferenceFile,
    [string]$RhinoRoot='C:\Program Files\Rhino 7',
    [switch]$RampeBaseline
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$work=Join-Path $root 'work\reference-test'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$ghio=Join-Path $RhinoRoot 'Plug-ins\Grasshopper\GH_IO.dll'
$exe=Join-Path $work 'ReferenceArchiveTests.exe'
& $compiler /nologo /codepage:65001 "/out:$exe" /reference:System.Runtime.Serialization.dll "/reference:$ghio" (Join-Path $root 'src\Models.cs') (Join-Path $root 'src\ScriptExtractor.cs') (Join-Path $root 'tests\ReferenceArchiveTests.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation du test archive impossible.'}
Copy-Item -LiteralPath $ghio -Destination $work -Force
$testArgs=@($ReferenceFile,(Join-Path $work 'summary.txt'))
if($RampeBaseline){$testArgs+='--rampe-baseline'}
& $exe @testArgs
if($LASTEXITCODE -ne 0){throw 'Regression du fichier de reference.'}
