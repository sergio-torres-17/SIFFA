<?php
declare(strict_types=1);

namespace Siffa\Services;

use BaconQrCode\Common\ErrorCorrectionLevel;
use BaconQrCode\Encoder\Encoder;
use Picqer\Barcode\Types\TypeCode128;
use Siffa\Core\Db;
use Siffa\Core\Env;
use Siffa\Core\Log;

/**
 * Tarjetas de invitación (1050 x 600) dibujadas con GD, iguales para PNG y PDF.
 * El código (QR o Code 128) contiene el número de empleado, para leerlo en el control de accesos.
 */
final class Cards
{
    public const WIDTH = 1050;
    public const HEIGHT = 600;
    private const SCALE = 2; // se dibuja al doble y se reduce: bordes suaves

    public static function fontRegular(): string
    {
        return BASE_PATH . '/resources/fonts/DejaVuSans.ttf';
    }

    public static function fontBold(): string
    {
        return BASE_PATH . '/resources/fonts/DejaVuSans-Bold.ttf';
    }

    // ---------- datos ----------

    /** @return list<array{employeeNumber:string,fullName:string,area:string,employeeType:string,adds:list<array{name:string,quantity:int}>}> */
    public static function loadAll(): array
    {
        $employees = Db::all(
            'SELECT e.employee_id, e.internal_identifier, e.first_name, e.last_name, a.description AS area, t.name AS type
               FROM employees e
               JOIN areas a ON a.area_id = e.area_id
               JOIN employee_types t ON t.employee_type_id = e.employee_type_id
              ORDER BY e.internal_identifier'
        );

        $adds = [];
        foreach (Db::all(
            'SELECT e.employee_id, t.name, COUNT(*) AS quantity
               FROM add_employees e JOIN add_types t ON t.add_type_id = e.add_type_id
              GROUP BY e.employee_id, t.name ORDER BY t.name'
        ) as $r) {
            $adds[$r['employee_id']][] = ['name' => $r['name'], 'quantity' => (int)$r['quantity']];
        }

        return array_map(static fn(array $e) => [
            'employeeNumber' => $e['internal_identifier'],
            'fullName' => trim($e['first_name'] . ' ' . $e['last_name']),
            'area' => $e['area'],
            'employeeType' => $e['type'],
            'adds' => $adds[$e['employee_id']] ?? [],
        ], $employees);
    }

    /** Un empleado de muestra (de preferencia con adicionales) para la vista previa. */
    public static function sample(): ?array
    {
        $cards = self::loadAll();
        foreach ($cards as $card) {
            if ($card['adds']) {
                return $card;
            }
        }
        return $cards[0] ?? null;
    }

    // ---------- logo ----------

    /** Carga el logo (PNG/JPG/WebP/GIF) como imagen GD; null si no es una imagen válida. */
    public static function decodeLogo(string $bytes): ?\GdImage
    {
        if ($bytes === '' || @getimagesizefromstring($bytes) === false) {
            return null;
        }
        $image = @imagecreatefromstring($bytes);
        if ($image === false) {
            return null;
        }
        imagealphablending($image, true);
        imagesavealpha($image, true);
        return $image;
    }

    // ---------- dibujo ----------

