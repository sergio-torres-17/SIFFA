<?php
declare(strict_types=1);

namespace Siffa\Services;

use Siffa\Core\Auth;
use Siffa\Core\Db;
use Siffa\Core\Env;

/** Usuarios de acceso al sistema (equivale a UserAccountService). */
final class Users
{
    public const MIN_PASSWORD = 8;

    public static function adminEmail(): string
    {
        return Auth::normalizeEmail(Env::get('ADMIN_EMAIL', 'adminsiffa@multicodedev.com') ?? '');
    }

    /** @return list<array<string,mixed>> */
    public static function all(): array
    {
        $admin = self::adminEmail();
        $rows = Db::all(
            'SELECT u.user_id, u.first_name, u.last_name, u.email, u.created_at,
                    (SELECT COUNT(*) FROM credentials c WHERE c.user_id = u.user_id) AS creds
               FROM users u ORDER BY u.first_name, u.last_name'
        );

        return array_map(static fn(array $r) => [
            'userId' => $r['user_id'],
            'firstName' => $r['first_name'],
            'lastName' => $r['last_name'],
            'fullName' => trim($r['first_name'] . ' ' . $r['last_name']),
            'email' => $r['email'],
            'hasCredential' => (int)$r['creds'] > 0,
            'isAdmin' => $r['email'] === $admin,
            'createdAt' => $r['created_at'],
        ], $rows);
    }

    /** @return array{isSuccess:bool,message:string} */
    public static function create(string $first, string $last, string $email, string $password): array
    {
        $first = trim($first);
        $last = trim($last);
        $email = Auth::normalizeEmail($email);

        if ($first === '') return self::fail('El nombre es obligatorio.');
        if ($last === '') return self::fail('Los apellidos son obligatorios.');
        if (mb_strlen($first) > 100 || mb_strlen($last) > 100) return self::fail('Nombre y apellidos admiten máximo 100 caracteres.');
        if (!filter_var($email, FILTER_VALIDATE_EMAIL)) return self::fail('El correo no es válido.');
        if (mb_strlen($email) > 100) return self::fail('El correo admite máximo 100 caracteres.');
        if ($error = self::passwordError($password)) return self::fail($error);

        if (Db::value('SELECT COUNT(*) FROM users WHERE email = ?', [$email]) > 0) {
            return self::fail('Ya existe un usuario con ese correo.');
        }

        Db::transaction(static function () use ($first, $last, $email, $password) {
            $userId = Db::uuid();
            Db::run('INSERT INTO users (user_id, first_name, last_name, email, created_at) VALUES (?, ?, ?, ?, ?)',
                [$userId, $first, $last, $email, Db::now()]);
            Db::run('INSERT INTO credentials (credential_id, user_id, password_hash, created_at) VALUES (?, ?, ?, ?)',
                [Db::uuid(), $userId, password_hash($password, PASSWORD_DEFAULT), Db::now()]);
        });

        return ['isSuccess' => true, 'message' => "Usuario {$email} creado."];
    }

    /** Asigna una contraseña nueva (también a un usuario que aún no tiene). */
    public static function resetPassword(string $userId, string $password): array
    {
        if ($error = self::passwordError($password)) return self::fail($error);

        $user = Db::one('SELECT user_id, email FROM users WHERE user_id = ?', [$userId]);
        if ($user === null) return self::fail('El usuario ya no existe.');

        $hash = password_hash($password, PASSWORD_DEFAULT);
        Db::transaction(static function () use ($userId, $hash) {
            Db::run('DELETE FROM credentials WHERE user_id = ?', [$userId]);
            Db::run('INSERT INTO credentials (credential_id, user_id, password_hash, created_at) VALUES (?, ?, ?, ?)',
                [Db::uuid(), $userId, $hash, Db::now()]);
        });

        return ['isSuccess' => true, 'message' => "Contraseña de {$user['email']} actualizada."];
    }

    public static function delete(string $userId, ?string $currentUserId): array
    {
        $user = Db::one('SELECT user_id, email FROM users WHERE user_id = ?', [$userId]);
        if ($user === null) return self::fail('El usuario ya no existe.');
        if ($user['email'] === self::adminEmail()) return self::fail('El usuario administrador no se puede eliminar.');
        if ($currentUserId !== null && $currentUserId === $userId) return self::fail('No puedes eliminar tu propio usuario.');

        Db::run('DELETE FROM users WHERE user_id = ?', [$userId]); // las credenciales se eliminan en cascada
        return ['isSuccess' => true, 'message' => "Usuario {$user['email']} eliminado."];
    }

    private static function passwordError(string $password): ?string
    {
        return mb_strlen($password) < self::MIN_PASSWORD
            ? 'La contraseña debe tener al menos ' . self::MIN_PASSWORD . ' caracteres.'
            : null;
    }

    private static function fail(string $message): array
    {
        return ['isSuccess' => false, 'message' => $message];
    }
}
