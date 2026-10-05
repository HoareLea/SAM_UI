# SPDX-License-Identifier: LGPL-3.0-or-later
# Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors
# Clone https://github.com/SAM-BIM/SAM_UI.wiki.git, run this script with
# -WikiDirectory pointing to that clone, review its diff, then commit and push
# the Wiki clone. Re-run after changing the repository guide; do not edit the
# Wiki copy of the guide independently.

param(
    [Parameter(Mandatory = $true)]
    [string]$WikiDirectory
)

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repositoryRoot 'documentation/user-guides/Part-O-Prepare-and-Run.md'
$wikiRoot = (Resolve-Path -LiteralPath $WikiDirectory).Path
$wikiGit = Join-Path $wikiRoot '.git'
$homePath = Join-Path $wikiRoot 'Home.md'
$pagePath = Join-Path $wikiRoot 'Part-O-Prepare-and-Run.md'
$homeLink = '- [Part O — Prepare & Run](Part-O-Prepare-and-Run)'

if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Guide not found: $source"
}
if (-not (Test-Path -LiteralPath $wikiGit) -or -not (Test-Path -LiteralPath $homePath -PathType Leaf)) {
    throw "Expected a SAM_UI Wiki clone containing Home.md: $wikiRoot"
}

# The repository guide is the sole maintained copy. The Wiki page is replaced
# byte-for-byte on each publication; existing ribbon and shortcut pages are untouched.
Copy-Item -LiteralPath $source -Destination $pagePath -Force

$homeContent = Get-Content -LiteralPath $homePath -Raw
if (-not $homeContent.Contains($homeLink)) {
    if ($homeContent -notmatch '(?m)^## User Guides\s*$') {
        throw "Home.md has no User Guides section: $homePath"
    }
    $homeContent = [regex]::Replace($homeContent, '(?m)^## User Guides\s*$', "## User Guides`n`n$homeLink", 1)
    [System.IO.File]::WriteAllText($homePath, $homeContent, [System.Text.UTF8Encoding]::new($false))
}

foreach ($page in Get-ChildItem -LiteralPath $wikiRoot -Filter '*.md' -File) {
    $pageContent = Get-Content -LiteralPath $page.FullName -Raw
    foreach ($match in [regex]::Matches($pageContent, '\[[^\]]+\]\(([^)]+)\)')) {
        $destination = $match.Groups[1].Value
        if ($destination -match '^(?:https?://|mailto:|#)') {
            continue
        }
        $slug = ($destination -split '[#?]', 2)[0]
        $linkedPage = if ($slug.EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase)) {
            Join-Path $wikiRoot $slug
        } else {
            Join-Path $wikiRoot "$slug.md"
        }
        if (-not (Test-Path -LiteralPath $linkedPage -PathType Leaf)) {
            throw "Broken Wiki link in $($page.Name): $destination"
        }
    }
}

Write-Output "Synced $source to $pagePath and linked it from $homePath"
