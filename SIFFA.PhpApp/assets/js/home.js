(function () {
  const { esc, get } = Siffa;
  const $ = (id) => document.getElementById(id);
  async function load() {
    const d = await get('/ajax/dashboard');
    if (d.totalEmployees === undefined) { $('d-updated').textContent = d.message || 'No se pudo cargar.'; return; }
    $('k-total').textContent = d.totalEmployees;
    $('k-entered').textContent = d.entered;
    $('k-pending').textContent = d.pending;
    $('k-inside').textContent = d.inside;
    $('k-exited').textContent = d.exited;
    $('d-updated').textContent = 'Actualizado a las ' + d.updatedAt + ' · se refresca cada 30 s' + (d.hasAccessActions ? '' : ' · faltan las acciones Ingreso/Salida en el catálogo');

    const max = Math.max(1, ...d.entriesByHour.map((h) => h.entries));
    $('hours').innerHTML = d.entriesByHour.length
      ? d.entriesByHour.map((h) => `<div class="bar" title="${h.entries} ingresos"><span>${h.entries}</span><i style="height:${Math.round(h.entries / max * 130)}px"></i><span>${String(h.hour).padStart(2, '0')}h</span></div>`).join('')
      : '<span class="muted">Aún no hay ingresos.</span>';

    $('areas').innerHTML = d.byArea.length
      ? d.byArea.map((a) => `<div class="area-row"><div class="top"><span>${esc(a.area)}</span><span>${a.entered}/${a.registered}</span></div><div class="progress"><i style="width:${a.registered ? Math.round(a.entered / a.registered * 100) : 0}%"></i></div></div>`).join('')
      : '<span class="muted">Sin empleados registrados.</span>';

    $('latest').innerHTML = d.latestAccesses.length
      ? d.latestAccesses.map((r) => `<tr><td>${esc(r.date.substring(11, 19))}</td><td>${esc(r.employeeNumber)}</td><td>${esc(r.employeeName)}</td><td>${esc(r.area)}</td><td><span class="tag ${r.isEntry ? 'ok' : 'warn'}">${esc(r.action)}</span></td></tr>`).join('')
      : '<tr><td colspan="5" class="muted">Sin movimientos.</td></tr>';
  }
  load();
  setInterval(load, 30000);
})();
