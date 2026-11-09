/*
 * Estado de la conexión mientras el usuario está dentro de CampesinApp.
 *  - Registra el service worker (/sw.js) que muestra la pantalla de CampesinApp cuando una página no se puede abrir.
 *  - Avisa con una franja en la parte superior cuando se pierde internet, cuando vuelve y cuando la conexión es lenta.
 * Es independiente de Bootstrap y del resto de scripts: puede cargarse en cualquier pantalla (con o sin sesión).
 */
(function () {
    'use strict';

    if ('serviceWorker' in navigator && (location.protocol === 'https:' || location.hostname === 'localhost' || location.hostname === '127.0.0.1')) {
        window.addEventListener('load', () => {
            navigator.serviceWorker.register('/sw.js').catch(() => { /* sin service worker la app funciona igual */ });
        });
    }

    const estilos = document.createElement('style');
    estilos.textContent =
        '.cp-conexion{position:fixed;top:0;left:0;right:0;z-index:3000;display:flex;align-items:center;justify-content:center;gap:.6rem;' +
        'padding:.55rem 1rem;font:700 .9rem/1.3 "Mulish","Segoe UI",system-ui,sans-serif;color:#fff;text-align:center;' +
        'box-shadow:0 4px 14px rgba(0,0,0,.25);transform:translateY(-110%);transition:transform .3s ease}' +
        '.cp-conexion.visible{transform:none}' +
        '.cp-conexion.sin-internet{background:#c62828}.cp-conexion.lenta{background:#ef6c00}.cp-conexion.restablecida{background:#2e7d32}';
    document.head.appendChild(estilos);

    const franja = document.createElement('div');
    franja.className = 'cp-conexion';
    franja.setAttribute('role', 'status');
    franja.setAttribute('aria-live', 'polite');
    document.body.appendChild(franja);

    let temporizador = null;

    function mostrar(clase, texto, ocultarEn) {
        clearTimeout(temporizador);
        franja.className = 'cp-conexion visible ' + clase;
        franja.textContent = texto;
        if (ocultarEn) temporizador = setTimeout(ocultar, ocultarEn);
    }

    function ocultar() {
        franja.classList.remove('visible');
    }

    function revisarVelocidad() {
        if (!navigator.onLine) return;
        const c = navigator.connection;
        if (c && (c.effectiveType === 'slow-2g' || c.effectiveType === '2g' || c.rtt > 1500)) {
            mostrar('lenta', 'Su conexión a internet es lenta: algunas pantallas pueden tardar más de lo normal.');
        } else if (franja.classList.contains('lenta')) {
            ocultar();
        }
    }

    window.addEventListener('offline', () => mostrar('sin-internet', 'Sin conexión a internet. Revise su red: cuando vuelva, CampesinApp seguirá funcionando.'));
    window.addEventListener('online', () => {
        mostrar('restablecida', 'Conexión restablecida.', 3500);
        setTimeout(revisarVelocidad, 3800);
    });
    if (navigator.connection) navigator.connection.addEventListener('change', revisarVelocidad);

    if (!navigator.onLine) {
        mostrar('sin-internet', 'Sin conexión a internet. Revise su red: cuando vuelva, CampesinApp seguirá funcionando.');
    } else {
        revisarVelocidad();
    }
})();
