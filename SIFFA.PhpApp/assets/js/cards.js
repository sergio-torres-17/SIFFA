(function () {
  const { post, toast, esc } = Siffa;
  const $ = (id) => document.getElementById(id);
  const root = $('cards');
  const logo = $('logo');
  const rel = (u) => u.replace(Siffa.base, '');

  logo.addEventListener('change', () => {
    const ok = logo.files.length === 1;
    $('btn-preview').disabled = !ok;
    $('btn-generate').disabled = !ok;
    if (ok) preview();
  });
  $('code').addEventListener('change', () => logo.files.length && preview());
  $('btn-preview').addEventListener('click', preview);

  function form() {
    const f = new FormData();
    f.append('logo', logo.files[0]);
    f.append('code', $('code').value);
    f.append('format', $('format').value);
    return f;
  }

  async function preview() {
    $('preview').innerHTML = '<span class="muted">Generando…</span>';
    const r = await post(rel(root.dataset.preview), form());
    if (!r.isSuccess) { $('preview').innerHTML = '<span class="muted">' + esc(r.message) + '</span>'; return; }
    $('preview').innerHTML = `<div><img alt="Vista previa" src="${r.image}"><p class="muted small center">${esc(r.sample.fullName)} · ${esc(r.sample.employeeNumber)}</p></div>`;
  }

  // Las tarjetas se generan en lotes cortos (el hosting corta las peticiones largas) y al final se descarga el .zip.
  $('btn-generate').addEventListener('click', async () => {
    const btn = $('btn-generate');
    const label = 'Generar todas (.zip)';
    btn.disabled = true; btn.textContent = 'Preparando…';
    try {
      const start = await post(rel(root.dataset.start), form());
      if (!start.isSuccess) { toast(start.message || 'No se pudieron generar las tarjetas.', 'error'); return; }

      let step;
      do {
        const f = new FormData();
        f.append('job', start.job);
        step = await post(rel(root.dataset.step), f);
        if (!step.isSuccess) { toast(step.message || 'No se pudieron generar las tarjetas.', 'error'); return; }
        btn.textContent = `Generando… ${step.done} de ${step.total}`;
      } while (!step.finished);

      window.location.href = root.dataset.download + '?job=' + encodeURIComponent(start.job);
      toast('Tarjetas generadas: ' + step.total, 'success');
    } catch (e) {
      toast('Error de conexión al generar. Revisa storage/logs en el servidor.', 'error');
    } finally {
      btn.disabled = false; btn.textContent = label;
    }
  });
})();