    /** Tarjeta completa al doble de resolución (2100 x 1200). */
    public static function renderRaster(array $card, ?\GdImage $logo, string $code = 'qr'): \GdImage
    {
        $s = self::SCALE;
        $im = imagecreatetruecolor(self::WIDTH * $s, self::HEIGHT * $s);
        imagealphablending($im, true);
        imageantialias($im, true);

        $night = self::color($im, '#0A1230');
        $navy = self::color($im, '#16234F');
        $cyan = self::color($im, '#22D3EE');
        $lavender = self::color($im, '#B6C0E0');
        $white = self::color($im, '#FFFFFF');

        imagefilledrectangle($im, 0, 0, self::WIDTH * $s, self::HEIGHT * $s, $night);
        self::roundRect($im, 24 * $s, 24 * $s, (self::WIDTH - 24) * $s, (self::HEIGHT - 24) * $s, 28 * $s, $navy);
        self::roundRect($im, 24 * $s, 24 * $s, 38 * $s, (self::HEIGHT - 24) * $s, 7 * $s, $cyan);

        // Logo (arriba a la izquierda, ajustado a una caja de 260 x 110)
        if ($logo !== null) {
            $lw = imagesx($logo);
            $lh = imagesy($logo);
            if ($lw > 0 && $lh > 0) {
                $scale = min(260 / $lw, 110 / $lh);
                $w = $lw * $scale;
                $h = $lh * $scale;
                $y = 56 + (110 - $h) / 2;
                imagecopyresampled($im, $logo, (int)(70 * $s), (int)($y * $s), 0, 0, (int)($w * $s), (int)($h * $s), $lw, $lh);
            }
        }

        $year = Env::get('EVENT_YEAR') ?: date('Y');
        self::text($im, 'INVITACIÓN', 70, 215, true, 30, $cyan, 500);
        self::text($im, "Fiesta de fin de año {$year}", 70, 252, false, 28, $lavender, 500);

        self::text($im, $card['fullName'], 70, 335, true, 54, $white, 520);
        self::text($im, $card['area'] . '  ·  ' . $card['employeeType'], 70, 385, false, 30, $lavender, 520);
        self::text($im, 'No. de empleado: ' . $card['employeeNumber'], 70, 428, false, 28, $white, 600);

        if ($card['adds']) {
            $line = implode('   ', array_map(static fn($a) => $a['name'] . ' ×' . $a['quantity'], $card['adds']));
            self::text($im, $line, 70, 520, true, 26, $cyan, 600);
        }

        if ($code === 'barcode') {
            self::drawBarcode($im, $card['employeeNumber'], $night, $white, $lavender);
        } else {
            self::drawQr($im, $card['employeeNumber'], $night, $white, $lavender);
        }

        return $im;
    }

    /** PNG de 1050 x 600. */
    public static function renderPng(array $card, ?\GdImage $logo, string $code = 'qr'): string
    {
        $big = self::renderRaster($card, $logo, $code);
        $small = imagecreatetruecolor(self::WIDTH, self::HEIGHT);
        imagecopyresampled($small, $big, 0, 0, 0, 0, self::WIDTH, self::HEIGHT, imagesx($big), imagesy($big));

        ob_start();
        imagepng($small, null, 6);
        $png = (string)ob_get_clean();
        return $png;
    }

    /** PDF de una página (525 x 300 pt) con la tarjeta incrustada como imagen. */
    public static function renderPdf(array $card, ?\GdImage $logo, string $code = 'qr'): string
    {
        $big = self::renderRaster($card, $logo, $code);
        $tmp = tempnam(self::tmpDir(), 'card') ?: throw new \RuntimeException('No se pudo crear un archivo temporal.');
        $png = $tmp . '.png';
        rename($tmp, $png);
        imagepng($big, $png, 6);

        try {
            $pdf = new \FPDF('L', 'pt', [self::WIDTH / 2, self::HEIGHT / 2]);
            $pdf->SetMargins(0, 0, 0);
            $pdf->SetAutoPageBreak(false);
            $pdf->AddPage();
            $pdf->Image($png, 0, 0, self::WIDTH / 2, self::HEIGHT / 2, 'PNG');
            return $pdf->Output('S');
        } finally {
            @unlink($png);
        }
    }

    // ---------- generación por lotes (zip) ----------
    //
    // El hosting compartido corta las peticiones largas, así que las tarjetas se generan en varias
    // peticiones cortas: start (valida y crea el trabajo) → step (procesa unos segundos, se repite) → download.

    private const STEP_SECONDS = 12;

    private static function jobDir(string $id): ?string
    {
        if (!preg_match('/^[a-f0-9]{16}$/', $id)) {
            return null;
        }
        $dir = self::tmpDir() . '/job-' . $id;
        return is_dir($dir) ? $dir : null;
    }

