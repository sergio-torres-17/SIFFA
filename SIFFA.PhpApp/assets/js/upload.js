(function () {
  const { post, toast, esc } = Siffa;
  const $ = (id) => document.getElementById(id);
  const urls = window.UPLOAD_URLS;
  const rel = (u) => u.replace(Siffa.base, '');
  let data = null, mode = 'area';

  function go(n) {
    document.querySelectorAll('[data-step]').forEach((s) => (s.hidden = s.dataset.step != n));
    document.querySelectorAll('.stepper li').forEach((l) => l.classList.toggle('on', l.dataset.s == n));
  }

  $('xlsx').addEventListener('change', () => ($('btn-parse').disabled = $('xlsx').files.length !== 1));

  $('btn-parse').addEventListener('click', async () => {
    const f = new FormData(); f.append('file', $('xlsx').files[0]);
    $('btn-parse').disabled = true;
    const r = await post(rel(urls.parse), f);
    $('btn-parse').disabled = false;
    const w = $('warnings');
    if (!r.isSuccess) {
      toast(r.message, 'error');
      if (r.warnings && r.warnings.length) { w.hidden = false; w.innerHTML = r.warnings.map(esc).join('<br>'); }
      return;
    }
    data = r;
    $('summary').textContent = r.count + ' empleados en "' + r.fileName + '"' + (r.warningsTotal ? ' · ' + r.warningsTotal + ' renglones omitidos' : '') + '.';
    renderAlloc();
    go(2);
  });

  document.querySelectorAll('#umode button').forEach((b) => b.addEventListener('click', () => {
    mode = b.dataset.mode;
    document.querySelectorAll('#umode button').forEach((x) => x.classList.toggle('on', x === b));
    renderAlloc();
  }));
  document.querySelector('[data-back]').addEventListener('click', () => go(1));

  function renderAlloc() {
    const rubros = mode === 'area' ? data.areas : data.types;
    if (!data.addTypes.length) { $('alloc').innerHTML = '<tr><td class="muted">No hay adicionales en el catálogo. Puedes continuar sin boletos.</td></tr>'; return; }
    const head = '<thead><tr><th>' + (mode === 'area' ? 'Área' : 'Tipo') + '</th>' + data.addTypes.map((a) => '<th>' + esc(a.name) + '</th>').join('') + '</tr></thead>';
    const body = rubros.map((r) => '<tr><td>' + esc(r.name) + ' <span class="muted small">(' + r.count + ')</span></td>' +
      data.addTypes.map((a) => `<td><input type="number" min="0" max="99" value="0" data-key="${esc(r.name)}" data-add="${esc(a.addTypeId)}"></td>`).join('') + '</tr>').join('');
    $('alloc').innerHTML = head + '<tbody>' + body + '</tbody>';
  }

  $('btn-register').addEventListener('click', async () => {
    const allocations = [...document.querySelectorAll('#alloc input')].map((i) => ({ key: i.dataset.key, addTypeId: i.dataset.add, quantity: parseInt(i.value, 10) || 0 }));
    $('btn-register').disabled = true;
    const r = await post(rel(urls.register), { mode, allocations });
    $('btn-register').disabled = false;
    if (!r.isSuccess) return toast(r.message, 'error');
    $('done-msg').textContent = r.message;
    go(3);
  });
})();
