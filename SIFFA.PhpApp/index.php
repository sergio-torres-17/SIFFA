<?php
declare(strict_types=1);

/**
 * SIFFA (PHP) · punto de entrada. Todas las rutas pasan por aquí (ver .htaccess).
 */

use Siffa\Core\Env;
use Siffa\Core\Http;
use Siffa\Core\Router;
use Siffa\Core\View;

define('BASE_PATH', __DIR__);

if (PHP_VERSION_ID < 80000) {
    http_response_code(500);
    exit('SIFFA necesita PHP 8.0 o superior (versión actual: ' . PHP_VERSION . '). Cámbiala en el panel de tu hosting.');
}

$autoload = BASE_PATH . '/vendor/autoload.php';
if (!is_file($autoload)) {
    http_response_code(500);
    exit('Falta la carpeta vendor/. Sube el paquete completo de SIFFA (o ejecuta "composer install").');
}
require $autoload;

Env::load(BASE_PATH . '/.env');

$debug = in_array(strtolower(Env::get('APP_ENV', 'production') ?? ''), ['local', 'dev', 'development'], true);
// En producción no se registran los avisos "Deprecated" (las librerías de vendor/ los generan con PHP 8.4+).
error_reporting($debug ? E_ALL : (E_ALL & ~E_DEPRECATED & ~E_USER_DEPRECATED));
ini_set('display_errors', $debug ? '1' : '0');
ini_set('log_errors', '1');
$logDir = BASE_PATH . '/storage/logs';
if (!is_dir($logDir)) {
    @mkdir($logDir, 0775, true);
}
if (is_dir($logDir) && is_writable($logDir)) {
    ini_set('error_log', $logDir . '/php-error.log');
}

date_default_timezone_set(Env::get('APP_TIMEZONE', 'America/Mexico_City') ?? 'UTC');
mb_internal_encoding('UTF-8');

\Siffa\Core\Log::registerFatalHandler();

Http::init();
Http::startSession();

// Cabeceras de seguridad básicas.
header('X-Content-Type-Options: nosniff');
header('X-Frame-Options: SAMEORIGIN');
header('Referrer-Policy: same-origin');
header("Permissions-Policy: camera=(self), microphone=(), geolocation=()"); // la cámara la usa el lector de QR / barras

$router = new Router();
(require BASE_PATH . '/app/routes.php')($router);

try {
    $path = Http::path();

    // Sin .env o sin base de datos lista, todo lleva a la instalación (salvo ella misma).
    if (!str_starts_with($path, '/setup') && !str_starts_with($path, '/api/') && !\Siffa\Core\Auth::check()) {
        $status = \Siffa\Services\Setup::status();
        if (!$status['connected'] || !$status['hasTables']) {
            Http::redirect('/setup');
        }
    }

    $router->dispatch(Http::method(), $path);
} catch (\Throwable $e) {
    error_log((string)$e);
    \Siffa\Core\Log::error('Error no controlado en ' . Http::method() . ' ' . ($path ?? '?'), [], $e);
    http_response_code(500);

    if (Http::wantsJson()) {
        Http::json(['isSuccess' => false, 'message' => $debug ? $e->getMessage() : 'Ocurrió un error en el servidor.'], 500);
    }

    View::render('error', [
        'title' => 'Error',
        'code' => 500,
        'message' => $debug ? $e->getMessage() : 'Ocurrió un error inesperado. Intenta de nuevo o revisa la carpeta storage/logs.',
    ], 'auth');
}
