<?php
declare(strict_types=1);

namespace Siffa\Services;

use Shuchkin\SimpleXLSX;
use Siffa\Core\Db;

/** Carga masiva de empleados desde Excel (equivale a EmployeeExcelReader + RegisterEmployeeImportAsync). */
final class Employees
{
    /**
     * Lee el .xlsx. Columnas, en este orden (renglón 1 = encabezados):
     * Tipo de empleado, Número, Nombre, Apellidos, Área, Correo.
     *
     * @return array{rows:list<array<string,string|null>>,warnings:list<string>}
     */
    public static function parseExcel(string $path): array
    {
        $xlsx = SimpleXLSX::parse($path);
        if (!$xlsx) {
            throw new \RuntimeException('No se pudo leer el Excel: ' . SimpleXLSX::parseError());
        }

        $rows = [];
        $warnings = [];

        foreach ($xlsx->rows(0) as $index => $cells) {
            if ($index === 0) {
                continue; // encabezados
            }
            $rowNumber = $index + 1;
            $cell = static fn(int $i): string => isset($cells[$i]) ? trim((string)$cells[$i]) : '';

            $type = $cell(0);
            $number = $cell(1);
            $first = $cell(2);
            $last = $cell(3);
            $area = $cell(4);
            $email = $cell(5);

            // Renglón completamente vacío: se ignora sin avisar.
            if ($type . $number . $first . $last . $area . $email === '') {
                continue;
            }

            $missing = [];
            if ($type === '') $missing[] = 'tipo de empleado';
            if ($number === '') $missing[] = 'número';
            if ($first === '') $missing[] = 'nombre';
            if ($area === '') $missing[] = 'área';
            if ($email === '') $missing[] = 'correo';

            if ($missing) {
                $warnings[] = "Renglón {$rowNumber} omitido: falta " . implode(', ', $missing) . '.';
                continue;
            }

            $rows[] = [
                'employeeTypeName' => $type,
                'internalIdentifier' => $number,
                'firstName' => $first,
                'lastName' => $last === '' ? null : $last,
                'areaName' => $area,
                'employeeEmail' => $email,
            ];
        }

        return ['rows' => $rows, 'warnings' => $warnings];
    }

