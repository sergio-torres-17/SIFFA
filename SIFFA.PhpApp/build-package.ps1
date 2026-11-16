# Genera dist\siffa-php.zip listo para subir por FTP / Administrador de archivos del hosting.
# Incluye vendor/ (no hace falta Composer ni SSH en el servidor). No incluye .env.
#   powershell -ExecutionPolicy Bypass -File .\build-package.ps1

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

if (-not (Test-Path "$root\vendor\autoload.php")) {
    Write-Host 'Instalando dependencias (composer install --no-dev)...'
    composer install --no-dev --optimize-autoloader
}

$dist = Join-Path $root 'dist'
$stage = Join-Path $dist 'siffa'
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null

foreach ($item in 'app', 'assets', 'database', 'resources', 'views', 'vendor') {
    Copy-Item (Join-Path $root $item) (Join-Path $stage $item) -Recurse
}
foreach ($file in 'index.php', '.htaccess', '.user.ini', '.env.example', 'composer.json', 'README.md') {
    if (Test-Path (Join-Path $root $file)) { Copy-Item (Join-Path $root $file) $stage }
}
New-Item -ItemType Directory -Path (Join-Path $stage 'storage') | Out-Null
Copy-Item (Join-Path $root 'storage\.htaccess') (Join-Path $stage 'storage')

$zip = Join-Path $dist 'siffa-php.zip'
# Zip propio con rutas "/" (Compress-Archive de PowerShell 5 usa "\" y se rompe en Linux).
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::Open($zip, 'Create')
try {
    $base = (Resolve-Path $stage).Path.TrimEnd('\') + '\'
    Get-ChildItem $stage -Recurse -Force -File | ForEach-Object {
        $entry = $_.FullName.Substring($base.Length).Replace('\', '/')
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $entry, 'Optimal')
    }
} finally { $archive.Dispose() }

Write-Host "Listo: $zip"
