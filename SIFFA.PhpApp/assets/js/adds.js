(function () {
  const { post, toast } = Siffa;
  const $ = (id) => document.getElementById(id);
  const form = $('f-add');

  function open(row) {
    form.addTypeId.value = row ? row.dataset.id : '';
    form.name.value = row ? row.dataset.name : '';
    form.description.value = row ? row.dataset.desc : '';
    $('m-add-title').textContent = row ? 'Editar adicional' : 'Nuevo adicional';
    Siffa.openModal('m-add');
  }
  $('add-new').addEventListener('click', () => open(null));
  document.addEventListener('click', async (ev) => {
    const row = ev.target.closest('tr[data-id]'); if (!row) return;
    if (ev.target.closest('[data-edit]')) open(row);
    if (ev.target.closest('[data-del]')) {
      if (!confirm('¿Eliminar "' + row.dataset.name + '"?')) return;
      const r = await post('/ajax/adds/delete', { addTypeId: row.dataset.id });
      toast(r.message, r.isSuccess ? 'success' : 'error');
      if (r.isSuccess) setTimeout(() => location.reload(), 600);
    }
  });
  form.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    const r = await post('/ajax/adds/save', { addTypeId: form.addTypeId.value, name: form.name.value, description: form.description.value });
    toast(r.message, r.isSuccess ? 'success' : 'error');
    if (r.isSuccess) setTimeout(() => location.reload(), 600);
  });
})();
