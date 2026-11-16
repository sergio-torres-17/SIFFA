<#
  Empaqueta la app de escritorio con Velopack (genera instalador y paquetes de actualización).

  Uso (desde la carpeta SIFFA.DesktopApp):
    .\pack.ps1 -Version 1.0.1
    .\pack.ps1 -Version 1.0.1 -Output C:\Releases\SIFFA

  Requisitos: dotnet tool install -g vpk   (usa la misma versión que el paquete NuGet Velopack del proyecto)

  Para publicar la actualización en GitHub Releases (la fuente configurada en appsettings.json):
    vpk upload github --repoUrl https://github.com/sergio-torres-17/SIFFA --publish --releaseName "SIFFA <versión>" --tag v<versión> --outputDir <Output> --token <TOKEN>
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Output = (Join-Path $PSScriptRoot 'Releases')
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'SIFFA.DesktopApp.csproj'
$publish = Join-Path $PSScriptRoot 'publish'

if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }

# Autocontenida: el equipo del usuario no necesita instalar .NET.
dotnet publish $project -c Release -r win-x64 --self-contained -p:Version=$Version -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish falló' }

# Si ya hay releases anteriores en $Output, vpk genera además los paquetes delta.
vpk pack --packId SIFFA --packVersion $Version --packDir $publish --mainExe SIFFA.DesktopApp.exe --outputDir $Output --packTitle 'SIFFA'
if ($LASTEXITCODE -ne 0) { throw 'vpk pack falló' }

Write-Host "Listo. Instalador y paquetes en: $Output"
