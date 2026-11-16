<div class="row end"><button class="btn primary" id="user-new" type="button">Nuevo usuario</button></div>
<div class="card">
    <div class="table-wrap">
        <table>
            <thead><tr><th>Nombre</th><th>Correo</th><th>Estado</th><th></th></tr></thead>
            <tbody>
            <?php foreach ($items as $u): ?>
                <tr data-id="<?= e($u['userId']) ?>" data-email="<?= e($u['email']) ?>">
                    <td><?= e($u['fullName']) ?><?= $u['isAdmin'] ? ' <span class="tag">Admin</span>' : '' ?></td>
                    <td><?= e($u['email']) ?></td>
                    <td><?= $u['hasCredential'] ? '<span class="tag ok">Activo</span>' : '<span class="tag warn">Sin contraseña</span>' ?></td>
                    <td class="end">
                        <button class="btn ghost sm" data-reset>Contraseña</button>
                        <?php if (!$u['isAdmin'] && $u['userId'] !== $currentUserId): ?><button class="btn danger sm" data-del>Eliminar</button><?php endif; ?>
                    </td>
                </tr>
            <?php endforeach; ?>
            </tbody>
        </table>
    </div>
</div>

<div class="modal" id="m-user" hidden>
    <form class="modal-box stack" id="f-user">
        <h3>Nuevo usuario</h3>
        <label>Nombre <input name="firstName" maxlength="100" required></label>
        <label>Apellidos <input name="lastName" maxlength="100" required></label>
        <label>Correo <input type="email" name="email" maxlength="100" required></label>
        <label>Contraseña (mín. 8) <input type="password" name="password" minlength="8" required autocomplete="new-password"></label>
        <div class="row end"><button type="button" class="btn ghost" data-close>Cancelar</button><button class="btn primary">Crear</button></div>
    </form>
</div>

<div class="modal" id="m-reset" hidden>
    <form class="modal-box stack" id="f-reset">
        <h3>Nueva contraseña</h3>
        <p class="muted" id="reset-who"></p>
        <input type="hidden" name="userId">
        <label>Contraseña (mín. 8) <input type="password" name="password" minlength="8" required autocomplete="new-password"></label>
        <div class="row end"><button type="button" class="btn ghost" data-close>Cancelar</button><button class="btn primary">Guardar</button></div>
    </form>
</div>
