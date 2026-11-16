# SIFFA · versión PHP

Misma aplicación web de SIFFA (control de ingresos para la fiesta de fin de año), hecha en **PHP plano + MySQL/MariaDB** para alojarla en hosting compartido (Hostinger, HostGator, etc.) sin SSH ni Composer en el servidor. Toda la configuración va en un archivo `.env`.

Incluye: inicio con indicadores, control de accesos (ingreso/salida + entrega de boletos, con lector de QR y código de barras por cámara), carga de empleados desde Excel, generación de invitaciones (PNG/PDF con QR o código de barras), catálogo de adicionales y usuarios de acceso. También expone la API que usa la app de escritorio en modo "consola web" (`/api/Auth/Login`, `/api/Auth/Ping`, `/api/ManagePlattform/ActivateSystem`).

## Requisitos del hosting

- PHP **8.0 o superior** (se elige en el panel del hosting).
- Extensiones: `pdo_mysql`, `gd` (con FreeType), `zip`, `mbstring`, `fileinfo` (casi todos los hostings las traen activas).
- MySQL 5.7+ o MariaDB 10.3+.
- Apache/LiteSpeed con `.htaccess` (mod_rewrite). En Nginx hay que reenviar todo a `index.php` (ver abajo).

## Instalación

1. **Empaqueta** (en tu PC, una sola vez): `powershell -ExecutionPolicy Bypass -File .\build-package.ps1` → genera `dist/siffa-php.zip` con todo (incluida la carpeta `vendor/`).
2. En el panel del hosting **crea una base de datos MySQL** y un usuario con todos los permisos sobre ella. Anota host, nombre, usuario y contraseña.
3. **Sube y descomprime** `siffa-php.zip` en la carpeta del sitio (`public_html/` o una subcarpeta como `public_html/siffa/`). Funciona en la raíz del dominio o en subcarpeta.
4. Copia `.env.example` como **`.env`** (mismo lugar que `index.php`) y llena `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASS` y una `ADMIN_ACTIVATION_KEY` larga y privada.
5. Abre `https://tu-dominio/` (o `/siffa/`): te llevará a **/setup**.
   - Escribe la clave de activación → *Crear tablas y datos base*.
   - Vuelve a escribirla y define la contraseña del administrador (`ADMIN_EMAIL`, por defecto `adminsiffa@multicodedev.com`).
6. Inicia sesión y crea más usuarios en **Usuarios**.

Alternativa: puedes importar `database/schema.sql` desde phpMyAdmin en lugar de usar el botón de /setup.

## Variables del `.env`

| Variable | Descripción |
|---|---|
| `APP_ENV` | `production` (oculta errores) o `local` (los muestra) |
| `APP_TIMEZONE` | Zona horaria de las horas de acceso (por defecto `America/Mexico_City`) |
| `EVENT_YEAR` | Año en las invitaciones (vacío = año actual) |
| `DB_HOST` `DB_PORT` `DB_NAME` `DB_USER` `DB_PASS` | Conexión MySQL |
| `ADMIN_EMAIL` | Correo del administrador |
| `ADMIN_ACTIVATION_KEY` | Clave para instalar y activar en /setup (también la usa la app de escritorio) |
| `SESSION_LIFETIME_HOURS` | Duración de la sesión |
| `LOGIN_MAX_ATTEMPTS` | Intentos fallidos permitidos cada 15 min |

## Migrar los datos de la versión .NET

La base es distinta (SQL Server → MySQL). Lo más simple: en la app actual exporta/guarda tu Excel de empleados y **súbelo en "Subir empleados"** de esta versión. Los accesos anteriores no se migran.

## Seguridad

- `.htaccess` bloquea `app/`, `vendor/`, `storage/`, `database/`, `views/` y `.env`. Si tu hosting **no** usa Apache, bloquea esas rutas desde el panel/Nginx.
- Todas las peticiones que modifican datos exigen token CSRF; las contraseñas se guardan con `password_hash`; hay límite de intentos de login.
- Si el panel permite apuntar el dominio a una carpeta, apunta a una donde `.env` no sea público, o confía en el `.htaccess`.

### Nginx

```nginx
location / { try_files $uri /index.php?$query_string; }
location ~ ^/(app|vendor|storage|database|views)/ { deny all; }
location ~ /\.env { deny all; }
```

## Problemas comunes

- **Pantalla blanca / error 500**: pon `APP_ENV=local` temporalmente o revisa los logs en `storage/logs/`: `siffa-AAAA-MM-DD.log` (bitácora de la app: errores, generación de tarjetas con tiempos y memoria; se conserva 30 días) y `php-error.log` (errores de PHP). Esa carpeta no es accesible desde internet; ábrela con el administrador de archivos.
- **"No se pudo conectar"** en /setup: en algunos hostings `DB_HOST` no es `localhost` (el panel indica el host).
- **Las tarjetas salen sin texto**: falta soporte FreeType en `gd`; actívalo en el panel de PHP.
- **No sube el Excel**: aumenta `upload_max_filesize` / `post_max_size` (el paquete trae `.user.ini` con 12M/14M).

## Desarrollo local

```bash
composer install
php -S 127.0.0.1:8080 router-dev.php   # o apuntar Apache/XAMPP a esta carpeta
```
