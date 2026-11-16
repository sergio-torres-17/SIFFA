<?php
declare(strict_types=1);

use Siffa\Core\Auth;
use Siffa\Core\Db;
use Siffa\Core\Http;
use Siffa\Core\Router;
use Siffa\Core\View;
use Siffa\Services\Access;
use Siffa\Services\AddTypes;
use Siffa\Services\Cards;
use Siffa\Services\Dashboard;
use Siffa\Services\Employees;
use Siffa\Services\Setup;
use Siffa\Services\Users;

return static function (Router $r): void {

    // ───────────────────────── Sesión ─────────────────────────

    $r->get('/login', static function () {
        if (Auth::check()) {
            Http::redirect('/');
        }
        $status = Setup::status();
        View::render('login', [
            'title' => 'Iniciar sesión',
            'needsSetup' => !$status['connected'] || !$status['hasTables'] || !$status['activated'],
        ], 'auth');
    }, true);

    $r->post('/login', static function () {
        $email = (string)($_POST['email'] ?? '');
        $password = (string)($_POST['password'] ?? '');

        if ($email === '' || $password === '') {
            Http::flash('error', 'Escribe tu correo y contraseña.');
            Http::redirect('/login');
        }

        try {
            [$user, $message] = Auth::attempt($email, $password);
        } catch (\Throwable $e) {
            Http::flash('error', 'No se pudo validar el acceso. Revisa la configuración de la base de datos.');
            Http::redirect('/login');
        }

        if ($user === null) {
            Http::flash('error', $message);
            Http::redirect('/login');
        }

        Auth::login($user);
        Http::redirect('/');
    }, true);

    $r->post('/logout', static function () {
        Auth::logout();
        Http::redirect('/login');
    });

    // ───────────────────────── Instalación ─────────────────────────

    $r->get('/setup', static function () {
        View::render('setup', [
            'title' => 'Instalación',
            'status' => Setup::status(),
            'keyConfigured' => Setup::keyConfigured(),
            'adminEmail' => Users::adminEmail(),
        ], 'auth');
    }, true);

    $r->post('/setup/install', static function () {
        $result = !Setup::keyIsValid($_POST['key'] ?? null)
            ? ['isSuccess' => false, 'message' => 'Clave de activación inválida.']
            : Setup::install();
        Http::flash($result['isSuccess'] ? 'success' : 'error', $result['message']);
        Http::redirect('/setup');
    }, true);

    $r->post('/setup/activate', static function () {
        $password = (string)($_POST['password'] ?? '');
        if ($password !== (string)($_POST['confirm'] ?? '')) {
            Http::flash('error', 'Las contraseñas no coinciden.');
            Http::redirect('/setup');
        }
        $result = Setup::activate($_POST['key'] ?? null, $password);
        Http::flash($result['isSuccess'] ? 'success' : 'error', $result['message']);
        Http::redirect($result['isSuccess'] ? '/login' : '/setup');
    }, true);

    // ───────────────────────── Páginas ─────────────────────────

    $r->get('/', static fn() => View::render('home', ['title' => 'Inicio', 'nav' => 'home', 'scripts' => ['home.js']]));

    $r->get('/access', static fn() => View::render('access', ['title' => 'Control de accesos', 'nav' => 'access', 'scripts' => ['zxing.min.js', 'quagga.min.js', 'scanner.js', 'access.js']]));

    $r->get('/cards', static fn() => View::render('cards', ['title' => 'Ver invitaciones', 'nav' => 'cards', 'scripts' => ['cards.js']]));

    $r->get('/upload', static fn() => View::render('upload', ['title' => 'Subir empleados', 'nav' => 'upload', 'scripts' => ['upload.js']]));

    $r->get('/adds', static fn() => View::render('adds', [
        'title' => 'Elementos adicionales', 'nav' => 'adds', 'scripts' => ['adds.js'], 'items' => AddTypes::all(),
    ]));

    $r->get('/users', static fn() => View::render('users', [
        'title' => 'Usuarios de acceso', 'nav' => 'users', 'scripts' => ['users.js'], 'items' => Users::all(), 'currentUserId' => Auth::id(),
    ]));

    // ───────────────────────── AJAX (sesión + CSRF) ─────────────────────────

    $r->get('/ajax/dashboard', static fn() => Http::json(Dashboard::summary()));

    $r->post('/ajax/access/search', static function () {
        Http::json(Access::search((string)(Http::input()['term'] ?? '')));
    });

    $r->post('/ajax/access/register', static function () {
        $in = Http::input();
        $type = ($in['type'] ?? '') === 'exit' ? 'exit' : 'entry';
        Http::json(Access::addTrace((string)($in['employeeNumber'] ?? ''), $type));
    });

    $r->post('/ajax/access/deliveries', static function () {
        $in = Http::input();
        Http::json(Access::saveDeliveries((string)($in['employeeNumber'] ?? ''), (array)($in['addTypeIds'] ?? [])));
    });

    $r->post('/ajax/adds/save', static function () {
        $in = Http::input();
        $id = isset($in['addTypeId']) && $in['addTypeId'] !== '' ? (string)$in['addTypeId'] : null;
        Http::json(AddTypes::save($id, (string)($in['name'] ?? ''), isset($in['description']) ? (string)$in['description'] : null));
    });

    $r->post('/ajax/adds/delete', static function () {
        Http::json(AddTypes::delete((string)(Http::input()['addTypeId'] ?? '')));
    });

    $r->post('/ajax/users/create', static function () {
        $in = Http::input();
        Http::json(Users::create((string)($in['firstName'] ?? ''), (string)($in['lastName'] ?? ''), (string)($in['email'] ?? ''), (string)($in['password'] ?? '')));
    });

    $r->post('/ajax/users/reset', static function () {
        $in = Http::input();
        Http::json(Users::resetPassword((string)($in['userId'] ?? ''), (string)($in['password'] ?? '')));
    });

    $r->post('/ajax/users/delete', static function () {
        Http::json(Users::delete((string)(Http::input()['userId'] ?? ''), Auth::id()));
    });

    // Paso 1 de "Subir empleados": lee el Excel y deja las filas en la sesión.
    $r->post('/ajax/upload/parse', static function () {
        $file = $_FILES['file'] ?? null;
        if (!$file || $file['error'] !== UPLOAD_ERR_OK) {
            Http::json(['isSuccess' => false, 'message' => 'No se recibió el archivo (revisa el tamaño máximo de subida del hosting).'], 422);
        }
        if (!preg_match('/\.xlsx$/i', (string)$file['name'])) {
            Http::json(['isSuccess' => false, 'message' => 'El archivo debe ser un Excel .xlsx.'], 422);
        }
        if ($file['size'] > 10 * 1024 * 1024) {
            Http::json(['isSuccess' => false, 'message' => 'El archivo pesa más de 10 MB.'], 422);
        }

        try {
            $read = Employees::parseExcel($file['tmp_name']);
        } catch (\Throwable $e) {
            Http::json(['isSuccess' => false, 'message' => 'No se pudo leer el archivo: ' . $e->getMessage()], 422);
        }

        if (!$read['rows']) {
            Http::json(['isSuccess' => false, 'message' => 'No se encontró ningún empleado válido en el archivo.', 'warnings' => array_slice($read['warnings'], 0, 10)], 422);
        }

        $_SESSION['import'] = $read['rows'];

        $areas = [];
        $types = [];
        foreach ($read['rows'] as $row) {
            $a = mb_strtolower($row['areaName']);
            $t = mb_strtolower($row['employeeTypeName']);
            $areas[$a] ??= ['name' => $row['areaName'], 'count' => 0];
            $types[$t] ??= ['name' => $row['employeeTypeName'], 'count' => 0];
            $areas[$a]['count']++;
            $types[$t]['count']++;
        }
        $byName = static function (array $x, array $y) { return strcasecmp($x['name'], $y['name']); };
        $areas = array_values($areas);
        $types = array_values($types);
        usort($areas, $byName);
        usort($types, $byName);

        Http::json([
            'isSuccess' => true,
            'fileName' => $file['name'],
            'count' => count($read['rows']),
            'areas' => $areas,
            'types' => $types,
            'warnings' => array_slice($read['warnings'], 0, 10),
            'warningsTotal' => count($read['warnings']),
            'addTypes' => array_map(static fn($a) => ['addTypeId' => $a['addTypeId'], 'name' => $a['name']], AddTypes::all()),
        ]);
    });

    // Paso final: registra la carga con el rubro y las cantidades elegidas.
    $r->post('/ajax/upload/register', static function () {
        $rows = $_SESSION['import'] ?? null;
        if (!is_array($rows) || !$rows) {
            Http::json(['isSuccess' => false, 'message' => 'La carga caducó. Vuelve a subir el archivo.'], 422);
        }
        $in = Http::input();
        $mode = ($in['mode'] ?? '') === 'type' ? 'type' : 'area';

        $allocations = [];
        foreach ((array)($in['allocations'] ?? []) as $a) {
            if (!is_array($a)) continue;
            $allocations[] = ['key' => (string)($a['key'] ?? ''), 'addTypeId' => (string)($a['addTypeId'] ?? ''), 'quantity' => (int)($a['quantity'] ?? 0)];
        }

        $result = Employees::register($rows, $mode, $allocations);
        if ($result['isSuccess']) {
            unset($_SESSION['import']);
        }
        Http::json($result);
    });

    // Vista previa de la tarjeta con el logo subido (el logo no se guarda en el servidor).
    $r->post('/ajax/cards/preview', static function () {
        $logo = readLogoUpload();
        $code = ($_POST['code'] ?? '') === 'barcode' ? 'barcode' : 'qr';

        $sample = Cards::sample();
        if ($sample === null) {
            Http::json(['isSuccess' => false, 'noEmployees' => true, 'message' => 'No hay empleados registrados todavía. Sube empleados para poder previsualizar y generar las tarjetas.'], 422);
        }

        $image = Cards::decodeLogo($logo);
        $png = Cards::renderPng($sample, $image, $code);
        Http::json([
            'isSuccess' => true,
            'image' => 'data:image/png;base64,' . base64_encode($png),
            'sample' => ['fullName' => $sample['fullName'], 'employeeNumber' => $sample['employeeNumber']],
        ]);
    });

    // Generación de todas las tarjetas por lotes: start → step (se repite) → download (.zip).
    $r->post('/ajax/cards/start', static function () {
        $logo = readLogoUpload();
        $format = ($_POST['format'] ?? '') === 'pdf' ? 'pdf' : 'png';
        $code = ($_POST['code'] ?? '') === 'barcode' ? 'barcode' : 'qr';

        $result = Cards::startJob($logo, $format, $code);
        Http::json($result, $result['isSuccess'] ? 200 : 422);
    });

    $r->post('/ajax/cards/step', static function () {
        $result = Cards::stepJob((string)($_POST['job'] ?? ''));
        Http::json($result, $result['isSuccess'] ? 200 : 422);
    });

    $r->get('/cards/download', static function () {
        $id = (string)($_GET['job'] ?? '');
        $result = Cards::jobResult($id);
        if ($result === null) {
            http_response_code(404);
            exit('La descarga ya no está disponible. Vuelve a generar las tarjetas.');
        }

        header('Content-Type: application/zip');
        header('Content-Disposition: attachment; filename="' . $result['fileName'] . '"');
        header('Content-Length: ' . filesize($result['path']));
        header('Cache-Control: no-store');
        readfile($result['path']);
        Cards::deleteJob($id);
        exit;
    });

    // ───────────────────────── API para el cliente de escritorio ─────────────────────────
    // Misma forma que la versión .NET: /api/Auth/Login, /api/Auth/Ping y /api/ManagePlattform/ActivateSystem.

    $r->get('/api/Auth/Ping', static fn() => Http::json(['ok' => true, 'app' => 'SIFFA']), true);

    $r->post('/api/Auth/Login', static function () {
        $in = Http::input();
        $username = (string)($in['username'] ?? $in['Username'] ?? '');
        $password = (string)($in['password'] ?? $in['Password'] ?? '');

        if (trim($username) === '' || $password === '') {
            Http::json(['isSuccess' => false, 'message' => 'Usuario o contraseña incorrectos.'], 401);
        }

        try {
            [$user, $message] = Auth::attempt($username, $password);
        } catch (\Throwable $e) {
            Http::json(['isSuccess' => false, 'message' => 'No se pudo validar el acceso.'], 500);
        }

        if ($user === null) {
            Http::json(['isSuccess' => false, 'message' => $message], 401);
        }

        Http::json([
            'isSuccess' => true,
            'message' => 'Acceso correcto.',
            'userName' => trim($user['first_name'] . ' ' . $user['last_name']),
            'email' => $user['email'],
            'userId' => $user['user_id'],
        ]);
    }, true);

    $r->post('/api/ManagePlattform/ActivateSystem', static function () {
        $in = Http::input();
        $result = Setup::activate((string)($in['seedActivation'] ?? $in['SeedActivation'] ?? ''), (string)($in['passwordAdmin'] ?? $in['PasswordAdmin'] ?? ''));
        Http::json($result, $result['isSuccess'] ? 200 : 400);
    }, true);
};

/** Lee y valida el logo subido (máximo 2 MB, debe ser una imagen). Termina con 422 si no sirve. */
function readLogoUpload(): string
{
    $file = $_FILES['logo'] ?? null;
    if (!$file || $file['error'] !== UPLOAD_ERR_OK) {
        Http::json(['isSuccess' => false, 'message' => 'No se recibió el logo.'], 422);
    }
    if ($file['size'] > 2 * 1024 * 1024) {
        Http::json(['isSuccess' => false, 'message' => 'El logo pesa más de 2 MB.'], 422);
    }
    $bytes = (string)file_get_contents($file['tmp_name']);
    if (Cards::decodeLogo($bytes) === null) {
        Http::json(['isSuccess' => false, 'message' => 'El archivo no es una imagen válida (usa PNG, JPG o WebP).'], 422);
    }
    return $bytes;
}
