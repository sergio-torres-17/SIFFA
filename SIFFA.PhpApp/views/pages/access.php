<div class="card access" id="access">
    <div class="seg" id="mode">
        <button type="button" class="on" data-mode="entry">Ingreso</button>
        <button type="button" data-mode="exit">Salida</button>
    </div>
    <label>Número de empleado, nombre o correo (o escanea el código)
        <input type="text" id="term" autocomplete="off" autofocus placeholder="Escanea o escribe y presiona Enter">
    </label>
    <div class="row">
        <button type="button" class="btn ghost" id="cam-toggle">Escanear con la cámara</button>
        <label class="btn ghost" for="cam-photo">Leer desde una foto</label>
        <input type="file" id="cam-photo" accept="image/*" capture="environment" hidden>
    </div>
    <div class="cam" id="cam" hidden>
        <video id="cam-video" playsinline muted></video>
        <div class="cam-frame"></div>
        <p class="muted small center" id="cam-msg">Apunta al código QR o de barras de la invitación.</p>
    </div>
    <ul class="suggest" id="suggest" hidden></ul>
    <div id="result" class="result" hidden></div>
</div>

<div class="modal" id="deliver" hidden>
    <div class="modal-box">
        <h3>Entrega de boletos</h3>
        <p id="deliver-who" class="muted"></p>
        <div id="deliver-list" class="stack"></div>
        <div class="row end">
            <button type="button" class="btn ghost" id="deliver-skip">Omitir</button>
            <button type="button" class="btn primary" id="deliver-save">Guardar entrega</button>
        </div>
    </div>
</div>
