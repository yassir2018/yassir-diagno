#Requires -Version 5.1

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$srcProj = Join-Path $root 'src\YassirDiagno\YassirDiagno.csproj'
$publishDir = Join-Path $root 'installer\publish'
$installerOut = Join-Path $root 'installer\out'

Write-Host '=== Step 1: Publish self-contained release ===' -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
& 'C:\Program Files\dotnet\dotnet.exe' publish $srcProj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host ''
Write-Host '=== Step 2: Generate harvest with proper directory structure ===' -ForegroundColor Cyan
$harvestFile = Join-Path $PSScriptRoot 'AppFiles.wxs'

function Sanitize { param([string]$s) ($s -replace '[^A-Za-z0-9_]', '_') }

$dirTree = @{}
$files = Get-ChildItem $publishDir -Recurse -File
foreach ($f in $files) {
    $rel = $f.FullName.Substring($publishDir.Length + 1)
    $relDir = [System.IO.Path]::GetDirectoryName($rel)
    if (-not $dirTree.ContainsKey($relDir)) { $dirTree[$relDir] = @() }
    $dirTree[$relDir] += $f
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
[void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
[void]$sb.AppendLine('  <Fragment>')

$dirIds = @{ '' = 'INSTALLFOLDER' }
$sortedDirs = $dirTree.Keys | Where-Object { $_ -ne '' } | Sort-Object
foreach ($d in $sortedDirs) {
    $id = 'D_' + (Sanitize $d)
    $dirIds[$d] = $id
}

if ($sortedDirs.Count -gt 0) {
    [void]$sb.AppendLine('    <DirectoryRef Id="INSTALLFOLDER">')
    foreach ($d in $sortedDirs) {
        $name = [System.IO.Path]::GetFileName($d)
        $id = $dirIds[$d]
        [void]$sb.AppendLine("      <Directory Id=`"$id`" Name=`"$name`"/>")
    }
    [void]$sb.AppendLine('    </DirectoryRef>')
}

[void]$sb.AppendLine('    <ComponentGroup Id="AppFiles">')

$idx = 0
foreach ($dir in $dirTree.Keys | Sort-Object) {
    $dirId = $dirIds[$dir]
    foreach ($f in $dirTree[$dir]) {
        $idx++
        $rel = $f.FullName.Substring($publishDir.Length + 1)
        $safe = (Sanitize $rel) + "_$idx"
        [void]$sb.AppendLine("      <Component Id=`"C_$safe`" Directory=`"$dirId`" Guid=`"*`">")
        [void]$sb.AppendLine("        <File Id=`"F_$safe`" Source=`"publish\$rel`" KeyPath=`"yes`"/>")
        [void]$sb.AppendLine('      </Component>')
    }
}

[void]$sb.AppendLine('    </ComponentGroup>')
[void]$sb.AppendLine('  </Fragment>')
[void]$sb.AppendLine('</Wix>')
Set-Content -Path $harvestFile -Value $sb.ToString() -Encoding UTF8
Write-Host "  Harvest: $idx files in $($dirTree.Count) directories"

Write-Host ''
Write-Host '=== Step 3: Build MSI ===' -ForegroundColor Cyan
if (Test-Path $installerOut) { Remove-Item $installerOut -Recurse -Force }
New-Item -ItemType Directory -Path $installerOut | Out-Null
$msiPath = Join-Path $installerOut 'YassirDiagno-1.0.0-x64.msi'

$env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"
Push-Location $PSScriptRoot
try {
    & wix build YassirDiagno.wxs AppFiles.wxs -ext WixToolset.UI.wixext -arch x64 -o $msiPath
    if ($LASTEXITCODE -ne 0) { throw "wix build failed (exit $LASTEXITCODE)" }
}
finally { Pop-Location }

$size = [math]::Round((Get-Item $msiPath).Length / 1MB, 2)
Write-Host ''
Write-Host "=== DONE ===" -ForegroundColor Green
Write-Host "MSI: $msiPath ($size MB)" -ForegroundColor Green
