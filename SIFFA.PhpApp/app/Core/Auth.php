<?php
declare(strict_types=1);

namespace Siffa\Core;

/** Sesión de usuario y verificación de credenciales. */
final class Auth
{
    public static function check(): bool
    {
        return !empty($_SESSION['user']['id']);
    }

    /** @return array{id:string,name:string,email:string}|null */
    public static function user(): ?array
    {
        return self::check() ? $_SESSION['user'] : null;
    }

    public static function id(): ?string
    {
        return $_SESSION['user']['id'] ?? null;
    }

    public static function login(array $user): void
    {
        session_regenerate_id(true);
        $_SESSION['user'] = [
            'id' => $user['user_id'],
            'name' => trim($user['first_name'] . ' ' . $user['last_name']),
            'email' => $user['email'],
        ];
        $_SESSION['csrf'] = bin2hex(random_bytes(32));
    }

    public static function logout(): void
    {
        $_SESSION = [];
        if (ini_get('session.use_cookies')) {
            $p = session_get_cookie_params();
            setcookie(session_name(), '', time() - 42000, $p['path'], $p['domain'] ?? '', $p['secure'], $p['httponly']);
        }
        session_destroy();
    }

    public static function normalizeEmail(string $email): string
    {
        return mb_strtolower(trim($email));
    }

    /**
     * Valida correo y contraseña. Devuelve [usuario|null, mensaje].
     * Incluye limitación de intentos por IP y por correo.
     *
     * @return array{0:?array<string,mixed>,1:string}
     */
    public static function attempt(string $email, string $password): array
    {
        $email = self::normalizeEmail($email);
        $ip = substr($_SERVER['REMOTE_ADDR'] ?? 'cli', 0, 45);
        $max = Env::int('LOGIN_MAX_ATTEMPTS', 8);
        $since = date('Y-m-d H:i:s', time() - 900);

        $recent = (int)Db::value(
            'SELECT COUNT(*) FROM login_attempts WHERE attempted_at > ? AND (ip = ? OR email = ?)',
            [$since, $ip, $email]
        );
        if ($recent >= $max) {
            return [null, 'Demasiados intentos fallidos. Espera 15 minutos e intenta de nuevo.'];
        }

        $row = Db::one(
            'SELECT u.user_id, u.first_name, u.last_name, u.email, c.password_hash
               FROM users u JOIN credentials c ON c.user_id = u.user_id
              WHERE u.email = ? ORDER BY c.created_at DESC LIMIT 1',
            [$email]
        );

        // Se verifica siempre contra un hash (aunque el usuario no exista) para igualar tiempos de respuesta.
        $hash = $row['password_hash'] ?? password_hash('relleno-' . bin2hex(random_bytes(4)), PASSWORD_DEFAULT);
        $valid = password_verify($password, $hash) && $row !== null;

        if (!$valid) {
            Db::run('INSERT INTO login_attempts (ip, email, attempted_at) VALUES (?, ?, ?)', [$ip, mb_substr($email, 0, 100), Db::now()]);
            // Limpieza ocasional de intentos viejos.
            if (random_int(1, 20) === 1) {
                Db::run('DELETE FROM login_attempts WHERE attempted_at < ?', [date('Y-m-d H:i:s', time() - 86400)]);
            }
            return [null, 'Usuario o contraseña incorrectos.'];
        }

        if (password_needs_rehash($row['password_hash'], PASSWORD_DEFAULT)) {
            Db::run('UPDATE credentials SET password_hash = ? WHERE user_id = ?', [password_hash($password, PASSWORD_DEFAULT), $row['user_id']]);
        }
        Db::run('DELETE FROM login_attempts WHERE email = ?', [$email]);

        return [$row, 'Acceso correcto.'];
    }
}
