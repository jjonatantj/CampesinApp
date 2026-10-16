/*
 * Mapas de Google Maps para los reportes de ubicación (CU-021) y la consulta de ubicación (CU-022).
 * Usa la misma clave de la API que el registro de puntos de acopio (appsettings: GoogleMaps:ApiKey), que el servidor
 * publica en <meta name="google-maps-key">. La API se carga una sola vez, la primera vez que aparece un mapa.
 *
 * Las coordenadas NO se escriben a mano: salen de la tabla punto_Acopio (columnas latitud y longitud), así que el
 * transportador ubica su reporte eligiendo un punto de acopio registrado.
 *
 * Los mapas se declaran en el HTML (también el que llega dentro de las ventanas emergentes) y se activan solos:
 *   <div data-mapa="puntos" data-select="#Lugar" data-puntos='[{"titulo":"...","lat":4.6,"lng":-74.1}]'></div>
 *       Muestra los puntos de acopio. Al elegir uno en la lista (el <select> indicado) o al hacer clic en su marcador,
 *       el mapa lo centra y lo resalta; se muestran sus coordenadas.
 *   <div data-mapa="ver" data-puntos='[{"lat":4.6,"lng":-74.1,"titulo":"...","hito":"...","fecha":"...","tipo":"reporte"}]'></div>
 *       Muestra el recorrido del pedido; el último punto "reporte" es la posición actual del transportador y un punto
 *       "destino" es el punto de acopio de entrega.
 * Si el mapa no se puede cargar (sin clave, sin internet), se avisa y las coordenadas siguen visibles como texto.
 */
