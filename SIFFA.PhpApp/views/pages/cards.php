<div class="card" id="cards" data-preview="<?= e(url('/ajax/cards/preview')) ?>" data-start="<?= e(url('/ajax/cards/start')) ?>" data-step="<?= e(url('/ajax/cards/step')) ?>" data-download="<?= e(url('/cards/download')) ?>">
    <div class="grid2">
        <div class="stack">
            <label>Logo de la empresa (PNG, JPG o WebP, máx. 2 MB)
                <input type="file" id="logo" accept="image/png,image/jpeg,image/webp">
            </label>
            <label>Tipo de código
                <select id="code"><option value="qr">Código QR</option><option value="barcode">Código de barras</option></select>
            </label>
            <label>Formato de salida
                <select id="format"><option value="png">Imágenes PNG</option><option value="pdf">PDF por tarjeta</option></select>
            </label>
            <div class="row">
                <button type="button" class="btn ghost" id="btn-preview" disabled>Vista previa</button>
                <button type="button" class="btn primary" id="btn-generate" disabled>Generar todas (.zip)</button>
            </div>
            <p class="muted small">El logo no se conserva: se usa solo para armar las tarjetas y se borra al terminar.</p>
        </div>
        <div class="preview" id="preview"><span class="muted">Elige un logo para ver la vista previa.</span></div>
    </div>
</div>
