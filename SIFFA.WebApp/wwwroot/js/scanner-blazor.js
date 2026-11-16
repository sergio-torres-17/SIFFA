// Puente entre scanner.js (cámara) y los componentes Blazor.
// Cada componente se identifica con un id: así, al cerrarse un diálogo, no apaga la cámara que abrió el siguiente.
window.siffaScan = (() => {
    let stop = null;
    let owner = null;

    return {
        // Devuelve null si la cámara abrió bien, o el texto del error.
        start: async (video, dotnetRef, id) => {
            try {
                if (stop) { stop(); stop = null; owner = null; }
                stop = await SiffaScanner.start(
                    video,
                    (text) => dotnetRef.invokeMethodAsync('OnScanned', text),
                    (state) => dotnetRef.invokeMethodAsync('OnStatus', state).catch(() => { }));
                owner = id;
                return null;
            } catch (e) {
                return e && e.message ? e.message : 'No se pudo abrir la cámara.';
            }
        },
        stop: (id) => {
            if (stop && owner === id) { stop(); stop = null; owner = null; }
        },
        // Lectura desde una foto tomada con la cámara nativa del equipo.
        bindPhoto: (input, dotnetRef) => {
            if (input.dataset.bound) return;
            input.dataset.bound = '1';
            input.addEventListener('change', async () => {
                const file = input.files && input.files[0];
                input.value = '';
                if (!file) return;
                try {
                    const text = await SiffaScanner.decodeFile(file);
                    if (text) await dotnetRef.invokeMethodAsync('OnScanned', text);
                    else await dotnetRef.invokeMethodAsync('OnPhotoFailed', 'No se encontró ningún código en la foto. Acércate y que se vea completo y enfocado.');
                } catch (e) {
                    await dotnetRef.invokeMethodAsync('OnPhotoFailed', e && e.message ? e.message : 'No se pudo leer la foto.');
                }
            });
        },
    };
})();
