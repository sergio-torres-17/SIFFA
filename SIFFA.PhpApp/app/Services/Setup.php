<?php
declare(strict_types=1);

namespace Siffa\Services;

use Siffa\Core\Db;
use Siffa\Core\Env;

/** Instalación: tablas, datos base y activación del administrador (equivale a ManagementPlattform). */
final class Setup
{
    /**
     * @return array{connected:bool,error:?string,hasTables:bool,adminExists:bool,activated:bool}
     */
    public static function status(): array
    {
        $status = self::readStatus();

        // Si hay conexión pero faltan las tablas o el usuario administrador (ADMIN_EMAIL / ADMIN_NAME del .env),
        // se crean solos. La contraseña la define la persona en /setup (con la clave de activación).
        static $tried = false;
        if (!$tried && $status['connected'] && (!$status['hasTables'] || !$status['adminExists'])) {
            $tried = true;
            $result = self::install();
            if (!$result['isSuccess']) {
                @error_log('[SIFFA] Autoinstalación: ' . $result['message']);
            }
            $status = self::readStatus();
        }

        return $status;
    }

    /** @return array{connected:bool,error:?string,hasTables:bool,adminExists:bool,activated:bool} */
    private static function readStatus(): array
    {
        $status = ['connected' => false, 'error' => null, 'hasTables' => false, 'adminExists' => false, 'activated' => false];

        try {
            Db::pdo();
            $status['connected'] = true;
        } catch (\Throwable $e) {
            $status['error'] = $e->getMessage();
            return $status;
        }

        try {
            $status['hasTables'] = Db::tableExists('users') && Db::tableExists('credentials') && Db::tableExists('traces');
            if ($status['hasTables']) {
                $admin = Db::one('SELECT user_id FROM users WHERE email = ?', [Users::adminEmail()]);
                $status['adminExists'] = $admin !== null;
                $status['activated'] = $admin !== null
                    && (int)Db::value('SELECT COUNT(*) FROM credentials WHERE user_id = ?', [$admin['user_id']]) > 0;
            }
        } catch (\Throwable $e) {
            $status['error'] = $e->getMessage();
        }

        return $status;
    }

    public static function keyIsValid(?string $provided): bool
    {
        $expected = Env::get('ADMIN_ACTIVATION_KEY');
        return $expected !== null && $expected !== '' && is_string($provided)
            && hash_equals(hash('sha256', $expected), hash('sha256', $provided));
    }

    public static function keyConfigured(): bool
    {
        return (Env::get('ADMIN_ACTIVATION_KEY') ?? '') !== '';
    }

    /** Crea las tablas que falten y los datos base. */
    public static function install(): array
    {
        try {
            $sql = file_get_contents(BASE_PATH . '/database/schema.sql') ?: '';
            // Se quitan los comentarios de línea y se separa por ";".
            $sql = preg_replace('/^\s*--.*$/m', '', $sql) ?? $sql;
            foreach (array_filter(array_map('trim', explode(';', $sql))) as $statement) {
                Db::pdo()->exec($statement);
            }

            self::seed();
            return ['isSuccess' => true, 'message' => 'Tablas y datos base listos.'];
        } catch (\Throwable $e) {
            return ['isSuccess' => false, 'message' => 'No se pudo crear la base de datos: ' . $e->getMessage()];
        }
    }

    /** Adicionales y acciones de acceso base, y el usuario administrador (sin contraseña todavía). */
    private static function seed(): void
    {
        $now = Db::now();

        foreach (['Boletos para agua', 'Boletos para comida', 'Boletos para bebida alcoholica'] as $name) {
            if ((int)Db::value('SELECT COUNT(*) FROM add_types WHERE name = ?', [$name]) === 0) {
                Db::run('INSERT INTO add_types (add_type_id, name, description, insertion_date) VALUES (?, ?, ?, ?)',
                    [Db::uuid(), $name, $name, $now]);
            }
        }

        foreach (['Ingreso', 'Salida'] as $name) {
            if ((int)Db::value('SELECT COUNT(*) FROM access_actions WHERE name = ?', [$name]) === 0) {
                Db::run('INSERT INTO access_actions (access_action_id, name, description, insertion_date) VALUES (?, ?, ?, ?)',
                    [Db::uuid(), $name, $name, $now]);
            }
        }

        if ((int)Db::value('SELECT COUNT(*) FROM users WHERE email = ?', [Users::adminEmail()]) === 0) {
            $parts = preg_split('/\s+/', trim((string)(Env::get('ADMIN_NAME') ?: 'Main Admin')), 2) ?: ['Main', 'Admin'];
            $first = $parts[0];
            $last = $parts[1] ?? '';
            Db::run('INSERT INTO users (user_id, first_name, last_name, email, created_at) VALUES (?, ?, ?, ?, ?)',
                [Db::uuid(), $first, $last, Users::adminEmail(), $now]);
        }
    }

    /** Da contraseña al administrador. Solo funciona mientras no tenga credenciales. */
    public static function activate(?string $key, string $password): array
    {
        if (!self::keyConfigured()) {
            return ['isSuccess' => false, 'message' => 'No hay una clave de activación configurada (ADMIN_ACTIVATION_KEY en .env).'];
        }
        if (!self::keyIsValid($key)) {
            return ['isSuccess' => false, 'message' => 'Clave de activación inválida.'];
        }
        if (mb_strlen($password) < Users::MIN_PASSWORD) {
            return ['isSuccess' => false, 'message' => 'La contraseña debe tener al menos ' . Users::MIN_PASSWORD . ' caracteres.'];
        }

        $admin = Db::one('SELECT user_id FROM users WHERE email = ?', [Users::adminEmail()]);
        if ($admin === null) {
            return ['isSuccess' => false, 'message' => 'No existe el usuario administrador. Crea primero las tablas.'];
        }
        if ((int)Db::value('SELECT COUNT(*) FROM credentials WHERE user_id = ?', [$admin['user_id']]) > 0) {
            return ['isSuccess' => false, 'message' => 'El sistema ya está activado.'];
        }

        Db::run('INSERT INTO credentials (credential_id, user_id, password_hash, created_at) VALUES (?, ?, ?, ?)',
            [Db::uuid(), $admin['user_id'], password_hash($password, PASSWORD_DEFAULT), Db::now()]);

        return ['isSuccess' => true, 'message' => 'Sistema activado correctamente.'];
    }
}