    /** @return array{isSuccess:bool,message:string,job?:string,total?:int} */
    public static function startJob(string $logoBytes, string $format, string $code): array
    {
        Log::info('Generación de tarjetas: inicio', [
            'formato' => $format, 'codigo' => $code, 'logo_bytes' => strlen($logoBytes),
            'php' => PHP_VERSION, 'memory_limit' => ini_get('memory_limit'), 'max_execution_time' => ini_get('max_execution_time'),
            'gd' => function_exists('imagettftext') ? 'freetype' : 'SIN freetype', 'zip' => class_exists(\ZipArchive::class) ? 'ok' : 'SIN ZipArchive',
        ]);

        if (!class_exists(\ZipArchive::class)) {
            Log::error('Falta la extensión zip');
            return ['isSuccess' => false, 'message' => 'La extensión PHP "zip" no está activa en el hosting.'];
        }
        if (self::decodeLogo($logoBytes) === null) {
            return ['isSuccess' => false, 'message' => 'El logo no es una imagen válida.'];
        }

        $total = (int)Db::value('SELECT COUNT(*) FROM employees');
        if ($total === 0) {
            return ['isSuccess' => false, 'message' => 'No hay empleados registrados para generar tarjetas.'];
        }

        // Limpia trabajos abandonados (más de 3 horas).
        foreach (glob(self::tmpDir() . '/job-*', GLOB_ONLYDIR) ?: [] as $old) {
            if (filemtime($old) < time() - 10800) {
                self::removeDir($old);
            }
        }

        $id = bin2hex(random_bytes(8));
        $dir = self::tmpDir() . '/job-' . $id;
        if (!@mkdir($dir, 0775, true) && !is_dir($dir)) {
            Log::error('No se pudo crear la carpeta de trabajo', ['dir' => $dir]);
            return ['isSuccess' => false, 'message' => 'No se pudo escribir en storage/tmp (revisa los permisos de la carpeta storage).'];
        }
        file_put_contents($dir . '/logo.bin', $logoBytes);
        file_put_contents($dir . '/meta.json', json_encode(['format' => $format, 'code' => $code, 'total' => $total, 'done' => 0, 'names' => new \stdClass()]));

        return ['isSuccess' => true, 'message' => 'Listo.', 'job' => $id, 'total' => $total];
    }

    /** Procesa tarjetas durante unos segundos. @return array{isSuccess:bool,message:string,done?:int,total?:int,finished?:bool} */
    public static function stepJob(string $id): array
    {
        @set_time_limit(60);
        @ini_set('memory_limit', '512M');

        $dir = self::jobDir($id);
        if ($dir === null) {
            return ['isSuccess' => false, 'message' => 'El trabajo de generación ya no existe. Vuelve a empezar.'];
        }

        $meta = json_decode((string)file_get_contents($dir . '/meta.json'), true);
        $format = $meta['format'];
        $code = $meta['code'];
        $used = (array)$meta['names'];
        $done = (int)$meta['done'];
        $from = $done;
        $total = (int)$meta['total'];
        $extension = $format === 'pdf' ? 'pdf' : 'png';
        $started = microtime(true);

        try {
            $logo = self::decodeLogo((string)file_get_contents($dir . '/logo.bin'));
            if ($logo === null) {
                throw new \RuntimeException('No se pudo leer el logo temporal.');
            }

            $cards = self::loadAll();
            $total = count($cards);

            $zip = new \ZipArchive();
            $opened = $zip->open($dir . '/cards.zip', \ZipArchive::CREATE);
            if ($opened !== true) {
                throw new \RuntimeException('No se pudo abrir el zip (código ' . var_export($opened, true) . ').');
            }

            for (; $done < $total && (microtime(true) - $started) < self::STEP_SECONDS; $done++) {
                $card = $cards[$done];
                try {
                    $data = $format === 'pdf' ? self::renderPdf($card, $logo, $code) : self::renderPng($card, $logo, $code);
                } catch (\Throwable $e) {
                    Log::error('Falló la tarjeta del empleado ' . $card['employeeNumber'], ['posicion' => $done + 1, 'de' => $total], $e);
                    throw $e;
                }

                $name = self::safeFileName($card['employeeNumber'] . '_' . $card['fullName']);
                $unique = $name;
                for ($n = 2; isset($used[mb_strtolower($unique)]); $n++) {
                    $unique = "{$name}_{$n}";
                }
                $used[mb_strtolower($unique)] = true;

                $zip->addFromString("{$unique}.{$extension}", $data);
                // PNG y PDF ya vienen comprimidos: se guardan sin recomprimir (más rápido).
                $zip->setCompressionName("{$unique}.{$extension}", \ZipArchive::CM_STORE);
                unset($data);
            }

            if (!$zip->close()) {
                throw new \RuntimeException('No se pudo escribir el zip (¿disco lleno o sin permisos?).');
            }
        } catch (\Throwable $e) {
            Log::error('Generación de tarjetas: fallo', ['avance' => $done, 'memoria_pico_mb' => round(memory_get_peak_usage(true) / 1048576, 1)], $e);
            self::removeDir($dir);
            return ['isSuccess' => false, 'message' => 'Error al generar las tarjetas: ' . $e->getMessage() . ' (detalle en storage/logs).'];
        }

        $meta['done'] = $done;
        $meta['total'] = $total;
        $meta['names'] = $used ?: new \stdClass();
        file_put_contents($dir . '/meta.json', json_encode($meta));
        touch($dir);

        Log::info('Generación de tarjetas: avance', [
            'procesadas' => $done - $from, 'acumulado' => "{$done}/{$total}",
            'segundos' => round(microtime(true) - $started, 1),
            'memoria_pico_mb' => round(memory_get_peak_usage(true) / 1048576, 1),
        ]);

        return ['isSuccess' => true, 'message' => 'Generando…', 'done' => $done, 'total' => $total, 'finished' => $done >= $total];
    }

