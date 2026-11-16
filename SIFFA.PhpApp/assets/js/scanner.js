/*
 * Lector de códigos QR y de barras con la cámara (compartido por la versión PHP y la .NET).
 *
 * Cada fotograma se prueba, en orden, con: BarcodeDetector del navegador (si existe), ZXing (QR y barras limpias)
 * y Quagga (barras dentro de la tarjeta completa). Las librerías zxing.min.js y quagga.min.js van incluidas.
 *
 *   const stop = await SiffaScanner.start(videoElement, (texto) => { ... }, (estado) => { ... });
 *   stop();                                          // apaga la cámara
 *   const texto = await SiffaScanner.decodeFile(f);  // lee un código de una foto (input type=file); '' si no hay
 *
 * La cámara en vivo solo funciona en HTTPS o en localhost; la lectura desde foto funciona en cualquier sitio.
 */
(function () {
  const NATIVE_FORMATS = ['qr_code', 'code_128', 'code_39', 'ean_13', 'ean_8', 'upc_a', 'upc_e', 'itf', 'codabar'];
  const REPEAT_MS = 2500; // el mismo código no se vuelve a reportar durante este tiempo
  const MAX_SIDE = 1600;  // lado máximo del fotograma que se analiza

  function beep() {
    try {
      if (navigator.vibrate) navigator.vibrate(60);
      const Ctx = window.AudioContext || window.webkitAudioContext;
      if (!Ctx) return;
      const ctx = new Ctx();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.frequency.value = 1100;
      gain.gain.value = 0.08;
      osc.connect(gain); gain.connect(ctx.destination);
      osc.start(); osc.stop(ctx.currentTime + 0.12);
      osc.onended = () => ctx.close();
    } catch (e) { /* el sonido es opcional */ }
  }

  async function openCamera() {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      throw new Error('El navegador no permite usar la cámara aquí (necesita HTTPS o localhost). Usa "Leer desde una foto".');
    }
    const tries = [
      { video: { facingMode: { ideal: 'environment' }, width: { ideal: 1920 }, height: { ideal: 1080 } }, audio: false },
      { video: { facingMode: { ideal: 'environment' } }, audio: false },
      { video: true, audio: false },
    ];
    let lastError = null;
    for (const constraints of tries) {
      try {
        return await navigator.mediaDevices.getUserMedia(constraints);
      } catch (e) {
        lastError = e;
        if (e && (e.name === 'NotAllowedError' || e.name === 'SecurityError')) {
          throw new Error('Permiso de cámara denegado. Permítelo en el navegador e inténtalo de nuevo.');
        }
      }
    }
    if (lastError && lastError.name === 'NotFoundError') throw new Error('No se encontró ninguna cámara.');
    throw new Error('No se pudo abrir la cámara: ' + (lastError && lastError.message ? lastError.message : lastError));
  }

  // Enfoque continuo (donde el navegador lo permite): sin él, las barras salen borrosas en tabletas.
  async function tuneCamera(stream) {
    try {
      const track = stream.getVideoTracks()[0];
      const caps = track.getCapabilities ? track.getCapabilities() : {};
      const advanced = [];
      if (caps.focusMode && caps.focusMode.includes('continuous')) advanced.push({ focusMode: 'continuous' });
      if (advanced.length) await track.applyConstraints({ advanced });
    } catch (e) { /* opcional */ }
  }

  // ---- Decodificación de un canvas con ZXing y Quagga ----

  let zxingReader = null;
  function zxing() {
    if (zxingReader || !window.ZXing) return zxingReader;
    const Z = window.ZXing;
    const hints = new Map();
    hints.set(Z.DecodeHintType.POSSIBLE_FORMATS, [
      Z.BarcodeFormat.QR_CODE, Z.BarcodeFormat.CODE_128, Z.BarcodeFormat.CODE_39,
      Z.BarcodeFormat.EAN_13, Z.BarcodeFormat.EAN_8, Z.BarcodeFormat.UPC_A, Z.BarcodeFormat.ITF,
    ]);
    hints.set(Z.DecodeHintType.TRY_HARDER, true);
    zxingReader = new Z.MultiFormatReader();
    zxingReader.setHints(hints);
    return zxingReader;
  }

  function decodeZxing(canvas) {
    const reader = zxing();
    if (!reader) return '';
    try {
      const Z = window.ZXing;
      const bitmap = new Z.BinaryBitmap(new Z.HybridBinarizer(new Z.HTMLCanvasElementLuminanceSource(canvas)));
      return reader.decode(bitmap).getText();
    } catch (e) { return ''; }
  }

  function decodeQuagga(canvas) {
    return new Promise((resolve) => {
      if (!(window.Quagga && window.Quagga.decodeSingle)) { resolve(''); return; }
      try {
        window.Quagga.decodeSingle({
          src: canvas.toDataURL('image/jpeg', 0.9),
          numOfWorkers: 0,
          locate: true,
          inputStream: { size: Math.max(canvas.width, canvas.height) },
          locator: { patchSize: 'medium', halfSample: false },
          decoder: { readers: ['code_128_reader', 'code_39_reader', 'ean_reader', 'ean_8_reader', 'upc_reader', 'i2of5_reader'] },
        }, (r) => resolve(r && r.codeResult && r.codeResult.code ? r.codeResult.code : ''));
      } catch (e) { resolve(''); }
    });
  }

  function drawScaled(source, w, h, canvas, ctx) {
    const scale = Math.min(1, MAX_SIDE / Math.max(w, h));
    canvas.width = Math.round(w * scale);
    canvas.height = Math.round(h * scale);
    ctx.drawImage(source, 0, 0, canvas.width, canvas.height);
  }

  async function decodeCanvas(canvas) {
    return decodeZxing(canvas) || (await decodeQuagga(canvas));
  }

  // ---- Cámara en vivo ----

  async function start(video, onCode, onStatus) {
    const status = (t) => { try { if (onStatus) onStatus(t); } catch (e) { /* nada */ } };
    const stream = await openCamera();
    await tuneCamera(stream);

    let stopped = false;
    let last = { text: '', at: 0 };
    let timer = 0;
    let frames = 0;

    const report = (text) => {
      if (stopped || !text) return;
      const now = Date.now();
      if (text === last.text && now - last.at < REPEAT_MS) return;
      last = { text, at: now };
      beep();
      onCode(text);
    };

    const stop = () => {
      stopped = true;
      clearTimeout(timer);
      stream.getTracks().forEach((t) => t.stop());
      video.srcObject = null;
    };

    video.setAttribute('playsinline', 'true');
    video.setAttribute('webkit-playsinline', 'true');
    video.muted = true;
    video.srcObject = stream;

    try {
      let native = null;
      if ('BarcodeDetector' in window) {
        try {
          const supported = await window.BarcodeDetector.getSupportedFormats();
          const formats = NATIVE_FORMATS.filter((f) => supported.includes(f));
          if (formats.length) native = new window.BarcodeDetector({ formats });
        } catch (e) { native = null; }
      }
      if (!native && !window.ZXing) throw new Error('No se cargó el lector de códigos (zxing.min.js).');

      await video.play();

      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d', { willReadFrequently: true });
      let lastStatus = 0;

      const tick = async () => {
        if (stopped) return;
        try {
          if (video.readyState >= 2 && video.videoWidth) {
            frames++;
            let text = '';

            if (native) {
              try {
                const codes = await native.detect(video);
                if (codes.length) text = codes[0].rawValue;
              } catch (e) { /* sigue con los demás lectores */ }
            }
            if (!text) {
              drawScaled(video, video.videoWidth, video.videoHeight, canvas, ctx);
              text = await decodeCanvas(canvas);
            }
            if (text) report(text);

            const now = Date.now();
            if (now - lastStatus > 1000) {
              lastStatus = now;
              status(`Buscando código… (${video.videoWidth}×${video.videoHeight}, ${native ? 'nativo + ' : ''}ZXing/Quagga, ${frames} fotogramas)`);
            }
          }
        } catch (e) { /* un fotograma fallido no detiene el lector */ }
        if (!stopped) timer = setTimeout(tick, 80);
      };
      tick();
    } catch (e) {
      stop();
      throw e;
    }

    return stop;
  }

  // ---- Lectura desde una foto (la cámara nativa del equipo enfoca mejor que el video en vivo) ----

  function loadImage(file) {
    return new Promise((resolve, reject) => {
      const url = URL.createObjectURL(file);
      const img = new Image();
      img.onload = () => { URL.revokeObjectURL(url); resolve(img); };
      img.onerror = () => { URL.revokeObjectURL(url); reject(new Error('No se pudo abrir la imagen.')); };
      img.src = url;
    });
  }

  async function decodeFile(file) {
    const img = await loadImage(file);
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d', { willReadFrequently: true });
    drawScaled(img, img.naturalWidth, img.naturalHeight, canvas, ctx);

    if ('BarcodeDetector' in window) {
      try {
        const supported = await window.BarcodeDetector.getSupportedFormats();
        const formats = NATIVE_FORMATS.filter((f) => supported.includes(f));
        if (formats.length) {
          const codes = await new window.BarcodeDetector({ formats }).detect(canvas);
          if (codes.length) return codes[0].rawValue;
        }
      } catch (e) { /* sigue con los demás lectores */ }
    }
    return decodeCanvas(canvas);
  }

  window.SiffaScanner = { start, decodeFile };
})();
