(function () {
  const { post, toast } = Siffa;
  const $ = (id) => document.getElementById(id);

  $('user-new').addEventListener('click', () => { $('f-user').reset(); Siffa.openModal('m-user'); });

  document.addEventListener('click', async (ev) => {
    const row = ev.target.closest('tr[data-id]'); if (!row) return;
    if (ev.target.closest('[data-reset]')) {
      const f = $('f-reset'); f.reset();
      f.userId.value = row.dataset.id;
      $('reset-who').textContent = row.dataset.email;
      Siffa.openModal('m-reset');
    }
    if (ev.target.closest('[data-del]')) {
      if (!confirm('¿Eliminar al usuario ' + row.dataset.email + '?')) return;
      const r = await post('/ajax/users/delete', { userId: row.dataset.id });
      toast(r.message, r.isSuccess ? 'success' : 'error');
      if (r.isSuccess) setTimeout(() => location.reload(), 600);
    }
  });

  $('f-user').addEventListener('submit', async (ev) => {
    ev.preventDefault();
    const f = ev.target;
    const r = await post('/ajax/users/create', { firstName: f.firstName.value, lastName: f.lastName.value, email: f.email.value, password: f.password.value });
    toast(r.message, r.isSuccess ? 'success' : 'error');
    if (r.isSuccess) setTimeout(() => location.reload(), 600);
  });
  $('f-reset').addEventListener('submit', async (ev) => {
    ev.preventDefault();
    const f = ev.target;
    const r = await post('/ajax/users/reset', { userId: f.userId.value, password: f.password.value });
    toast(r.message, r.isSuccess ? 'success' : 'error');
    if (r.isSuccess) Siffa.closeModal('m-reset');
  });
})();
