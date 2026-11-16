<?php
// Solo para desarrollo con el servidor integrado de PHP:  php -S 127.0.0.1:8080 router-dev.php
$path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH) ?: '/';
$file = __DIR__ . $path;

if ($path !== '/' && is_file($file) && !preg_match('#^/(app|vendor|storage|database|views)/#', $path) && !str_starts_with(basename($path), '.')) {
    return false; // archivo estático (assets/…)
}

$_SERVER['SCRIPT_NAME'] = '/index.php';
require __DIR__ . '/index.php';
