/*
 * Service worker de CampesinApp.
 *
 * Solo hace una cosa: cuando el navegador no logra abrir una pantalla de CampesinApp (sin internet, servidor apagado o
 * conexión demasiado lenta), muestra la pantalla de CampesinApp (/offline.html) en lugar de la del navegador.
 * No guarda páginas ni datos de la aplicación: todo lo demás va directo a la red, como siempre.
 *
 * Funciona en https y en http://localhost (requisito de los service workers). Se registra desde wwwroot/js/conexion.js.
 */
const VERSION = 'v2';
const CACHE = 'campesinapp-sin-conexion-' + VERSION;
const TIEMPO_MAXIMO_MS = 45000;

const ARCHIVOS = [
    '/offline.html',
    '/css/campesinapp-error.css',
    '/fonts/mulish-latin.woff2',
    '/fonts/mulish-latin-ext.woff2',
    '/images/logo.jpeg',
    '/favicon.ico',
    '/images/error/fondo-ondas.svg',
    '/images/error/paisaje.svg',
    '/images/error/ilustracion-wifi.svg',
    '/images/error/icono-globo.svg',
    '/images/error/icono-servidor.svg'
];

self.addEventListener('install', (evento) => {
    evento.waitUntil(
        caches.open(CACHE)
            .then((cache) => cache.addAll(ARCHIVOS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', (evento) => {
    evento.waitUntil(
        caches.keys()
            .then((nombres) => Promise.all(nombres.filter((n) => n.startsWith('campesinapp-sin-conexion-') && n !== CACHE).map((n) => caches.delete(n))))
            .then(() => self.clients.claim())
    );
});

/** Pantalla de "sin conexión" con el motivo indicado (lenta | servidor | sin-internet). */
async function pantallaSinConexion(motivo) {
    const cache = await caches.open(CACHE);
    const respuesta = await cache.match('/offline.html');
    if (!respuesta) {
        return new Response('Sin conexión', { status: 503, headers: { 'Content-Type': 'text/plain; charset=utf-8' } });
    }

    const html = (await respuesta.text()).replace('<!--MOTIVO-->', '<script>window.__motivo = "' + motivo + '";</script>');
    return new Response(html, { status: 200, headers: { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store' } });
}

async function navegar(peticion) {
    const control = new AbortController();
    const temporizador = setTimeout(() => control.abort(), TIEMPO_MAXIMO_MS);
    try {
        const respuesta = await fetch(peticion, { signal: control.signal });
        clearTimeout(temporizador);
        return respuesta;
    } catch (error) {
        clearTimeout(temporizador);
        if (error && error.name === 'AbortError') return pantallaSinConexion('lenta');
        return pantallaSinConexion(self.navigator.onLine === false ? 'sin-internet' : 'servidor');
    }
}

self.addEventListener('fetch', (evento) => {
    const peticion = evento.request;
    if (peticion.method !== 'GET') return;

    const url = new URL(peticion.url);
    if (url.origin !== self.location.origin) return;

    // Los archivos de la pantalla de "sin conexión" se sirven del caché (con la red como respaldo).
    if (ARCHIVOS.includes(url.pathname) && peticion.mode !== 'navigate') {
        evento.respondWith(caches.match(url.pathname).then((guardado) => guardado || fetch(peticion)));
        return;
    }

    if (peticion.mode === 'navigate') {
        evento.respondWith(navegar(peticion));
    }
});