(function () {
    'use strict';

    const CENTRO_COLOMBIA = { lat: 4.5709, lng: -74.2973 };
    const clave = document.querySelector('meta[name="google-maps-key"]')?.content || '';
    let carga = null;

    function cargarApi() {
        if (window.google && window.google.maps && window.google.maps.Map) return Promise.resolve();
        if (!clave) return Promise.reject(new Error('Falta la clave de Google Maps'));
        if (!carga) {
            carga = new Promise((resolver, rechazar) => {
                window.__campesinappMapasListo = resolver;
                const script = document.createElement('script');
                script.src = 'https://maps.googleapis.com/maps/api/js?key=' + encodeURIComponent(clave) +
                    '&callback=__campesinappMapasListo&loading=async';
                script.async = true;
                script.onerror = () => { carga = null; rechazar(new Error('No se pudo cargar Google Maps')); };
                document.head.appendChild(script);
            });
        }
        return carga;
    }

    /** Ejecuta la función cuando el contenedor ya es visible (los modales de Bootstrap lo muestran con animación). */
    function cuandoVisible(contenedor, funcion) {
        if (contenedor.offsetWidth > 0) { funcion(); return; }
        const modal = contenedor.closest('.modal');
        let hecho = false;
        const ejecutar = () => { if (!hecho) { hecho = true; funcion(); } };
        if (modal) modal.addEventListener('shown.bs.modal', ejecutar, { once: true });
        setTimeout(ejecutar, 700);
    }

    function avisarError(contenedor, mensaje) {
        contenedor.classList.add('mapa-error');
        contenedor.textContent = mensaje;
    }

    function leerPuntos(contenedor) {
        let puntos = [];
        try { puntos = JSON.parse(contenedor.dataset.puntos || '[]'); } catch (e) { puntos = []; }
        return puntos.filter((p) => Number.isFinite(p.lat) && Number.isFinite(p.lng));
    }

    const icono = (color, escala) => ({
        path: google.maps.SymbolPath.CIRCLE,
        scale: escala,
        fillColor: color,
        fillOpacity: 1,
        strokeColor: '#ffffff',
        strokeWeight: 2
    });

    // ------------------------------------------------------------------------------------------ elegir punto de acopio

    function iniciarPuntos(contenedor) {
        const formulario = contenedor.closest('form') || document;
        const lista = formulario.querySelector(contenedor.dataset.select);
        const estado = formulario.querySelector('[data-mapa-estado]');
        const puntos = leerPuntos(contenedor);

        if (puntos.length === 0) {
            avisarError(contenedor, 'No hay puntos de acopio registrados para mostrar en el mapa.');
            return;
        }

        const mapa = new google.maps.Map(contenedor, {
            center: CENTRO_COLOMBIA,
            zoom: 6,
            streetViewControl: false,
            mapTypeControl: false
        });

        const ventana = new google.maps.InfoWindow();
        const marcadores = new Map();
        const limites = new google.maps.LatLngBounds();

        puntos.forEach((p) => {
            const marcador = new google.maps.Marker({ map: mapa, position: { lat: p.lat, lng: p.lng }, title: p.titulo, icon: icono('#2e7d32', 9) });
            marcador.addListener('click', () => {
                if (lista) {
                    lista.value = p.titulo;
                    lista.dispatchEvent(new Event('change', { bubbles: true }));
                } else {
                    seleccionar(p.titulo, true);
                }
            });
            marcadores.set(p.titulo, { marcador, punto: p });
            limites.extend(marcador.getPosition());
        });

        function seleccionar(titulo, centrar) {
            marcadores.forEach((m) => { m.marcador.setIcon(icono('#2e7d32', 9)); m.marcador.setZIndex(1); });
            const elegido = marcadores.get(titulo);
            if (!elegido) {
                ventana.close();
                if (estado) estado.textContent = 'Elija el punto de acopio en la lista o haga clic en su marcador: el mapa muestra sus coordenadas.';
                return;
            }
            const { marcador, punto } = elegido;
            marcador.setIcon(icono('#c62828', 14));
            marcador.setZIndex(100);
            if (centrar) {
                mapa.panTo(marcador.getPosition());
                if (mapa.getZoom() < 12) mapa.setZoom(13);
            }
            const html = document.createElement('div');
            const nombre = document.createElement('strong');
            nombre.textContent = punto.titulo;
            const direccion = document.createElement('div');
            direccion.textContent = punto.direccion || '';
            html.append(nombre, direccion);
            ventana.setContent(html);
            ventana.open(mapa, marcador);
            if (estado) estado.textContent = 'Coordenadas de ' + punto.titulo + ': ' + punto.lat.toFixed(6) + ', ' + punto.lng.toFixed(6);
        }

        lista?.addEventListener('change', () => seleccionar(lista.value, true));

        cuandoVisible(contenedor, () => {
            google.maps.event.trigger(mapa, 'resize');
            if (lista && marcadores.has(lista.value)) {
                seleccionar(lista.value, true);
            } else {
                mapa.fitBounds(limites, 40);
            }
        });
    }

    // ------------------------------------------------------------------------------------------ ver recorrido

    function iniciarVista(contenedor) {
        const todos = leerPuntos(contenedor);
        const reportes = todos.filter((p) => p.tipo !== 'destino');
        const destino = todos.find((p) => p.tipo === 'destino');

        if (todos.length === 0) {
            avisarError(contenedor, 'Este reporte no tiene coordenadas registradas.');
            return;
        }

        const referencia = reportes.length ? reportes[reportes.length - 1] : destino;
        const mapa = new google.maps.Map(contenedor, {
            center: { lat: referencia.lat, lng: referencia.lng },
            zoom: 14,
            streetViewControl: false,
            mapTypeControl: false
        });

        const ventana = new google.maps.InfoWindow();
        const limites = new google.maps.LatLngBounds();

        function contenidoVentana(p, titulo) {
            const html = document.createElement('div');
            const negrita = document.createElement('strong');
            negrita.textContent = titulo;
            const detalle = document.createElement('div');
            detalle.textContent = [p.titulo, p.fecha].filter(Boolean).join(' · ');
            const coords = document.createElement('div');
            coords.textContent = p.lat.toFixed(6) + ', ' + p.lng.toFixed(6);
            html.append(negrita, detalle, coords);
            return html;
        }

        reportes.forEach((p, i) => {
            const esActual = i === reportes.length - 1;
            const marcador = new google.maps.Marker({
                map: mapa,
                position: { lat: p.lat, lng: p.lng },
                title: p.titulo,
                label: { text: String(i + 1), color: '#ffffff', fontWeight: '700' },
                icon: icono(esActual ? '#c62828' : '#2e7d32', esActual ? 15 : 11),
                zIndex: esActual ? 100 : i
            });
            marcador.addListener('click', () => {
                ventana.setContent(contenidoVentana(p, (esActual ? 'Ubicación actual · ' : '') + (p.hito || '')));
                ventana.open(mapa, marcador);
            });
            limites.extend(marcador.getPosition());
        });

        if (reportes.length > 1) {
            new google.maps.Polyline({
                map: mapa,
                path: reportes.map((p) => ({ lat: p.lat, lng: p.lng })),
                strokeColor: '#2e7d32',
                strokeOpacity: 0.8,
                strokeWeight: 4
            });
        }

        if (destino) {
            const marcador = new google.maps.Marker({
                map: mapa,
                position: { lat: destino.lat, lng: destino.lng },
                title: destino.titulo,
                icon: {
                    path: google.maps.SymbolPath.BACKWARD_CLOSED_ARROW,
                    scale: 7,
                    fillColor: '#1565c0',
                    fillOpacity: 1,
                    strokeColor: '#ffffff',
                    strokeWeight: 2
                }
            });
            marcador.addListener('click', () => {
                ventana.setContent(contenidoVentana(destino, 'Punto de acopio de entrega'));
                ventana.open(mapa, marcador);
            });
            limites.extend(marcador.getPosition());
        }

        cuandoVisible(contenedor, () => {
            google.maps.event.trigger(mapa, 'resize');
            if (todos.length > 1) {
                mapa.fitBounds(limites, 40);
            } else {
                mapa.setCenter({ lat: referencia.lat, lng: referencia.lng });
            }
        });
    }

    // ------------------------------------------------------------------------------------------ activación

    function activar(contenedor) {
        if (contenedor.dataset.mapaListo) return;
        contenedor.dataset.mapaListo = '1';
        cargarApi()
            .then(() => {
                if (contenedor.dataset.mapa === 'puntos') iniciarPuntos(contenedor);
                else iniciarVista(contenedor);
            })
            .catch(() => avisarError(contenedor, 'No se pudo cargar el mapa. Las coordenadas se muestran como texto.'));
    }

    function buscarMapas() {
        document.querySelectorAll('[data-mapa]:not([data-mapa-listo])').forEach(activar);
    }

    new MutationObserver(buscarMapas).observe(document.body, { childList: true, subtree: true });
    buscarMapas();
})();
