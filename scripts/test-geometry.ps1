param([string]$RhinoRoot='C:\Program Files\Rhino 7',[string]$ReferenceDirectory='')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$work=Join-Path $root 'work\geometry-tests'
New-Item -ItemType Directory -Force -Path $work | Out-Null
if($ReferenceDirectory){$rhino=Join-Path $ReferenceDirectory 'RhinoCommon.dll';$grasshopper=Join-Path $ReferenceDirectory 'Grasshopper.dll';$ghio=Join-Path $ReferenceDirectory 'GH_IO.dll'}
else{$rhino=Join-Path $RhinoRoot 'System\RhinoCommon.dll';$grasshopper=Join-Path $RhinoRoot 'Plug-ins\Grasshopper\Grasshopper.dll';$ghio=Join-Path $RhinoRoot 'Plug-ins\Grasshopper\GH_IO.dll'}
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $work 'GeometryTests.exe'
& $compiler /nologo /codepage:65001 "/out:$exe" /reference:System.Runtime.Serialization.dll "/reference:$rhino" "/reference:$grasshopper" "/reference:$ghio" (Join-Path $root 'src\Models.cs') (Join-Path $root 'src\GeometryCapture.cs') (Join-Path $root 'tests\GeometryTests.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation des tests geometriques impossible.'}
Copy-Item -LiteralPath $rhino,$grasshopper,$ghio -Destination $work -Force
& $exe
if($LASTEXITCODE -ne 0){throw 'Tests geometriques simples en echec.'}
