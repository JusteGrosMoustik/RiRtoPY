param(
    [string]$RhinoRoot = 'C:\Program Files\Rhino 7',
    [string]$ReferenceDirectory = ''
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'VERSION doit etre au format major.minor.patch.' }
foreach ($relative in @('src\Models.cs','src\UpdateService.cs','installer\Setup.cs','src\Plugin.cs')) {
    if (![IO.File]::ReadAllText((Join-Path $root $relative)).Contains('"' + $version)) {
        throw "Version incoherente dans $relative. Utiliser scripts/set-version.ps1."
    }
}
$dist = Join-Path $root 'dist'
$payload = Join-Path $dist 'plugin'
New-Item -ItemType Directory -Force -Path $payload | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Compilateur .NET Framework introuvable.' }
if ($ReferenceDirectory) {
    $rhino = Join-Path $ReferenceDirectory 'RhinoCommon.dll'
    $gh = Join-Path $ReferenceDirectory 'Grasshopper.dll'
    $ghio = Join-Path $ReferenceDirectory 'GH_IO.dll'
} else {
    $rhino = Join-Path $RhinoRoot 'System\RhinoCommon.dll'
    $gh = Join-Path $RhinoRoot 'Plug-ins\Grasshopper\Grasshopper.dll'
    $ghio = Join-Path $RhinoRoot 'Plug-ins\Grasshopper\GH_IO.dll'
}
foreach ($reference in @($rhino,$gh,$ghio)) {
    if (!(Test-Path -LiteralPath $reference)) { throw "Reference introuvable : $reference" }
}
$sources = @(Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName)
$plugin = Join-Path $payload 'RiRtoPY.gha'
$argsCompiler = @('/nologo','/target:library','/optimize+','/codepage:65001',"/out:$plugin", "/reference:$rhino", "/reference:$gh", "/reference:$ghio", '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll', '/reference:System.Runtime.Serialization.dll', '/reference:System.Net.Http.dll') + $sources
$argsCompiler += '/resource:' + (Join-Path $root 'python\rir_nodes.py') + ',RiRtoPY.rir_nodes.py'
& $compiler @argsCompiler
if ($LASTEXITCODE -ne 0) { throw 'Echec de compilation du plugin.' }
$setup = Join-Path $dist "RiRtoPY-Setup-$version.exe"
& $compiler /nologo /target:winexe /optimize+ /codepage:65001 "/out:$setup" "/resource:$plugin,RiRtoPY.gha" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $root 'installer\Setup.cs')
if ($LASTEXITCODE -ne 0) { throw 'Echec de compilation de l installation.' }
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination $payload -Force
Copy-Item -LiteralPath (Join-Path $root 'VERSION') -Destination $payload -Force
Copy-Item -LiteralPath (Join-Path $root 'python') -Destination $payload -Recurse -Force
$zip = Join-Path $dist "RiRtoPY-Plugin-$version.zip"
Compress-Archive -Path (Join-Path $payload '*') -DestinationPath $zip -Force
$runtimeZip = Join-Path $dist "RiRtoPY-Python-$version.zip"
Compress-Archive -Path (Join-Path $root 'python'), (Join-Path $root 'docs\GENERAL_NODES.md') -DestinationPath $runtimeZip -Force
$sums = @($setup,$zip,$runtimeZip) | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + (Split-Path $_ -Leaf) }
[IO.File]::WriteAllLines((Join-Path $dist 'SHA256SUMS.txt'), $sums, (New-Object Text.UTF8Encoding($false)))
Write-Output "Compile : $setup"