    /** @return array{path:string,fileName:string,count:int}|null */
    public static function jobResult(string $id): ?array
    {
        $dir = self::jobDir($id);
        if ($dir === null || !is_file($dir . '/cards.zip')) {
            return null;
        }
        $meta = json_decode((string)file_get_contents($dir . '/meta.json'), true);
        if ((int)$meta['done'] < (int)$meta['total']) {
            return null;
        }
        return ['path' => $dir . '/cards.zip', 'fileName' => 'tarjetas_invitacion_' . ($meta['format'] === 'pdf' ? 'pdf' : 'png') . '.zip', 'count' => (int)$meta['total']];
    }

    public static function deleteJob(string $id): void
    {
        $dir = self::jobDir($id);
        if ($dir !== null) {
            self::removeDir($dir);
        }
    }

    private static function removeDir(string $dir): void
    {
        foreach (glob($dir . '/*') ?: [] as $f) {
            @unlink($f);
        }
        @rmdir($dir);
    }

    // ---------- utilidades de dibujo ----------

    private static function tmpDir(): string
    {
        $dir = BASE_PATH . '/storage/tmp';
        if (!is_dir($dir)) {
            @mkdir($dir, 0775, true);
        }
        return is_dir($dir) && is_writable($dir) ? $dir : sys_get_temp_dir();
    }

    private static function safeFileName(string $name): string
    {
        $clean = trim(preg_replace('/[\\\\\\/:*?"<>|\x00-\x1f]/u', '_', $name) ?? '');
        return $clean === '' ? 'tarjeta' : mb_substr($clean, 0, 120);
    }

    private static function color(\GdImage $im, string $hex): int
    {
        $hex = ltrim($hex, '#');
        return imagecolorallocate($im, hexdec(substr($hex, 0, 2)), hexdec(substr($hex, 2, 2)), hexdec(substr($hex, 4, 2)));
    }

    private static function roundRect(\GdImage $im, float $x1, float $y1, float $x2, float $y2, float $r, int $color): void
    {
        $x1 = (int)$x1; $y1 = (int)$y1; $x2 = (int)$x2; $y2 = (int)$y2; $r = (int)$r;
        imagefilledrectangle($im, $x1 + $r, $y1, $x2 - $r, $y2, $color);
        imagefilledrectangle($im, $x1, $y1 + $r, $x2, $y2 - $r, $color);
        $d = $r * 2;
        imagefilledellipse($im, $x1 + $r, $y1 + $r, $d, $d, $color);
        imagefilledellipse($im, $x2 - $r, $y1 + $r, $d, $d, $color);
        imagefilledellipse($im, $x1 + $r, $y2 - $r, $d, $d, $color);
        imagefilledellipse($im, $x2 - $r, $y2 - $r, $d, $d, $color);
    }

