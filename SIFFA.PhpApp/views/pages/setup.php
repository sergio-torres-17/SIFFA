<?php
use Siffa\Core\Http;
/** @var array $status */
$ready = $status['connected'] && $status['hasTables'];
?>
<h2>Instalación</h2>
<ol class="steps">
    <li class="<?= $status['connected'] ? 'ok' : 'bad' ?>">Conexión a la base de datos
        <?php if (!$status['connected']): ?><small>No se pudo conectar. Revisa DB_HOST, DB_NAME, DB_USER y DB_PASS en tu archivo <code>.env</code>.<br><?= e($status['error']) ?></small><?php endif; ?></li>
    <li class="<?= $status['hasTables'] ? 'ok' : 'bad' ?>">Tablas creadas</li>
    <li class="<?= $status['activated'] ? 'ok' : 'bad' ?>">Administrador activado</li>
</ol>

<?php if (!$keyConfigured): ?>
    <div class="alert warn">Define <code>ADMIN_ACTIVATION_KEY</code> en tu <code>.env</code> para poder instalar y activar el sistema.</div>
<?php endif; ?>

<?php if ($status['connected'] && !$status['hasTables']): ?>
    <form method="post" action="<?= e(url('/setup/install')) ?>" class="stack">
        <input type="hidden" name="_csrf" value="<?= e(Http::csrfToken()) ?>">
        <label>Clave de activación
            <input type="password" name="key" required autocomplete="off">
        </label>
        <button class="btn primary block" type="submit">Crear tablas y datos base</button>
    </form>
<?php elseif ($ready && !$status['activated']): ?>
    <form method="post" action="<?= e(url('/setup/activate')) ?>" class="stack">
        <input type="hidden" name="_csrf" value="<?= e(Http::csrfToken()) ?>">
        <p class="muted">El administrador será <strong><?= e($adminEmail) ?></strong>.</p>
        <label>Clave de activación
            <input type="password" name="key" required autocomplete="off">
        </label>
        <label>Contraseña del administrador (mín. 8 caracteres)
            <input type="password" name="password" required minlength="8" autocomplete="new-password">
        </label>
        <label>Confirmar contraseña
            <input type="password" name="confirm" required minlength="8" autocomplete="new-password">
        </label>
        <button class="btn primary block" type="submit">Activar sistema</button>
    </form>
<?php elseif ($status['activated']): ?>
    <p class="center"><a class="btn primary" href="<?= e(url('/login')) ?>">Ir a iniciar sesión</a></p>
<?php endif; ?>
