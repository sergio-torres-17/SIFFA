<?php
declare(strict_types=1);

namespace Siffa\Services;

use Siffa\Core\Db;

/** Registro de ingresos y egresos y entrega de boletos (equivale a AccessControlService). */
final class Access
{
    /**
     * Registra un ingreso ('entry') o un egreso ('exit').
     *
     * @return array{status:bool,message:string,employeeAdds:?array,employeeNumber:?string,employeeName:?string}
     */
    public static function addTrace(string $identifier, string $type): array
    {
        $movement = $type === 'entry' ? 'Ingreso' : 'Salida';
        $identifier = trim($identifier);

        if ($identifier === '') {
            return self::response(false, 'Captura el código o número de empleado');
        }

        $employee = Db::one('SELECT employee_id, internal_identifier, first_name, last_name FROM employees WHERE internal_identifier = ?', [$identifier]);
        if ($employee === null) {
            return self::response(false, 'Empleado no encontrado');
        }

        $fullName = trim($employee['first_name'] . ' ' . $employee['last_name']);

        $action = Db::one('SELECT access_action_id FROM access_actions WHERE name = ?', [$movement]);
        if ($action === null) {
            return self::response(false, "Falta la acción {$movement} en el catálogo de acciones de acceso");
        }

        $already = Db::value('SELECT COUNT(*) FROM traces WHERE employee_id = ? AND access_action_id = ?',
            [$employee['employee_id'], $action['access_action_id']]);
        if ((int)$already > 0) {
            return self::response(false, "{$movement} ya existente para el usuario {$fullName}", null,
                $employee['internal_identifier'], $fullName);
        }

        $now = Db::now();
        Db::run(
            'INSERT INTO traces (trace_id, access_action_id, employee_id, trace_date, description, insertion_date) VALUES (?, ?, ?, ?, ?, ?)',
            [Db::uuid(), $action['access_action_id'], $employee['employee_id'], $now, mb_substr("{$movement} de {$fullName}", 0, 100), $now]
        );

        // Cada fila de add_employees es un boleto: se agrupan por tipo para mostrar la cantidad.
        $adds = Db::all(
            'SELECT t.add_type_id, t.name, COUNT(*) AS quantity, SUM(e.is_delivered) AS delivered
               FROM add_employees e JOIN add_types t ON t.add_type_id = e.add_type_id
              WHERE e.employee_id = ?
              GROUP BY t.add_type_id, t.name ORDER BY t.name',
            [$employee['employee_id']]
        );
        $adds = array_map(static fn(array $a) => [
            'addTypeId' => $a['add_type_id'],
            'name' => $a['name'],
            'quantity' => (int)$a['quantity'],
            'deliveryAdd' => (int)$a['delivered'] === (int)$a['quantity'],
        ], $adds);

        return self::response(true, "{$movement} de {$fullName} agregado correctamente", $adds,
            $employee['internal_identifier'], $fullName);
    }

    /**
     * Busca por número, nombre o correo (máximo 10 coincidencias).
     *
     * @return list<array{employeeNumber:string,fullName:string,email:string,area:string}>
     */
    public static function search(string $term): array
    {
        $term = trim($term);
        if ($term === '') {
            return [];
        }

        $select = 'SELECT e.internal_identifier, e.first_name, e.last_name, e.email, a.description AS area
                     FROM employees e JOIN areas a ON a.area_id = e.area_id';

        // Coincidencia exacta de número o correo: es lo que devuelve un lector de códigos.
        $rows = Db::all("{$select} WHERE e.internal_identifier = ? OR e.email = ?", [$term, $term]);

        if (!$rows) {
            $like = Db::like($term);
            $rows = Db::all(
                "{$select} WHERE CONCAT(e.first_name, ' ', IFNULL(e.last_name, '')) LIKE ? OR e.email LIKE ? OR e.internal_identifier LIKE ?
                  ORDER BY e.first_name, e.last_name LIMIT 10",
                [$like, $like, $like]
            );
        }

        return array_map(static fn(array $r) => [
            'employeeNumber' => $r['internal_identifier'],
            'fullName' => trim($r['first_name'] . ' ' . $r['last_name']),
            'email' => $r['email'],
            'area' => $r['area'],
        ], $rows);
    }

    /** @param list<string> $addTypeIds */
    public static function saveDeliveries(string $employeeNumber, array $addTypeIds): array
    {
        $employee = Db::one('SELECT employee_id FROM employees WHERE internal_identifier = ?', [trim($employeeNumber)]);
        if ($employee === null) {
            return ['isSuccess' => false, 'message' => 'Empleado no encontrado'];
        }
        $addTypeIds = array_values(array_filter($addTypeIds, 'is_string'));
        if (!$addTypeIds) {
            return ['isSuccess' => false, 'message' => 'No se marcó ningún boleto como entregado'];
        }

        $marks = implode(',', array_fill(0, count($addTypeIds), '?'));
        $statement = Db::run(
            "UPDATE add_employees SET is_delivered = 1, delivery_date = ?
              WHERE employee_id = ? AND is_delivered = 0 AND add_type_id IN ({$marks})",
            array_merge([Db::now(), $employee['employee_id']], $addTypeIds)
        );

        return ['isSuccess' => true, 'message' => 'Se guardaron ' . $statement->rowCount() . ' boletos entregados'];
    }

    private static function response(bool $status, string $message, ?array $adds = null, ?string $number = null, ?string $name = null): array
    {
        return ['status' => $status, 'message' => $message, 'employeeAdds' => $adds, 'employeeNumber' => $number, 'employeeName' => $name];
    }
}
