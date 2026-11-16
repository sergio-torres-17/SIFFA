<?php
/** @var string $content @var string $title */
use Siffa\Core\Auth;
use Siffa\Core\Http;

$nav = $nav ?? '';
$scripts = $scripts ?? [];
$user = Auth::user();
$items = [
    ['home', '/', 'Inicio', 'M3 11l9-8 9 8v10a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z'],
    ['access', '/access', 'Control de accesos', 'M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h2v2h-2zM18 14h2v6h-4v-2h2z'],
    ['cards', '/cards', 'Ver invitaciones', 'M3 5h18v14H3zM3 9h18M7 14h4'],
    ['upload', '/upload', 'Subir empleados', 'M12 16V4m0 0l-4 4m4-4l4 4M4 20h16'],
    ['adds', '/adds', 'Adicionales', 'M12 5v14M5 12h14'],
    ['users', '/users', 'Usuarios', 'M16 11a4 4 0 1 0-8 0 4 4 0 0 0 8 0zM4 21a8 8 0 0 1 16 0'],
];
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
<body>
<div class="shell">
    <aside class="side" id="side">
        <div class="brand"><span class="brand-mark">S</span><span>SIFFA</span></div>
        <nav>
            <?php foreach ($items as [$key, $path, $label, $icon]): ?>
                <a href="<?= e(url($path)) ?>" class="nav-item<?= $nav === $key ? ' active' : '' ?>">
                    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="<?= e($icon) ?>"/></svg>
                    <span><?= e($label) ?></span>
                </a>
            <?php endforeach; ?>
        </nav>
        <div class="side-foot">
            <div class="who"><?= e($user['name'] ?? '') ?><small><?= e($user['email'] ?? '') ?></small></div>
            <form method="post" action="<?= e(url('/logout')) ?>">
                <input type="hidden" name="_csrf" value="<?= e(Http::csrfToken()) ?>">
                <button class="btn ghost block" type="submit">Cerrar sesión</button>
            </form>
        </div>
    </aside>
    <main class="main">
        <header class="topbar">
            <button class="burger" type="button" onclick="document.getElementById('side').classList.toggle('open')" aria-label="Menú">☰</button>
            <h1><?= e($title) ?></h1>
        </header>
        <section class="page">
            <?php foreach (Http::takeFlash() as $f): ?>
                <div class="alert <?= e($f['type']) ?>"><?= e($f['message']) ?></div>
            <?php endforeach; ?>
            <?= $content ?>
        </section>
    </main>
</div>
<div class="toasts" id="toasts"></div>
<script src="<?= e(asset('js/app.js')) ?>"></script>
<?php foreach ($scripts as $s): ?>
<script src="<?= e(asset('js/' . $s)) ?>"></script>
<?php endforeach; ?>
</body>
</html>
