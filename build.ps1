param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $root "artifacts\msi-files"
$distDir = Join-Path $root "dist"
$msiPath = Join-Path $distDir "DeepCool.Cases.Digital.zh-CN.msi"

New-Item -ItemType Directory -Force $publishDir | Out-Null
New-Item -ItemType Directory -Force $distDir | Out-Null

dotnet publish (Join-Path $root "DeepCool.Cases.Digital\DeepCool.Cases.Digital.csproj") `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o $publishDir

Remove-Item (Join-Path $publishDir "*.pdb") -Force -ErrorAction SilentlyContinue

wix build (Join-Path $root "installer\DeepCool.Cases.Digital.zh-cn.wxs") `
    -arch x64 `
    -ext WixToolset.UI.wixext `
    -culture zh-CN `
    -d SourceDir=$publishDir `
    -o $msiPath

Get-Item $msiPath
