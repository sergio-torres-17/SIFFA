<?php
declare(strict_types=1);

namespace Siffa\Services;

use Siffa\Core\Db;

/** Indicadores, gráficas y tablas del inicio (equivale a DashboardService). */
final class Dashboard
{
    // El catálogo no tiene un código fijo: se reconoce por nombre (Entrada/Ingreso y Salida/Egreso).
    private static function isEntry(string $name): bool
    {
        return stripos($name, 'entr') !== false || stripos($name, 'ingres') !== false;
    }

    private static function isExit(string $name): bool
    {
        return stripos($name, 'sal') !== false || stripos($name, 'exit') !== false || stripos($name, 'egres') !== false;
    }

    public static function summary(int $latestCount = 10): array
    {
        $employees = Db::all(
            'SELECT e.employee_id, e.internal_identifier, e.first_name, e.last_name, a.description AS area
               FROM employees e JOIN areas a ON a.area_id = e.area_id'
        );
        $traces = Db::all(
            'SELECT t.employee_id, t.trace_date, x.name AS action
               FROM traces t JOIN access_actions x ON x.access_action_id = t.access_action_id'
        );
        $actions = Db::all('SELECT name FROM access_actions');

        $classified = [];
        foreach ($traces as $t) {
            $entry = self::isEntry($t['action']);
            $exit = self::isExit($t['action']);
            if ($entry || $exit) {
                $classified[] = $t + ['isEntry' => $entry, 'isExit' => $exit];
            }
        }

        $entered = [];
        foreach ($classified as $t) {
            if ($t['isEntry']) $entered[$t['employee_id']] = true;
        }

        // Último movimiento de cada empleado.
        $last = [];
        foreach ($classified as $t) {
            $id = $t['employee_id'];
            if (!isset($last[$id]) || $t['trace_date'] > $last[$id]['trace_date']) {
                $last[$id] = $t;
            }
        }
        $exited = [];
        foreach ($last as $id => $t) {
            if ($t['isExit']) $exited[$id] = true;
        }

        $total = count($employees);

        // Por área
        $byArea = [];
        foreach ($employees as $e) {
            $a = $e['area'];
            $byArea[$a] ??= ['area' => $a, 'registered' => 0, 'entered' => 0, 'exited' => 0];
            $byArea[$a]['registered']++;
            if (isset($entered[$e['employee_id']])) $byArea[$a]['entered']++;
            if (isset($exited[$e['employee_id']])) $byArea[$a]['exited']++;
        }
        $byArea = array_values($byArea);
        foreach ($byArea as &$row) {
            $row['pending'] = $row['registered'] - $row['entered'];
        }
        unset($row);
        usort($byArea, static fn($x, $y) => [$y['registered'], $x['area']] <=> [$x['registered'], $y['area']]);

        // Ingresos por hora
        $hours = [];
        foreach ($classified as $t) {
            if ($t['isEntry']) {
                $h = (int)substr($t['trace_date'], 11, 2);
                $hours[$h] = ($hours[$h] ?? 0) + 1;
            }
        }
        ksort($hours);
        $entriesByHour = [];
        foreach ($hours as $h => $count) {
            $entriesByHour[] = ['hour' => $h, 'entries' => $count];
        }

        // Últimos accesos
        usort($classified, static fn($x, $y) => strcmp($y['trace_date'], $x['trace_date']));
        $byId = [];
        foreach ($employees as $e) {
            $byId[$e['employee_id']] = $e;
        }
        $latest = [];
        foreach ($classified as $t) {
            if (count($latest) >= $latestCount) break;
            $e = $byId[$t['employee_id']] ?? null;
            if ($e === null) continue;
            $latest[] = [
                'date' => $t['trace_date'],
                'employeeNumber' => $e['internal_identifier'],
                'employeeName' => trim($e['first_name'] . ' ' . $e['last_name']),
                'area' => $e['area'],
                'action' => $t['action'],
                'isEntry' => $t['isEntry'],
            ];
        }

        $hasActions = false;
        foreach ($actions as $a) {
            if (self::isEntry($a['name']) || self::isExit($a['name'])) { $hasActions = true; break; }
        }

        return [
            'totalEmployees' => $total,
            'entered' => count($entered),
            'pending' => $total - count($entered),
            'exited' => count($exited),
            'inside' => count($entered) - count($exited),
            'hasAccessActions' => $hasActions,
            'byArea' => $byArea,
            'entriesByHour' => $entriesByHour,
            'latestAccesses' => $latest,
            'updatedAt' => date('H:i:s'),
        ];
    }
}
