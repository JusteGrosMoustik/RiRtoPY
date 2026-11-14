param([Parameter(Mandatory=$true)][string]$SourceDirectory)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$paths = Get-Content (Join-Path $root 'python\source-manifest.txt')
$sourcePaths = @{}
foreach ($path in $paths) { $sourcePaths[$path.Replace('/','_')] = $path }
$revision = 'faa6a97653e9da17c6779103ac39537efff60da8'
$emitter = [IO.File]::ReadAllText((Join-Path $root 'src\GeneralRevitEmitter.cs'))
$automatic = @{}
foreach ($m in [regex]::Matches($emitter, 'new Rule\("([^"\r\n]+)", "([0-9a-f-]+)"')) { $automatic[$m.Groups[2].Value] = $m.Groups[1].Value }
$helpers = @{
    'QueryLevels'='query_levels'; 'QuerySheets'='query_sheets'; 'QueryMaterials'='query_materials'
    'QueryPhases'='query_phases'; 'QueryViewElements'='query_view_elements'; 'QueryTitleBlocks'='query_title_blocks'
    'ElementPropertyType'='element_type'; 'ElementIdentity'='element_identity'; 'ElementMaterials'='element_materials'
    'ElementMaterialQuantities'='material_quantities'; 'QueryElementParameters'='element_parameters'
    'ElementParameter'='element_parameter / set_parameter'; 'ElementClassFilter'='class_filter'
    'ElementLogicalAndFilter'='logical_and'; 'ElementLogicalOrFilter'='logical_or'
}
$nodes = @()
foreach ($file in (Get-ChildItem -LiteralPath $SourceDirectory -Filter '*.cs' | Sort-Object Name)) {
    $text = [IO.File]::ReadAllText($file.FullName)
    $classes = [regex]::Matches($text, 'public\s+(?:(?:abstract|sealed|partial)\s+)*class\s+(\w+)\s*:\s*([^\r\n{]+)')
    for ($i=0; $i -lt $classes.Count; $i++) {
        $m = $classes[$i]
        $end = if ($i+1 -lt $classes.Count) { $classes[$i+1].Index } else { $text.Length }
        $body = $text.Substring($m.Index,$end-$m.Index)
        $guid = [regex]::Match($body, 'ComponentGuid\s*=>\s*new\s+(?:System\.)?Guid\("([0-9A-Fa-f-]+)"')
        if (!$guid.Success) { continue }
        $namespaces = [regex]::Matches($text.Substring(0,$m.Index), 'namespace\s+([\w.]+)')
        $namespace = $namespaces[$namespaces.Count-1].Groups[1].Value
        if (!$namespace.Contains('.')) {
            $outer = @($namespaces | Where-Object { $_.Groups[1].Value.Contains('.') })[-1].Groups[1].Value
            $namespace = "$outer.$namespace"
        }
        $id = $guid.Groups[1].Value.ToLowerInvariant()
        $path = $sourcePaths[$file.Name]
        if (!$path) { throw "Source file not present in pinned manifest: $($file.Name)" }
        $status='not_translated'; $function=$null; $scope='No generated or manual implementation claimed.'
        if ($automatic.ContainsKey($id)) {
            $status='graph_subset'; $function=$automatic[$id]; $scope='Read-only subset; strict port signatures, RiR 1.35, one flat element list. See GENERAL_NODES.md.'
        } elseif ($helpers.ContainsKey($m.Groups[1].Value)) {
            $status='manual_helper_subset'; $function=$helpers[$m.Groups[1].Value]; $scope='Callable helper only; native DB values and documented restrictions; no general graph conversion.'
        } elseif ($m.Groups[1].Value -eq 'QueryRooms') {
            $status='room_chain_only'; $function='RoomAreaEmitter'; $scope='Exact five-node room/surface profile only.'
        }
        $nodes += [pscustomobject]@{ type="$namespace.$($m.Groups[1].Value)"; component_guid=$id; source_file=$path; source_url="https://github.com/mcneel/rhino.inside-revit/blob/$revision/$path"; status=$status; implementation=$function; scope=$scope }
    }
}
$duplicates=@($nodes | Group-Object component_guid | Where-Object Count -gt 1)
if ($duplicates.Count) { throw 'Duplicate component GUID in source inventory.' }
$catalog = [ordered]@{ schema='rir-to-python/catalog/1'; source_revision=$revision; source_files=@(Get-ChildItem $SourceDirectory -Filter '*.cs').Count; note='Source inventory of concrete GUID-bearing components, not a claim of universal compatibility. API doubles are not Revit validation.'; nodes=@($nodes | Sort-Object type) }
$json = $catalog | ConvertTo-Json -Depth 6
[IO.File]::WriteAllText((Join-Path $root 'python\node-catalog.json'),$json,(New-Object Text.UTF8Encoding($false)))
$nodes | Group-Object status | Sort-Object Name | Select-Object Count,Name