    /**
     * Texto con tipografía TTF. x, y y size están en píxeles de la tarjeta (1050 x 600);
     * y es la línea base. Si no cabe en $maxWidth, se reduce el tamaño.
     */
    private static function text(\GdImage $im, string $text, float $x, float $y, bool $bold, float $size, int $color,
                                 float $maxWidth = 0, string $align = 'left'): void
    {
        $s = self::SCALE;
        $font = $bold ? self::fontBold() : self::fontRegular();
        $px = $size * $s;

        $width = self::measure($text, $font, $px);
        if ($maxWidth > 0 && $width > $maxWidth * $s) {
            $px = max(12 * $s, $px * ($maxWidth * $s) / $width);
            $width = self::measure($text, $font, $px);
        }

        $left = $x * $s;
        if ($align === 'center') {
            $left -= $width / 2;
        }
        // GD mide el tamaño en puntos a 96 ppp: 1 px = 0.75 pt.
        imagettftext($im, $px * 0.75, 0, (int)round($left), (int)round($y * $s), $color, $font, $text);
    }

    private static function measure(string $text, string $font, float $px): float
    {
        $box = imagettfbbox($px * 0.75, 0, $font, $text);
        return $box ? (float)abs($box[2] - $box[0]) : 0.0;
    }

    private static function drawQr(\GdImage $im, string $content, int $dark, int $white, int $lavender): void
    {
        $s = self::SCALE;
        $box = 260;
        $qrX = self::WIDTH - 70 - $box;
        $qrY = 150;

        self::roundRect($im, ($qrX - 14) * $s, ($qrY - 14) * $s, ($qrX + $box + 14) * $s, ($qrY + $box + 14) * $s, 18 * $s, $white);

        $qr = Encoder::encode($content, ErrorCorrectionLevel::Q(), 'UTF-8');
        $matrix = $qr->getMatrix();
        $count = $matrix->getWidth();
        $cell = ($box * $s) / $count;

        for ($row = 0; $row < $count; $row++) {
            for ($col = 0; $col < $count; $col++) {
                if ($matrix->get($col, $row) === 1) {
                    imagefilledrectangle(
                        $im,
                        (int)floor($qrX * $s + $col * $cell), (int)floor($qrY * $s + $row * $cell),
                        (int)ceil($qrX * $s + ($col + 1) * $cell) - 1, (int)ceil($qrY * $s + ($row + 1) * $cell) - 1,
                        $dark
                    );
                }
            }
        }

        self::text($im, 'Presenta este código en el acceso', $qrX + $box / 2, $qrY + $box + 52, false, 20, $lavender, $box + 60, 'center');
    }

    private static function drawBarcode(\GdImage $im, string $content, int $dark, int $white, int $lavender): void
    {
        $s = self::SCALE;
        $boxW = 360; // ZXing exige un margen blanco de al menos la mitad del ancho del código
        $boxH = 200;
        $boxX = self::WIDTH - 50 - $boxW;
        $boxY = 190;

        self::roundRect($im, $boxX * $s, $boxY * $s, ($boxX + $boxW) * $s, ($boxY + $boxH) * $s, 18 * $s, $white);

        // Code 128 solo codifica ASCII imprimible; lo demás se sustituye por "?".
        $text = preg_replace('/[^\x20-\x7e]/u', '?', $content) ?? '?';
        $barcode = (new TypeCode128())->getBarcodeData($text);
        $modules = (int)$barcode->getWidth();

        $maxBars = $boxW / 2 - 4; // el código ocupa como mucho la mitad de la caja
        $cell = max(1.0, floor($maxBars / $modules));
        if ($cell * $modules > $maxBars) {
            $cell = $maxBars / $modules; // texto muy largo: se encoge
        }
        $barsW = $cell * $modules;
        $barsX = $boxX + ($boxW - $barsW) / 2;
        $barsY = $boxY + 24;
        $barsH = 110;

        $cursor = 0;
        foreach ($barcode->getBars() as $bar) {
            $w = (int)$bar->getWidth();
            if ($bar->isBar()) {
                imagefilledrectangle(
                    $im,
                    (int)round(($barsX + $cursor * $cell) * $s), (int)round($barsY * $s),
                    (int)round(($barsX + ($cursor + $w) * $cell) * $s) - 1, (int)round(($barsY + $barsH) * $s) - 1,
                    $dark
                );
            }
            $cursor += $w;
        }

        self::text($im, $text, $boxX + $boxW / 2, $boxY + 24 + $barsH + 34, true, 28, $dark, $boxW - 24, 'center');
        self::text($im, 'Presenta este código en el acceso', $boxX + $boxW / 2, $boxY + $boxH + 36, false, 20, $lavender, $boxW + 60, 'center');
    }
}