    /**
     * Registra la carga: áreas y tipos (sin repetir), configuración de adicionales por rubro, empleados
     * y un boleto (fila de add_employees) por cada adicional. Todo en una sola transacción.
     *
     * @param list<array<string,mixed>> $employees
     * @param 'area'|'type'            $mode
     * @param list<array{key:string,addTypeId:string,quantity:int}> $allocations
     */
    public static function register(array $employees, string $mode, array $allocations): array
    {
        if (!$employees) {
            return ['isSuccess' => false, 'message' => 'No hay empleados para registrar.'];
        }
        foreach ($allocations as $a) {
            if ((int)$a['quantity'] < 0) {
                return ['isSuccess' => false, 'message' => 'La cantidad de adicionales no puede ser negativa.'];
            }
        }
        $mode = $mode === 'type' ? 'type' : 'area';
        $lower = static fn(string $s): string => mb_strtolower(trim($s));

        // Los adicionales deben existir en el catálogo.
        $positive = array_values(array_filter($allocations, static fn($a) => (int)$a['quantity'] > 0));
        $addTypeIds = array_values(array_unique(array_map(static fn($a) => (string)$a['addTypeId'], $positive)));
        if ($addTypeIds) {
            $marks = implode(',', array_fill(0, count($addTypeIds), '?'));
            $known = (int)Db::value("SELECT COUNT(*) FROM add_types WHERE add_type_id IN ({$marks})", $addTypeIds);
            if ($known !== count($addTypeIds)) {
                return ['isSuccess' => false, 'message' => 'Alguno de los tipos de adicional ya no existe en el catálogo.'];
            }
        }

        try {
            return Db::transaction(static function () use ($employees, $mode, $positive, $lower) {
                $now = Db::now();

                // 1) Áreas y tipos de empleado: se reutilizan los existentes y solo se crean los que faltan.
                $areas = [];
                foreach (Db::all('SELECT area_id, description FROM areas') as $r) {
                    $areas[$lower($r['description'])] = $r['area_id'];
                }
                $types = [];
                foreach (Db::all('SELECT employee_type_id, name FROM employee_types') as $r) {
                    $types[$lower($r['name'])] = $r['employee_type_id'];
                }
                $newAreas = 0;
                $newTypes = 0;

                foreach ($employees as $e) {
                    $areaKey = $lower($e['areaName']);
                    if (!isset($areas[$areaKey])) {
                        $areas[$areaKey] = Db::uuid();
                        Db::run('INSERT INTO areas (area_id, description, insertion_date) VALUES (?, ?, ?)', [$areas[$areaKey], trim($e['areaName']), $now]);
                        $newAreas++;
                    }
                    $typeKey = $lower($e['employeeTypeName']);
                    if (!isset($types[$typeKey])) {
                        $types[$typeKey] = Db::uuid();
                        Db::run('INSERT INTO employee_types (employee_type_id, name, insertion_date) VALUES (?, ?, ?)', [$types[$typeKey], trim($e['employeeTypeName']), $now]);
                        $newTypes++;
                    }
                }

                // Configuración de adicionales por rubro: se actualiza la cantidad si ya existe y se inserta si no.
                $configsSaved = 0;
                foreach ($positive as $a) {
                    $key = $lower((string)$a['key']);
                    $rubroId = $mode === 'area' ? ($areas[$key] ?? null) : ($types[$key] ?? null);
                    if ($rubroId === null) {
                        continue;
                    }
                    $column = $mode === 'area' ? 'area_id' : 'employee_type_id';
                    $existing = Db::one("SELECT add_type_employee_id FROM add_type_employees WHERE add_type_id = ? AND {$column} = ?", [$a['addTypeId'], $rubroId]);
                    if ($existing) {
                        Db::run('UPDATE add_type_employees SET quantity = ? WHERE add_type_employee_id = ?', [(int)$a['quantity'], $existing['add_type_employee_id']]);
                    } else {
                        Db::run(
                            'INSERT INTO add_type_employees (add_type_employee_id, add_type_id, employee_type_id, area_id, quantity, insertion_date) VALUES (?, ?, ?, ?, ?, ?)',
                            [Db::uuid(), $a['addTypeId'], $mode === 'type' ? $rubroId : null, $mode === 'area' ? $rubroId : null, (int)$a['quantity'], $now]
                        );
                    }
                    $configsSaved++;
                }

                // 2) Empleados: sin repetir dentro del archivo ni contra los existentes (número y correo son únicos).
                $existingNumbers = [];
                $existingEmails = [];
                foreach (Db::all('SELECT internal_identifier, email FROM employees') as $r) {
                    $existingNumbers[$lower($r['internal_identifier'])] = true;
                    $existingEmails[$lower($r['email'])] = true;
                }

                $byKey = [];
                foreach ($positive as $a) {
                    $byKey[$lower((string)$a['key'])][] = $a;
                }

                $inserted = 0;
                $skipped = 0;
                $addsInserted = 0;

                foreach ($employees as $e) {
                    $number = trim($e['internalIdentifier']);
                    $email = trim($e['employeeEmail']);
                    $nk = $lower($number);
                    $ek = $lower($email);

                    if (isset($existingNumbers[$nk]) || isset($existingEmails[$ek])) {
                        $skipped++;
                        continue;
                    }
                    $existingNumbers[$nk] = true;
                    $existingEmails[$ek] = true;

                    $employeeId = Db::uuid();
                    Db::run(
                        'INSERT INTO employees (employee_id, employee_type_id, internal_identifier, area_id, first_name, last_name, email, insertion_date)
                         VALUES (?, ?, ?, ?, ?, ?, ?, ?)',
                        [$employeeId, $types[$lower($e['employeeTypeName'])], $number, $areas[$lower($e['areaName'])],
                         trim($e['firstName']), $e['lastName'] === null ? null : trim($e['lastName']), $email, $now]
                    );
                    $inserted++;

                    // 3) Boletos del empleado: una fila por cada uno.
                    $rubro = $mode === 'area' ? $e['areaName'] : $e['employeeTypeName'];
                    foreach ($byKey[$lower($rubro)] ?? [] as $a) {
                        for ($i = 0; $i < (int)$a['quantity']; $i++) {
                            Db::run(
                                'INSERT INTO add_employees (add_employee_id, add_type_id, employee_id, is_delivered, insertion_date) VALUES (?, ?, ?, 0, ?)',
                                [Db::uuid(), $a['addTypeId'], $employeeId, $now]
                            );
                            $addsInserted++;
                        }
                    }
                }

                $message = "Se registraron {$inserted} empleados, {$addsInserted} adicionales, {$configsSaved} cantidades configuradas, {$newAreas} áreas nuevas y {$newTypes} tipos de empleado nuevos.";
                if ($skipped > 0) {
                    $message .= " Se omitieron {$skipped} empleados repetidos (número o correo ya existente).";
                }
                return ['isSuccess' => true, 'message' => $message];
            });
        } catch (\Throwable $e) {
            return ['isSuccess' => false, 'message' => 'Error al registrar la carga: ' . $e->getMessage()];
        }
    }
}
