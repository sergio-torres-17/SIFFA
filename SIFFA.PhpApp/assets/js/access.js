(function () {
  const { esc, post, toast } = Siffa;
  const $ = (id) => document.getElementById(id);
  let mode = "entry",
    timer = null,
    current = null;

  document.querySelectorAll("#mode button").forEach((b) =>
    b.addEventListener("click", () => {
      mode = b.dataset.mode;
      document
        .querySelectorAll("#mode button")
        .forEach((x) => x.classList.toggle("on", x === b));
      $("term").focus();
    }),
  );

  const term = $("term");
  term.addEventListener("input", () => {
    clearTimeout(timer);
    const v = term.value.trim();
    if (v.length < 2) {
      $("suggest").hidden = true;
      return;
    }
    timer = setTimeout(async () => {
      const list = await post("/ajax/access/search", { term: v });
      if (!Array.isArray(list) || !list.length) {
        $("suggest").hidden = true;
        return;
      }
      $("suggest").innerHTML = list
        .map(
          (e) =>
            `<li data-n="${esc(e.employeeNumber)}">${esc(e.fullName)} · ${esc(e.employeeNumber)}<small>${esc(e.area)} · ${esc(e.email)}</small></li>`,
        )
        .join("");
      $("suggest").hidden = false;
    }, 250);
  });
  $("suggest").addEventListener("click", (ev) => {
    const li = ev.target.closest("li");
    if (!li) return;
    register(li.dataset.n);
  });
  term.addEventListener("keydown", async (ev) => {
    if (ev.key !== "Enter") return;
    ev.preventDefault();
    clearTimeout(timer);
    lookup(term.value.trim());
  });

  async function lookup(v) {
    if (!v) return;
    // Un lector envía el número exacto; si no, se busca y se toma la única coincidencia.
    const list = await post("/ajax/access/search", { term: v });
    if (Array.isArray(list) && list.length === 1)
      return register(list[0].employeeNumber);
    if (Array.isArray(list) && list.length > 1) return; // se elige de la lista
    register(v);
  }

  // ---- Lector con cámara (QR y código de barras) ----
  let stopCam = null,
    handling = false;
  const camBtn = $("cam-toggle");

  async function toggleCam() {
    if (stopCam) {
      closeCam();
      return;
    }
    camBtn.disabled = true;
    // El panel se muestra ya, con el estado de cada paso: si algo falla, el motivo queda a la vista en la pantalla.
    const msg = $("cam-msg");
    $("cam").hidden = false;
    msg.textContent =
      "Abriendo la cámara… acepta el permiso si el navegador lo pide.";
    try {
      if (typeof SiffaScanner === "undefined") {
        throw new Error(
          "No se cargó el lector (falta assets/js/scanner.js, zxing.min.js o quagga.min.js en el servidor).",
        );
      }
      stopCam = await SiffaScanner.start($("cam-video"), onScan, (t) => {
        msg.textContent = t;
      });
      camBtn.textContent = "Cerrar cámara";
    } catch (e) {
      const text = (e && e.message) || "No se pudo abrir la cámara.";
      $("cam").hidden = true;
      const box = $("result");
      box.hidden = false;
      box.className = "result bad";
      box.textContent =
        "Cámara: " + text + (e && e.name ? " [" + e.name + "]" : "");
      toast(text, "error");
    }
    camBtn.disabled = false;
  }

  function closeCam() {
    if (stopCam) stopCam();
    stopCam = null;
    $("cam").hidden = true;
    camBtn.textContent = "Escanear con la cámara";
  }

  async function onScan(text) {
    // Mientras se registra o se entrega boletos, se ignoran nuevas lecturas.
    if (handling || !$("deliver").hidden) return;
    handling = true;
    try {
      await lookup(text.trim());
      closeCam();
    } finally {
      handling = false;
    }
  }

  // Foto con la cámara nativa del equipo (enfoca mejor que el video en vivo, útil en tabletas).
  const photoInput = $("cam-photo"); // puede no existir si el servidor tiene una versión vieja de access.php
  if (photoInput)
    photoInput.addEventListener("change", async (ev) => {
      const file = ev.target.files && ev.target.files[0];
      ev.target.value = "";
      if (!file) return;
      toast("Leyendo la foto…", "success");
      try {
        const text = await SiffaScanner.decodeFile(file);
        if (!text) {
          toast(
            "No se encontró ningún código en la foto. Acércate y que se vea completo y enfocado.",
            "error",
          );
          return;
        }
        await onScan(text);
      } catch (e) {
        toast(e.message || "No se pudo leer la foto.", "error");
      }
    });

  camBtn.addEventListener("click", toggleCam);
  window.addEventListener("pagehide", closeCam);

  async function register(number) {
    $("suggest").hidden = true;
    const r = await post("/ajax/access/register", {
      employeeNumber: number,
      type: mode,
    });
    const box = $("result");
    box.hidden = false;
    box.className = "result " + (r.status ? "ok" : "bad");
    box.textContent = r.message || "Sin respuesta.";
    term.value = "";
    term.focus();
    if (r.status && mode === "entry" && r.employeeAdds && r.employeeAdds.length)
      openDelivery(r);
  }

  function openDelivery(r) {
    current = r;
    $("deliver-who").textContent = r.employeeName + " · " + r.employeeNumber;
    $("deliver-list").innerHTML = r.employeeAdds
      .map(
        (a) =>
          `<label class="ticket"><input type="checkbox" value="${esc(a.addTypeId)}" ${a.deliveryAdd ? "checked disabled" : ""}> ${esc(a.name)} × ${a.quantity}${a.deliveryAdd ? " (entregado)" : ""}</label>`,
      )
      .join("");
    Siffa.openModal("deliver");
  }
  $("deliver-skip").addEventListener("click", () => {
    Siffa.closeModal("deliver");
    term.focus();
  });
  $("deliver-save").addEventListener("click", async () => {
    const ids = [
      ...document.querySelectorAll(
        "#deliver-list input:checked:not(:disabled)",
      ),
    ].map((i) => i.value);
    if (!ids.length) {
      Siffa.closeModal("deliver");
      term.focus();
      return;
    }
    const r = await post("/ajax/access/deliveries", {
      employeeNumber: current.employeeNumber,
      addTypeIds: ids,
    });
    toast(r.message, r.isSuccess ? "success" : "error");
    Siffa.closeModal("deliver");
    term.focus();
  });
})();
