// Menú lateral (hamburguesa). Funciona con JavaScript puro para no depender del circuito de Blazor,
// que en una tablet puede tardar o no conectar. El estado vive en la clase "siffa-nav-open" de <html>.
(function () {
    var root = document.documentElement;
    var WIDE = 960; // igual que el punto de corte "md" de MudBlazor

    function isWide() { return window.innerWidth >= WIDE; }
    function setOpen(open) { root.classList.toggle('siffa-nav-open', open); }

    // Estado inicial: abierto en pantallas anchas, cerrado en angostas.
    setOpen(isWide());

    // Al girar la tablet o cambiar el tamaño, se adapta solo si cruza el punto de corte.
    var wasWide = isWide();
    window.addEventListener('resize', function () {
        var wide = isWide();
        if (wide !== wasWide) { wasWide = wide; setOpen(wide); }
    });

    document.addEventListener('click', function (e) {
        var t = e.target;
        if (!(t instanceof Element)) return;

        if (t.closest('[data-siffa-nav-toggle]')) {
            setOpen(!root.classList.contains('siffa-nav-open'));
            return;
        }
        // En pantallas angostas el menú es un panel: se cierra al tocar un enlace o el fondo oscuro.
        if (!isWide() && root.classList.contains('siffa-nav-open') &&
            (t.closest('.siffa-nav a') || t.closest('.siffa-nav-backdrop'))) {
            setOpen(false);
        }
    });
})();
