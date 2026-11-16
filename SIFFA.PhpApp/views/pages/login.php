<?php use Siffa\Core\Http; ?>
<?php if (!empty($needsSetup)): ?>
    <div class="alert warn">El sistema aún no está instalado o activado. <a href="<?= e(url('/setup')) ?>">Ir a la instalación</a></div>
<?php endif; ?>
<form method="post" action="<?= e(url('/login')) ?>" class="stack" autocomplete="on">
    <input type="hidden" name="_csrf" value="<?= e(Http::csrfToken()) ?>">
    <label>Correo
        <input type="email" name="email" required autofocus autocomplete="username">
    </label>
    <label>Contraseña
        <input type="password" name="password" required autocomplete="current-password">
    </label>
    <button class="btn primary block" type="submit">Entrar</button>
</form>
