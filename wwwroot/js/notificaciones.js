/*
 * Notificaciones en vivo. Cada pocos segundos el navegador pregunta al servidor (/Notificaciones/Sondeo) por:
 *   - el contador de la campana,
 *   - las notificaciones nuevas, que aparecen como una tarjeta flotante en la esquina (se ve en cualquier pantalla),
 *   - las ventanas emergentes pendientes (llegada del pedido, entrega completada),
 *   - y, para el transportador, la alerta obligatoria de reporte de ubicación cada 2 horas (RN05-CU021).
 * La última notificación que ya vio el navegador se guarda en localStorage (por usuario) para no repetir tarjetas
 * al cambiar de pantalla ni al abrir otra pestaña.
 */
(function () {
    'use strict';

    const contexto = document.getElementById('cu-contexto');
    if (contexto?.dataset.autenticado !== '1' || !window.CampesinApp) {
        return;
    }

    const INTERVALO_MS = 6000;
    const DURACION_TARJETA_MS = 10000;
    const MAXIMO_TARJETAS = 3;
    const contador = document.getElementById('campanaContador');
    const clave = 'cp_ultima_notif_' + (contexto.dataset.usuario || '0');

    // Icono, color y título de cada tipo (el mismo criterio que VistaHelpers.DatosNotificacion en el servidor).
    const TIPOS = {
        Ubicacion: ['bi-geo-alt-fill', 'nt-verde', 'Ubicación actualizada'],
        Llegada: ['bi-truck', 'nt-azul', 'Su pedido llegó'],
        EntregaCompletada: ['bi-check-circle-fill', 'nt-verde', 'Entrega completada'],
        Novedad: ['bi-exclamation-triangle-fill', 'nt-amarillo', 'Novedad en la entrega'],
        Asignacion: ['bi-signpost-split-fill', 'nt-azul', 'Transporte asignado'],
        EntregaPendiente: ['bi-hourglass-split', 'nt-naranja', 'Falta una confirmación'],
        Calificacion: ['bi-star-fill', 'nt-amarillo', 'Nueva calificación']
    };

    let ultimoVisto = null;
    try {
        const guardado = localStorage.getItem(clave);
        if (guardado !== null) ultimoVisto = parseInt(guardado, 10);
    } catch (e) { /* sin almacenamiento: se usa solo la memoria de esta pantalla */ }

    function recordar(id) {
        ultimoVisto = id;
        try { localStorage.setItem(clave, String(id)); } catch (e) { /* ignorado */ }
    }

    function actualizarContador(noLeidas) {
        if (!contador) return;
        contador.textContent = noLeidas > 99 ? '99+' : String(noLeidas);
        contador.classList.toggle('d-none', noLeidas === 0);
        contador.classList.remove('pulso');
        if (noLeidas > 0) {
            void contador.offsetWidth; // reinicia la animación
            contador.classList.add('pulso');
        }
    }

    function contenedorTarjetas() {
        let c = document.getElementById('cpTarjetas');
        if (!c) {
            c = document.createElement('div');
            c.id = 'cpTarjetas';
            c.className = 'cp-tarjetas';
            c.setAttribute('aria-live', 'polite');
            document.body.appendChild(c);
        }
        return c;
    }

    function texto(valor) {
        const nodo = document.createElement('span');
        nodo.textContent = valor;
        return nodo.innerHTML;
    }

    function mostrarTarjeta(n) {
        const [icono, clase, titulo] = TIPOS[n.tipo] || ['bi-bell-fill', 'nt-verde', 'Notificación'];
        const contenedor = contenedorTarjetas();
        while (contenedor.children.length >= MAXIMO_TARJETAS) {
            contenedor.firstElementChild.remove();
        }

        const tarjeta = document.createElement('div');
        tarjeta.className = 'cp-tarjeta ' + clase;
        tarjeta.setAttribute('role', 'status');
        tarjeta.innerHTML =
            '<span class="cp-tarjeta-icono"><i class="bi ' + icono + '"></i></span>' +
            '<div class="cp-tarjeta-cuerpo">' +
            '<div class="cp-tarjeta-titulo">' + texto(titulo) + '</div>' +
            '<div class="cp-tarjeta-mensaje">' + texto(n.mensaje) + '</div>' +
            '<div class="cp-tarjeta-pie"><i class="bi bi-clock"></i> ' + texto(n.cuando || 'ahora') + ' · <u>Ver notificaciones</u></div>' +
            '</div>' +
            '<button type="button" class="cp-tarjeta-cerrar" aria-label="Cerrar">&times;</button>' +
            '<span class="cp-tarjeta-barra" style="animation-duration:' + DURACION_TARJETA_MS + 'ms"></span>';

        const quitar = () => {
            tarjeta.classList.add('saliendo');
            setTimeout(() => tarjeta.remove(), 250);
        };
        tarjeta.querySelector('.cp-tarjeta-cerrar').addEventListener('click', (evento) => {
            evento.stopPropagation();
            quitar();
        });
        tarjeta.addEventListener('click', () => {
            quitar();
            window.CampesinApp.abrir('/Notificaciones/Lista', 'principal');
        });
        contenedor.appendChild(tarjeta);
        setTimeout(quitar, DURACION_TARJETA_MS);
    }

    async function sondear() {
        if (document.hidden) return;

        try {
            const url = '/Notificaciones/Sondeo' + (ultimoVisto !== null ? '?ultimo=' + ultimoVisto : '');
            const respuesta = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'same-origin',
                cache: 'no-store'
            });
            if (!respuesta.ok) return;

            const datos = await respuesta.json();
            const habiaNuevas = ultimoVisto !== null && datos.noLeidas > 0 && datos.nuevas.length > 0;
            actualizarContador(datos.noLeidas);

            // Con la primera consulta (sin referencia) solo se fija desde dónde contar; las tarjetas son para lo que llegue después.
            if (datos.ultimoId > (ultimoVisto ?? -1)) recordar(datos.ultimoId);
            if (habiaNuevas) datos.nuevas.forEach(mostrarTarjeta);

            // No se interrumpe a quien está llenando un formulario o leyendo una ventana.
            if (window.CampesinApp.estaAbierto('principal') || window.CampesinApp.estaAbierto('superpuesta')) return;

            // En la pantalla del reporte ya está registrando el avance: la alerta no se repite encima.
            const enPantallaReporte = window.location.pathname.toLowerCase().startsWith('/ubicacion/reportar');

            if (datos.reporteVencido && !enPantallaReporte) {
                await window.CampesinApp.abrir('/Ubicacion/AlertaVencida', 'superpuesta');
            } else if (datos.hayPopup) {
                await window.CampesinApp.abrir('/Notificaciones/Popup', 'superpuesta');
            }
        } catch (error) {
            // Sin conexión: se vuelve a intentar en el siguiente ciclo.
        }
    }

    sondear();
    setInterval(sondear, INTERVALO_MS);
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) sondear();
    });
})();
