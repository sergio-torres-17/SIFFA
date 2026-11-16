<div id="dash" data-url="<?= e(url('/ajax/dashboard')) ?>">
    <div class="kpis">
        <div class="kpi"><span>Registrados</span><b id="k-total">–</b></div>
        <div class="kpi ok"><span>Han ingresado</span><b id="k-entered">–</b></div>
        <div class="kpi warn"><span>Pendientes</span><b id="k-pending">–</b></div>
        <div class="kpi"><span>Dentro ahora</span><b id="k-inside">–</b></div>
        <div class="kpi"><span>Han salido</span><b id="k-exited">–</b></div>
    </div>
    <p class="muted small" id="d-updated">Cargando…</p>

    <div class="grid2">
        <div class="card">
            <h3>Ingresos por hora</h3>
            <div id="hours" class="bars"></div>
        </div>
        <div class="card">
            <h3>Avance por área</h3>
            <div id="areas"></div>
        </div>
    </div>

    <div class="card">
        <h3>Últimos accesos</h3>
        <div class="table-wrap">
            <table>
                <thead><tr><th>Hora</th><th>Número</th><th>Nombre</th><th>Área</th><th>Movimiento</th></tr></thead>
                <tbody id="latest"><tr><td colspan="5" class="muted">Sin datos</td></tr></tbody>
            </table>
        </div>
    </div>
</div>
