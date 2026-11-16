<?php
declare(strict_types=1);

use Siffa\Core\Http;

/** Escapa texto para HTML. */
function e(mixed $value): string
{
    return htmlspecialchars((string)$value, ENT_QUOTES | ENT_SUBSTITUTE, 'UTF-8');
}

/** URL de una ruta de la aplicación (respeta la subcarpeta). */
function url(string $path = '/'): string
{
    return Http::url($path);
}

/** URL de un archivo estático con parámetro de versión para vencer la caché. */
function asset(string $path): string
{
    $file = BASE_PATH . '/assets/' . ltrim($path, '/');
    // Versión por contenido (no por fecha): si el archivo cambia, la URL cambia y ni el CDN ni el navegador sirven la copia vieja.
    $v = is_file($file) ? substr((string)md5_file($file), 0, 10) : '1';
    return Http::url('/assets/' . ltrim($path, '/')) . '?v=' . $v;
}
