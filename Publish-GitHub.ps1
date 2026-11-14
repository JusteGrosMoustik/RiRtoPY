# Run locally after reviewing the sources. Requires Git and GitHub CLI (gh).
param()
$ErrorActionPreference = 'Stop'
$repo = 'JusteGrosMoustik/RiRtoPY'
$root = $PSScriptRoot
foreach ($command in @('git','gh')) {
    if (!(Get-Command $command -ErrorAction SilentlyContinue)) { throw "$command est requis. Installer Git et GitHub CLI, puis relancer." }
}
gh auth status
if ($LASTEXITCODE -ne 0) { throw 'Se connecter avec gh auth login, puis relancer ce script.' }
$identityText = gh api user
if ($LASTEXITCODE -ne 0) { throw 'Impossible de verifier le compte GitHub.' }
$identity = $identityText | ConvertFrom-Json
$version = (Get-Content (Join-Path $root 'VERSION') -Raw).Trim()
$dist = Join-Path $root 'dist'
foreach ($line in (Get-Content (Join-Path $dist 'SHA256SUMS.txt'))) {
    $parts = $line -split '  ',2
    if ($parts.Count -ne 2 -or $parts[1] -notmatch '^RiRtoPY-(Setup|Plugin|Python)-[0-9.]+\.(exe|zip)$') { throw 'Liste des empreintes invalide.' }
    $actual = (Get-FileHash -LiteralPath (Join-Path $dist $parts[1]) -Algorithm SHA256).Hash
    if ($actual -ne $parts[0]) { throw "Empreinte incorrecte : $($parts[1])" }
}
# New checkout: never overwrite a user's existing checkout or force a branch.
$checkout = Join-Path ([IO.Path]::GetTempPath()) ('RiRtoPY-publish-' + [Guid]::NewGuid().ToString('N'))
gh repo clone $repo $checkout -- --branch main
if ($LASTEXITCODE -ne 0) { throw 'Clonage impossible.' }
Get-ChildItem -LiteralPath $root -Force | Where-Object { $_.Name -notin @('.git','work','dist','bin','obj','.vs','local.settings.json') } | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $checkout -Recurse -Force
}
Push-Location $checkout
try {
    git add --all
    if ($LASTEXITCODE -ne 0) { throw 'Preparation du commit impossible.' }
    git diff --cached --quiet
    if ($LASTEXITCODE -eq 1) {
        $email = "$($identity.id)+$($identity.login)@users.noreply.github.com"
        git -c "user.name=$($identity.login)" -c "user.email=$email" commit -m "Add RiRtoPY $version plugin, installer and updater"
        if ($LASTEXITCODE -ne 0) { throw 'Commit impossible.' }
        git push origin HEAD:main
        if ($LASTEXITCODE -ne 0) { throw 'Push refuse. Aucune tentative de force ne sera effectuee.' }
    } elseif ($LASTEXITCODE -ne 0) { throw 'Verification du diff impossible.' }
    $sha = git rev-parse HEAD
    gh release view "v$version" --repo $repo *> $null
    if ($LASTEXITCODE -ne 0) {
        gh release create "v$version" (Join-Path $dist "RiRtoPY-Setup-$version.exe") (Join-Path $dist "RiRtoPY-Plugin-$version.zip") (Join-Path $dist "RiRtoPY-Python-$version.zip") (Join-Path $dist 'SHA256SUMS.txt') --repo $repo --target $sha --title "RiRtoPY $version" --notes-file (Join-Path $root 'docs\RELEASE_NOTES.md') --latest
        if ($LASTEXITCODE -ne 0) { throw 'Sources publiees mais creation de release impossible. Consulter Actions et Releases.' }
    }
    Write-Output "Sources et release disponibles : https://github.com/$repo/releases"
    Write-Output "Copie de travail conservee : $checkout"
} finally { Pop-Location }
