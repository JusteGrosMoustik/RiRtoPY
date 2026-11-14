param([string]$IronPythonDirectory = 'C:\Program Files\Rhino 7\Plug-ins\IronPython')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$catalog = Get-Content (Join-Path $root 'python\node-catalog.json') -Raw | ConvertFrom-Json
$paths = Get-Content (Join-Path $root 'python\source-manifest.txt')
if (@($catalog.nodes | Group-Object component_guid | Where-Object Count -gt 1).Count) { throw 'Catalogue : GUIDs dupliques.' }
if (@($catalog.nodes | Where-Object { $_.source_file -notin $paths }).Count) { throw 'Catalogue : chemin de source invalide.' }
Write-Output "PASS: catalogue $($catalog.nodes.Count) composants, GUIDs uniques, sources officielles du manifeste."
$work = Join-Path $root 'work\tests'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe = Join-Path $work 'CoreTests.exe'
& $compiler /nologo /codepage:65001 "/out:$exe" /reference:System.Runtime.Serialization.dll /reference:System.Net.Http.dll /reference:System.Windows.Forms.dll (Join-Path $root 'src\Models.cs') (Join-Path $root 'src\PythonEmitter.cs') (Join-Path $root 'src\RoomAreaEmitter.cs') (Join-Path $root 'src\GeneralRevitEmitter.cs') (Join-Path $root 'src\UpdateService.cs') (Join-Path $root 'tests\CoreTests.cs') (Join-Path $root 'tests\RoomAreaTests.cs') (Join-Path $root 'tests\GeneralRevitTests.cs') ("/resource:$(Join-Path $root 'python\rir_nodes.py'),RiRtoPY.rir_nodes.py")
if ($LASTEXITCODE -ne 0) { throw 'Compilation des tests impossible.' }
& $exe $work
if ($LASTEXITCODE -ne 0) { throw 'Tests C# en echec.' }
$python = Get-Command python -ErrorAction SilentlyContinue
if ($python) {
    & $python.Source (Join-Path $root 'tests\test_generated.py') $work
} elseif (Test-Path -LiteralPath (Join-Path $IronPythonDirectory 'IronPython.dll')) {
    foreach ($name in @('IronPython.dll','Microsoft.Scripting.dll','Microsoft.Dynamic.dll')) {
        Copy-Item -LiteralPath (Join-Path $IronPythonDirectory $name) -Destination $work -Force
    }
    $runner = Join-Path $work 'RunIronPython.exe'
    & $compiler /nologo "/out:$runner" "/reference:$(Join-Path $work 'IronPython.dll')" "/reference:$(Join-Path $work 'Microsoft.Scripting.dll')" (Join-Path $root 'tests\RunIronPython.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Compilation du test Python impossible.' }
    & $runner (Join-Path $root 'tests\test_generated.py') $work
} else { throw 'Python ou IronPython requis pour verifier le code genere.' }
if ($LASTEXITCODE -ne 0) { throw 'Execution du Python genere en echec.' }
