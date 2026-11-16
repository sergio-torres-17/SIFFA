<div class="card" id="upload">
    <ol class="stepper"><li class="on" data-s="1">1 · Archivo</li><li data-s="2">2 · Adicionales</li><li data-s="3">3 · Listo</li></ol>

    <div data-step="1">
        <p class="muted">Excel (.xlsx), renglón 1 con encabezados y las columnas en este orden:
            <b>Tipo de empleado · Número · Nombre · Apellidos · Área · Correo</b>.</p>
        <input type="file" id="xlsx" accept=".xlsx">
        <div class="row"><button type="button" class="btn primary" id="btn-parse" disabled>Leer archivo</button></div>
        <div id="warnings" class="alert warn" hidden></div>
    </div>

    <div data-step="2" hidden>
        <p id="summary" class="muted"></p>
        <div class="seg" id="umode">
            <button type="button" class="on" data-mode="area">Por área</button>
            <button type="button" data-mode="type">Por tipo de empleado</button>
        </div>
        <p class="muted small">Cantidad de boletos de cada adicional que recibirá cada empleado del rubro (0 = ninguno).</p>
        <div class="table-wrap"><table id="alloc"></table></div>
        <div class="row end">
            <button type="button" class="btn ghost" data-back>Atrás</button>
            <button type="button" class="btn primary" id="btn-register">Registrar empleados</button>
        </div>
    </div>

    <div data-step="3" hidden>
        <div class="alert success" id="done-msg"></div>
        <div class="row"><a class="btn primary" href="<?= e(url('/cards')) ?>">Ver invitaciones</a>
        <a class="btn ghost" href="<?= e(url('/upload')) ?>">Subir otro archivo</a></div>
    </div>
</div>
<script>window.UPLOAD_URLS = {parse: <?= json_encode(url('/ajax/upload/parse')) ?>, register: <?= json_encode(url('/ajax/upload/register')) ?>};</script>
