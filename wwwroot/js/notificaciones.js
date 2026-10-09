/*
 * Sondeo de notificaciones: cada pocos segundos pregunta al servidor por el contador de la campana, por ventanas
 * emergentes pendientes (llegada del pedido, entrega completada) y, para el transportador, por la alerta
 * obligatoria de reporte de ubicación cada 2 horas (RN05-CU021).
 */
(function () {
    'use strict';

    if (document.getElementById('cu-contexto')?.dataset.autenticado !== '1' || !window.CampesinApp) {
        return;
    }

    const INTERVALO_MS = 15000;
    const contador = document.getElementById('campanaContador');

    function actualizarContador(noLeidas) {
        if (!contador) return;
        contador.textContent = noLeidas > 99 ? '99+' : String(noLeidas);
        contador.classList.toggle('d-none', noLeidas === 0);
    }

    async function sondear() {
        if (document.hidden) return;

        try {
            const respuesta = await fetch('/Notificaciones/Sondeo', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'same-origin'
            });
            if (!respuesta.ok) return;

            const datos = await respuesta.json();
            actualizarContador(datos.noLeidas);

            // No se interrumpe a quien está llenando un formulario o leyendo una ventana.
            if (window.CampesinApp.estaAbierto('principal') || window.CampesinApp.estaAbierto('superpuesta')) return;

            if (datos.reporteVencido) {
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
