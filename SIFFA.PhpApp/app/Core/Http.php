<?php
declare(strict_types=1);

namespace Siffa\Core;

/** Petición y respuestas HTTP, sesión y CSRF. */
final class Http
{
    private static string $basePath = '';

    public static function init(): void
    {
        // Carpeta donde vive index.php (vacía si está en la raíz del dominio).
        $dir = str_replace('\\', '/', dirname($_SERVER['SCRIPT_NAME'] ?? '/'));
        self::$basePath = $dir === '/' || $dir === '.' ? '' : rtrim($dir, '/');
    }

    public static function basePath(): string
    {
        return self::$basePath;
    }

    public static function isHttps(): bool
    {
        return (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off')
            || ($_SERVER['HTTP_X_FORWARDED_PROTO'] ?? '') === 'https'
            || (int)($_SERVER['SERVER_PORT'] ?? 0) === 443;
    }

    /** URL relativa a la carpeta de la aplicación: url('/users') → /siffa/users */
    public static function url(string $path = '/'): string
    {
        return self::$basePath . '/' . ltrim($path, '/');
    }

    public static function method(): string
    {
        return strtoupper($_SERVER['REQUEST_METHOD'] ?? 'GET');
    }

    /** Ruta pedida, sin la carpeta base ni la cadena de consulta. */
    public static function path(): string
    {
        $uri = parse_url($_SERVER['REQUEST_URI'] ?? '/', PHP_URL_PATH) ?: '/';
        $uri = rawurldecode($uri);
        if (self::$basePath !== '' && str_starts_with($uri, self::$basePath)) {
            $uri = substr($uri, strlen(self::$basePath));
        }
        $uri = '/' . trim($uri, '/');
        // Si se entra directo a /index.php, se trata como la raíz.
        return $uri === '/index.php' ? '/' : $uri;
    }

    public static function wantsJson(): bool
    {
        $accept = $_SERVER['HTTP_ACCEPT'] ?? '';
        $type = $_SERVER['CONTENT_TYPE'] ?? '';
        return str_contains($accept, 'application/json')
            || str_contains($type, 'application/json')
            || self::path() !== '/' && str_starts_with(self::path(), '/api/');
    }

    /** Cuerpo JSON de la petición, o $_POST si no es JSON. */
    public static function input(): array
    {
        $type = $_SERVER['CONTENT_TYPE'] ?? '';
        if (str_contains($type, 'application/json')) {
            $decoded = json_decode(file_get_contents('php://input') ?: '', true);
            return is_array($decoded) ? $decoded : [];
        }
        return $_POST;
    }

    public static function json(mixed $data, int $status = 200): never
    {
        http_response_code($status);
        header('Content-Type: application/json; charset=utf-8');
        header('Cache-Control: no-store');
        echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES | JSON_PARTIAL_OUTPUT_ON_ERROR);
        exit;
    }

    public static function redirect(string $path): never
    {
        header('Location: ' . (preg_match('#^https?://#', $path) ? $path : self::url($path)));
        exit;
    }

    // ---------- sesión ----------

    public static function startSession(): void
    {
        if (session_status() === PHP_SESSION_ACTIVE) {
            return;
        }

        $lifetime = Env::int('SESSION_LIFETIME_HOURS', 8) * 3600;
        // Carpeta propia para las sesiones: en hosting compartido la carpeta global se limpia seguido.
        $dir = BASE_PATH . '/storage/sessions';
        if (!is_dir($dir)) {
            @mkdir($dir, 0775, true);
        }
        if (is_dir($dir) && is_writable($dir)) {
            session_save_path($dir);
        }

        ini_set('session.gc_maxlifetime', (string)$lifetime);
        ini_set('session.use_strict_mode', '1');
        ini_set('session.use_only_cookies', '1');
        session_name('SIFFASESSID');
        session_set_cookie_params([
            'lifetime' => 0,
            'path' => self::$basePath === '' ? '/' : self::$basePath . '/',
            'secure' => self::isHttps(),
            'httponly' => true,
            'samesite' => 'Lax',
        ]);
        session_start();

        // Caducidad por inactividad.
        $now = time();
        if (isset($_SESSION['last_seen']) && $now - (int)$_SESSION['last_seen'] > $lifetime) {
            $_SESSION = [];
            session_regenerate_id(true);
        }
        $_SESSION['last_seen'] = $now;
    }

    public static function csrfToken(): string
    {
        if (empty($_SESSION['csrf'])) {
            $_SESSION['csrf'] = bin2hex(random_bytes(32));
        }
        return $_SESSION['csrf'];
    }

    /** Valida el token CSRF (campo _csrf o cabecera X-CSRF-Token). */
    public static function checkCsrf(): bool
    {
        $sent = $_POST['_csrf'] ?? $_SERVER['HTTP_X_CSRF_TOKEN'] ?? '';
        return is_string($sent) && $sent !== '' && hash_equals(self::csrfToken(), $sent);
    }

    // ---------- mensajes de una sola vez ----------

    public static function flash(string $type, string $message): void
    {
        $_SESSION['flash'][] = ['type' => $type, 'message' => $message];
    }

    /** @return list<array{type:string,message:string}> */
    public static function takeFlash(): array
    {
        $flash = $_SESSION['flash'] ?? [];
        unset($_SESSION['flash']);
        return $flash;
    }
}
