<div class="row end"><button class="btn primary" id="add-new" type="button">Nuevo adicional</button></div>
<div class="card">
    <div class="table-wrap">
        <table>
            <thead><tr><th>Nombre</th><th>Descripción</th><th>Boletos</th><th>Configs.</th><th></th></tr></thead>
            <tbody>
            <?php foreach ($items as $a): ?>
                <tr data-id="<?= e($a['addTypeId']) ?>" data-name="<?= e($a['name']) ?>" data-desc="<?= e($a['description']) ?>">
                    <td><?= e($a['name']) ?></td>
                    <td class="muted"><?= e($a['description']) ?></td>
                    <td><?= e($a['ticketsAssigned']) ?></td>
                    <td><?= e($a['configurations']) ?></td>
                    <td class="end"><button class="btn ghost sm" data-edit>Editar</button> <button class="btn danger sm" data-del>Eliminar</button></td>
                </tr>
            <?php endforeach; ?>
            <?php if (!$items): ?><tr><td colspan="5" class="muted">Sin adicionales.</td></tr><?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<div class="modal" id="m-add" hidden>
    <form class="modal-box stack" id="f-add">
        <h3 id="m-add-title">Adicional</h3>
        <input type="hidden" name="addTypeId">
        <label>Nombre <input name="name" maxlength="50" required></label>
        <label>Descripción <input name="description" maxlength="200"></label>
        <div class="row end"><button type="button" class="btn ghost" data-close>Cancelar</button><button class="btn primary">Guardar</button></div>
    </form>
</div>
