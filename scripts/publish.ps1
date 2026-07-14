<#
.SYNOPSIS
  Empaqueta FerreSys para distribución (self-contained, win-x64).

.EXAMPLE
  .\scripts\publish.ps1
  .\scripts\publish.ps1 -Configuration Release -Runtime win-x64
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$Runtime = "win-x64",

    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "FerreSys.Infrastructure.UI\FerreSys.Infrastructure.UI.csproj"
$outDir = Join-Path $root "artifacts\publish\$Runtime"

# Lee Version desde Directory.Build.props
$propsPath = Join-Path $root "Directory.Build.props"
[xml]$propsXml = Get-Content $propsPath
$version = $propsXml.Project.PropertyGroup.Version
if (-not $version) { $version = "0.0.0" }

Write-Host "Publicando FerreSys v$version ($Configuration / $Runtime)..." -ForegroundColor Cyan

$publishArgs = @(
    "publish", $project,
    "-c", $Configuration,
    "-r", $Runtime,
    "-o", $outDir,
    "/p:PublishSingleFile=true",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:EnableCompressionInSingleFile=true",
    "/p:DebugType=None",
    "/p:DebugSymbols=false"
)

if ($FrameworkDependent) {
    $publishArgs += "/p:SelfContained=false"
    Write-Host "Modo: framework-dependent (requiere .NET Runtime en la máquina destino)" -ForegroundColor Yellow
} else {
    $publishArgs += "/p:SelfContained=true"
    Write-Host "Modo: self-contained (incluye runtime)" -ForegroundColor Green
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# No incluir la DB de desarrollo en el paquete
$db = Join-Path $outDir "ferresys.db"
if (Test-Path $db) {
    Remove-Item $db -Force
    Write-Host "Eliminada ferresys.db del paquete (no debe distribuirse la DB de desarrollo)." -ForegroundColor Yellow
}

$zipName = "FerreSys-v$version-$Runtime.zip"
$zipPath = Join-Path $root "artifacts\$zipName"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Compress-Archive -Path (Join-Path $outDir "*") -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "Listo." -ForegroundColor Green
Write-Host "  Carpeta: $outDir"
Write-Host "  ZIP:     $zipPath"
Write-Host ""
Write-Host "Instalación: descomprimir el ZIP en la máquina destino y ejecutar FerreSys.Infrastructure.UI.exe"
Write-Host "Actualización: ver sección 'Actualizaciones' en README.md (conservar ferresys.db)."
