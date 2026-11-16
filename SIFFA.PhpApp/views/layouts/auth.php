<?php
/** @var string $content @var string $title */
use Siffa\Core\Http;
?><!doctype html>
<html lang="es">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="csrf-token" content="<?= e(Http::csrfToken()) ?>">
    <meta name="base-url" content="<?= e(Http::url('/')) ?>">
    <title><?= e($title) ?> · SIFFA</title>
    <link rel="stylesheet" href="<?= e(asset('css/app.css')) ?>">
</head>
<body class="auth-body">
<main class="auth-card">
    <div class="brand big"><span class="brand-mark">S</span><span>SIFFA</span></div>
    <p class="muted center">Control de ingresos · Fiesta de fin de año</p>
    <?php foreach (Http::takeFlash() as $f): ?>
        <div class="alert <?= e($f['type']) ?>"><?= e($f['message']) ?></div>
    <?php endforeach; ?>
    <?= $content ?>
</main>
</body>
</html>
