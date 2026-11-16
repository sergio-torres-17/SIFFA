<?php
declare(strict_types=1);

namespace Siffa\Services;

use Siffa\Core\Db;

/** Catálogo de adicionales: boletos de agua, comida, bebida, premios… (equivale a AddTypeService). */
final class AddTypes
{
    private const MAX_NAME = 50;
    private const MAX_DESCRIPTION = 200;

    /** @return list<array<string,mixed>> */
    public static function all(): array
    {
        $rows = Db::all(
            'SELECT a.add_type_id, a.name, a.description,
                    (SELECT COUNT(*) FROM add_employees e WHERE e.add_type_id = a.add_type_id) AS tickets,
                    (SELECT COUNT(*) FROM add_type_employees t WHERE t.add_type_id = a.add_type_id) AS configs
               FROM add_types a ORDER BY a.name'
        );

        return array_map(static fn(array $r) => [
            'addTypeId' => $r['add_type_id'],
            'name' => $r['name'],
            'description' => $r['description'],
            'ticketsAssigned' => (int)$r['tickets'],
            'configurations' => (int)$r['configs'],
        ], $rows);
    }

    public static function save(?string $id, string $name, ?string $description): array
    {
        $name = trim($name);
        $description = ($description === null || trim($description) === '') ? null : trim($description);

        if ($name === '') return self::fail('El nombre es obligatorio.');
        if (mb_strlen($name) > self::MAX_NAME) return self::fail('El nombre admite máximo ' . self::MAX_NAME . ' caracteres.');
        if ($description !== null && mb_strlen($description) > self::MAX_DESCRIPTION) {
            return self::fail('La descripción admite máximo ' . self::MAX_DESCRIPTION . ' caracteres.');
        }

        $duplicate = Db::value('SELECT COUNT(*) FROM add_types WHERE name = ? AND add_type_id <> ?', [$name, $id ?? '']);
        if ((int)$duplicate > 0) return self::fail("Ya existe un adicional llamado \"{$name}\".");

        if ($id === null || $id === '') {
            Db::run('INSERT INTO add_types (add_type_id, name, description, insertion_date) VALUES (?, ?, ?, ?)',
                [Db::uuid(), $name, $description, Db::now()]);
            return ['isSuccess' => true, 'message' => "Adicional \"{$name}\" creado."];
        }

        if (Db::value('SELECT COUNT(*) FROM add_types WHERE add_type_id = ?', [$id]) == 0) {
            return self::fail('El adicional ya no existe.');
        }
        Db::run('UPDATE add_types SET name = ?, description = ? WHERE add_type_id = ?', [$name, $description, $id]);
        return ['isSuccess' => true, 'message' => "Adicional \"{$name}\" actualizado."];
    }

    /** Solo se elimina si nadie lo usa (ni boletos ni configuraciones por rubro). */
    public static function delete(string $id): array
    {
        $row = Db::one('SELECT name FROM add_types WHERE add_type_id = ?', [$id]);
        if ($row === null) return self::fail('El adicional ya no existe.');

        $used = (int)Db::value('SELECT COUNT(*) FROM add_employees WHERE add_type_id = ?', [$id])
              + (int)Db::value('SELECT COUNT(*) FROM add_type_employees WHERE add_type_id = ?', [$id]);
        if ($used > 0) return self::fail("\"{$row['name']}\" está en uso y no se puede eliminar.");

        Db::run('DELETE FROM add_types WHERE add_type_id = ?', [$id]);
        return ['isSuccess' => true, 'message' => "Adicional \"{$row['name']}\" eliminado."];
    }

    private static function fail(string $message): array
    {
        return ['isSuccess' => false, 'message' => $message];
    }
}
