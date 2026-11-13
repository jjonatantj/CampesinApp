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
        const g = window.google && window.google.maps;
        if (g && g.Map && g.Marker && g.Geocoder) return Promise.resolve();
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
            }).then(() => {
                // Con loading=async las clases (Map, Marker, Geocoder) solo existen después de importar sus librerías.
                const gm = window.google.maps;
                if (typeof gm.importLibrary !== 'function') return null;
                return Promise.all(['maps', 'marker', 'geocoding'].map((nombre) => gm.importLibrary(nombre)));
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

    // ------------------------------------------------------------------------------------------ reporte de ubicación

    /**
     * Mapa del formulario "Reporte de Ubicación": dibuja los puntos de acopio y, según el usuario va eligiendo, el municipio,
     * el origen, el destino y el lugar actual. Las coordenadas salen de punto_Acopio (y del municipio, si algún punto
     * de acopio está en él; si no, Google lo geolocaliza por su nombre), así que nadie las escribe.
     */
    function iniciarReporte(contenedor) {
        const formulario = contenedor.closest('form') || document;
        const campo = (selector) => (selector ? formulario.querySelector(selector) : null);
        const listas = {
            municipio: campo(contenedor.dataset.municipio),
            origen: campo(contenedor.dataset.origen),
            destino: campo(contenedor.dataset.destino),
            lugar: campo(contenedor.dataset.select)
        };
        const estado = formulario.querySelector('[data-mapa-estado]');
        const textoInicial = estado ? estado.textContent : '';
        const puntos = leerPuntos(contenedor);
        let municipios = [];
        try { municipios = JSON.parse(contenedor.dataset.municipios || '[]'); } catch (e) { municipios = []; }

        const mapa = new google.maps.Map(contenedor, { center: CENTRO_COLOMBIA, zoom: 6, streetViewControl: false, mapTypeControl: false });
        const geocodificador = new google.maps.Geocoder();
        const cacheMunicipios = new Map();
        const ventana = new google.maps.InfoWindow();
        const porNombre = new Map(puntos.map((p) => [p.titulo, p]));

        // Por carretera: tocar el mapa marca dónde va el camión (el navegador a veces no entrega la ubicación).
        mapa.addListener('click', (evento) => {
            if (listas.lugar?.value !== 'En carretera' || !evento.latLng) return;
            const lat = formulario.querySelector('#Latitud');
            const lng = formulario.querySelector('#Longitud');
            if (!lat || !lng) return;
            lat.value = evento.latLng.lat().toFixed(6);
            lng.value = evento.latLng.lng().toFixed(6);
            listas.lugar.dispatchEvent(new Event('change', { bubbles: true }));
        });

        // Marcadores de fondo: todos los puntos de acopio registrados.
        puntos.forEach((p) => {
            const marcador = new google.maps.Marker({ map: mapa, position: { lat: p.lat, lng: p.lng }, title: p.titulo, icon: icono('#9e9e9e', 6), zIndex: 1 });
            marcador.addListener('click', () => {
                if (listas.lugar) {
                    listas.lugar.value = p.titulo;
                    listas.lugar.dispatchEvent(new Event('change', { bubbles: true }));
                }
            });
        });

        const etiquetas = {
            municipio: { texto: 'M', color: '#ef6c00', nombre: 'Municipio' },
            origen: { texto: 'O', color: '#2e7d32', nombre: 'Origen' },
            destino: { texto: 'D', color: '#1565c0', nombre: 'Destino' },
            lugar: { texto: 'A', color: '#c62828', nombre: 'Ubicación actual' }
        };
        const claves = ['municipio', 'origen', 'destino', 'lugar'];
        const marcadores = {};
        let linea = null;

        function posicionMunicipio(nombre) {
            const m = municipios.find((x) => x.titulo === nombre);
            if (!m) return Promise.resolve(null);
            if (Number.isFinite(m.lat) && Number.isFinite(m.lng)) return Promise.resolve({ lat: m.lat, lng: m.lng });
            if (cacheMunicipios.has(nombre)) return Promise.resolve(cacheMunicipios.get(nombre));
            return new Promise((resolver) => {
                geocodificador.geocode({ address: m.titulo + ', ' + m.departamento + ', Colombia' }, (resultados, estadoGeo) => {
                    const ok = estadoGeo === 'OK' && resultados && resultados[0];
                    const pos = ok ? { lat: resultados[0].geometry.location.lat(), lng: resultados[0].geometry.location.lng() } : null;
                    cacheMunicipios.set(nombre, pos);
                    resolver(pos);
                });
            });
        }

        async function posicion(clave) {
            const valor = listas[clave]?.value;
            if (!valor) return null;
            if (clave === 'municipio') return posicionMunicipio(valor);
            if (clave === 'lugar' && valor === 'En carretera') {
                const lat = Number.parseFloat(formulario.querySelector('#Latitud')?.value);
                const lng = Number.parseFloat(formulario.querySelector('#Longitud')?.value);
                return Number.isFinite(lat) && Number.isFinite(lng) ? { lat, lng } : null;
            }
            const p = porNombre.get(valor);
            return p ? { lat: p.lat, lng: p.lng } : null;
        }

        async function actualizar(centrar) {
            const posiciones = await Promise.all(claves.map(posicion));
            const limites = new google.maps.LatLngBounds();
            const lineas = [];
            let hayPuntos = false;

            claves.forEach((clave, i) => {
                const pos = posiciones[i];
                const info = etiquetas[clave];
                marcadores[clave]?.setMap(null);
                marcadores[clave] = null;
                if (!listas[clave] || !listas[clave].value) return;
                if (!pos) {
                    lineas.push(info.nombre + ': ' + listas[clave].value + ' (coordenadas no disponibles)');
                    return;
                }
                const marcador = new google.maps.Marker({
                    map: mapa,
                    position: pos,
                    title: info.nombre + ': ' + listas[clave].value,
                    label: { text: info.texto, color: '#ffffff', fontWeight: '700' },
                    icon: icono(info.color, 14),
                    zIndex: 100
                });
                marcador.addListener('click', () => {
                    const html = document.createElement('div');
                    const negrita = document.createElement('strong');
                    negrita.textContent = clave === 'lugar' && listas.lugar.value === 'En carretera'
                        ? 'Va por carretera' + (listas.destino?.value ? ' rumbo a ' + listas.destino.value : '')
                        : info.nombre + ': ' + listas[clave].value;
                    const coords = document.createElement('div');
                    coords.textContent = pos.lat.toFixed(6) + ', ' + pos.lng.toFixed(6);
                    html.append(negrita, coords);
                    ventana.setContent(html);
                    ventana.open(mapa, marcador);
                });
                marcadores[clave] = marcador;
                limites.extend(pos);
                hayPuntos = true;
                const enCarretera = clave === 'lugar' && listas.lugar.value === 'En carretera';
                lineas.push(enCarretera
                    ? 'Va por carretera (' + pos.lat.toFixed(6) + ', ' + pos.lng.toFixed(6) + ')' + (listas.destino?.value ? ' rumbo a ' + listas.destino.value : '')
                    : info.nombre + ': ' + listas[clave].value + ' (' + pos.lat.toFixed(6) + ', ' + pos.lng.toFixed(6) + ')');
            });

            // Ruta prevista: origen → ubicación actual → destino.
            const ruta = ['origen', 'lugar', 'destino'].map((c) => posiciones[claves.indexOf(c)]).filter(Boolean);
            linea?.setMap(null);
            linea = ruta.length > 1
                ? new google.maps.Polyline({ map: mapa, path: ruta, strokeColor: '#2e7d32', strokeOpacity: 0.8, strokeWeight: 4 })
                : null;

            if (estado) {
                estado.replaceChildren();
                if (lineas.length === 0) {
                    estado.textContent = textoInicial;
                } else {
                    lineas.forEach((texto) => { const d = document.createElement('div'); d.textContent = texto; estado.appendChild(d); });
                }
            }

            if (centrar && hayPuntos) {
                if (limites.getNorthEast().equals(limites.getSouthWest())) {
                    mapa.setCenter(limites.getCenter());
                    mapa.setZoom(13);
                } else {
                    mapa.fitBounds(limites, 50);
                }
            }
        }

        Object.values(listas).forEach((lista) => lista?.addEventListener('change', () => actualizar(true)));

        cuandoVisible(contenedor, () => {
            google.maps.event.trigger(mapa, 'resize');
            if (Object.values(listas).some((l) => l && l.value)) {
                actualizar(true);
            } else if (puntos.length) {
                const limites = new google.maps.LatLngBounds();
                puntos.forEach((p) => limites.extend({ lat: p.lat, lng: p.lng }));
                mapa.fitBounds(limites, 40);
            }
        });
    }

    // La región filtra los municipios del desplegable de municipio (cada <option> trae su región en data-region).
    function filtrarMunicipios(filtro) {
        const lista = (filtro.closest('form') || document).querySelector(filtro.dataset.filtroMunicipios);
        if (!lista) return;
        Array.from(lista.options).forEach((opcion) => {
            if (!opcion.value) return;
            const visible = !filtro.value || opcion.dataset.region === filtro.value;
            opcion.hidden = !visible;
            opcion.disabled = !visible;
        });
        if (lista.value && lista.selectedOptions[0]?.disabled) {
            lista.value = '';
            lista.dispatchEvent(new Event('change', { bubbles: true }));
        }
    }

    document.addEventListener('change', (evento) => {
        if (evento.target.matches?.('[data-filtro-municipios]')) filtrarMunicipios(evento.target);
    });

    // ------------------------------------------------------------------------------------------ formulario del reporte

    /**
     * Reglas del formulario "Reporte de Ubicación" que no dependen de Google Maps:
     *  - las fechas, el origen y el destino salen del viaje elegido y no se escriben (origen y destino quedan bloqueados);
     *  - todo lo demás se elige libremente: el lugar (lista o mapa), el tipo de reporte y "En carretera";
     *  - "En carretera" toma las coordenadas del navegador o las que se marquen tocando el mapa.
     */
    function configurarFormularioReporte(formulario) {
        if (formulario.dataset.reporteListo) return;
        formulario.dataset.reporteListo = '1';

        const campo = (id) => formulario.querySelector('#' + id);
        const viaje = campo('IdViaje');
        const origen = campo('PuntoOrigen');
        const destino = campo('PuntoDestino');
        const lugar = campo('Lugar');
        const tipo = campo('Hito');
        const latitud = campo('Latitud');
        const longitud = campo('Longitud');
        const aviso = formulario.querySelector('[data-aviso-carretera]');
        const textoAviso = aviso ? aviso.textContent : '';
        const CARRETERA = 'En carretera';
        const SALIDA = 'Salida';

        let viajes = [];
        try { viajes = JSON.parse(viaje?.dataset.viajes || '[]'); } catch (e) { viajes = []; }

        function bloquear(lista, valor) {
            if (!lista) return;
            let espejo = lista.parentElement.querySelector('input[data-espejo="' + lista.id + '"]');
            if (valor == null) {
                lista.disabled = false;
                espejo?.remove();
                return;
            }
            lista.value = valor;
            lista.disabled = true;
            if (!espejo) {
                espejo = document.createElement('input');
                espejo.type = 'hidden';
                espejo.name = lista.name;
                espejo.dataset.espejo = lista.id;
                lista.parentElement.appendChild(espejo);
            }
            espejo.value = lista.value;
        }

        function avisar(texto) { if (aviso) aviso.textContent = texto || textoAviso; }

        function cambiar(lista) { lista?.dispatchEvent(new Event('change', { bubbles: true })); }

        // Garantiza que el valor exista como opción (el destino puede ser una dirección y no un punto de acopio) y lo bloquea.
        function fijar(lista, valor) {
            if (!lista) return;
            if (valor && !Array.from(lista.options).some((o) => o.value === valor)) {
                lista.add(new Option(valor, valor));
            }
            bloquear(lista, valor || null);
        }

        const fechaInicial = campo('FechaInicialVista');
        const fechaFinal = campo('FechaFinalVista');
        const bloqueRetraso = campo('bloqueRetraso');
        const textoRetraso = campo('textoRetraso');
        const fechaActual = campo('FechaActualVista');

        // Fechas del viaje: la inicial es el día en que se asignó; la final, la propuesta al asignarlo.
        // Si ya se pasó de la final, aparece además la fecha actual.
        function mostrarFechas(actual) {
            if (fechaInicial) fechaInicial.value = actual ? actual.fechaInicial : '';
            if (fechaFinal) fechaFinal.value = actual ? actual.fechaFinal : '';
            const retrasado = !!(actual && actual.retrasado);
            bloqueRetraso?.classList.toggle('d-none', !retrasado);
            if (retrasado) {
                if (fechaActual) fechaActual.value = actual.hoy;
                if (textoRetraso) {
                    const dias = actual.diasRetraso;
                    textoRetraso.textContent = 'El viaje ya pasó la fecha final propuesta (' + dias + (dias === 1 ? ' día' : ' días') + ' de retraso).';
                }
            }
        }

        function aplicarViaje() {
            const actual = viajes.find((v) => String(v.id) === viaje?.value);
            const tiene = !!(actual && actual.tieneReportes);
            mostrarFechas(actual);
            // Origen y destino salen siempre del viaje; solo quedan libres si el pedido no los tiene definidos.
            if (actual) {
                fijar(origen, actual.origen);
                fijar(destino, actual.destino);
            } else {
                // Sin viaje elegido no hay origen ni destino que mostrar.
                [origen, destino].forEach((lista) => {
                    if (!lista) return;
                    bloquear(lista, null);
                    lista.value = '';
                    lista.disabled = true;
                });
            }
            // Todos los tipos de reporte se pueden elegir; si el viaje aún no tiene reportes se sugiere "Salida".
            if (tipo && actual && !tiene && !tipo.value) tipo.value = SALIDA;
            cambiar(origen);
        }

        function ubicar() {
            if (!navigator.geolocation) {
                avisar('Su navegador no permite obtener la ubicación actual. Toque en el mapa el lugar de la carretera donde va.');
                cambiar(lugar);
                return;
            }
            avisar('Obteniendo su ubicación actual…');
            navigator.geolocation.getCurrentPosition(
                (posicion) => {
                    latitud.value = posicion.coords.latitude.toFixed(6);
                    longitud.value = posicion.coords.longitude.toFixed(6);
                    avisar('Ubicación en carretera: ' + latitud.value + ', ' + longitud.value);
                    cambiar(lugar);
                },
                () => {
                    // Sin GPS no se pierde la opción: se marca dónde va tocando el mapa.
                    avisar('No se pudo obtener su ubicación automática. Toque en el mapa el lugar de la carretera donde va.');
                    cambiar(lugar);
                },
                { enableHighAccuracy: true, timeout: 15000 });
        }

        viaje?.addEventListener('change', aplicarViaje);
        lugar?.addEventListener('change', () => {
            if (lugar.value === CARRETERA) {
                if (!latitud.value) ubicar();
            } else {
                latitud.value = '';
                longitud.value = '';
                avisar('');
            }
        });

        const filtro = formulario.querySelector('[data-filtro-municipios]');
        if (filtro) filtrarMunicipios(filtro);
        aplicarViaje();
    }

    // ------------------------------------------------------------------------------------------ activación

    function activar(contenedor) {
        if (contenedor.dataset.mapaListo) return;
        contenedor.dataset.mapaListo = '1';
        cargarApi()
            .then(() => {
                if (contenedor.dataset.mapa === 'reporte') iniciarReporte(contenedor);
                else if (contenedor.dataset.mapa === 'puntos') iniciarPuntos(contenedor);
                else iniciarVista(contenedor);
            })
            .catch((error) => {
                console.error('Mapa:', error);
                avisarError(contenedor, 'No se pudo cargar el mapa. Las coordenadas se muestran como texto.');
            });
    }

    function buscarMapas() {
        document.querySelectorAll('form[data-reporte-form]:not([data-reporte-listo])').forEach(configurarFormularioReporte);
        document.querySelectorAll('[data-mapa]:not([data-mapa-listo])').forEach(activar);
    }

    new MutationObserver(buscarMapas).observe(document.body, { childList: true, subtree: true });
    buscarMapas();
})();
