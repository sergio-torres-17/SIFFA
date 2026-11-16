/* SIFFA · utilidades compartidas */
(function () {
  const meta = (n) => document.querySelector('meta[name="' + n + '"]')?.content || '';
  const csrf = meta('csrf-token');
  const base = meta('base-url').replace(/\/$/, '');

  const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  function toast(message, type) {
    const box = document.getElementById('toasts');
    if (!box) return alert(message);
    const t = document.createElement('div');
    t.className = 'toast ' + (type || '');
    t.textContent = message;
    box.appendChild(t);
    setTimeout(() => t.remove(), 5000);
  }

  /** POST JSON o FormData. Devuelve el JSON (también cuando el servidor responde con error). */
  async function post(path, data) {
    const headers = { 'Accept': 'application/json', 'X-CSRF-Token': csrf };
    let body;
    if (data instanceof FormData) {
      body = data;
    } else {
      headers['Content-Type'] = 'application/json';
      body = JSON.stringify(data || {});
    }
    return request(path, { method: 'POST', headers, body });
  }

  async function get(path) {
    return request(path, { headers: { 'Accept': 'application/json' } });
  }

  async function request(path, options) {
    try {
      const res = await fetch(base + path, { credentials: 'same-origin', ...options });
      if (res.status === 401) { location.href = base + '/login'; return { isSuccess: false, message: 'Sesión expirada.' }; }
      const json = await res.json().catch(() => null);
      return json || { isSuccess: false, message: 'Respuesta inesperada del servidor (' + res.status + ').' };
    } catch (e) {
      return { isSuccess: false, message: 'No se pudo conectar con el servidor.' };
    }
  }

  function openModal(id) { const m = document.getElementById(id); if (m) { m.hidden = false; m.querySelector('input:not([type=hidden]),button')?.focus(); } }
  function closeModal(id) { const m = document.getElementById(id); if (m) m.hidden = true; }

  document.addEventListener('click', (ev) => {
    if (ev.target.closest('[data-close]')) ev.target.closest('.modal').hidden = true;
  });
  document.addEventListener('keydown', (ev) => {
    if (ev.key === 'Escape') document.querySelectorAll('.modal:not([hidden])').forEach((m) => (m.hidden = true));
  });

  window.Siffa = { esc, toast, post, get, openModal, closeModal, base };
})();
