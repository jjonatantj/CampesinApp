/*
 * Cierre por inactividad (FA02-CU032).
 * Si el usuario no hace clic, no escribe, no se desplaza ni mueve el cursor durante el tiempo configurado
 * (data-inactividad-minutos de #cu-contexto, igual al tiempo de la sesión del servidor), se envía a /Sesion/Expirada,
 * que finaliza la sesión y lleva al inicio de sesión con el aviso "La sesión ha expirado por inactividad".
 * El sondeo de notificaciones no cuenta como actividad del usuario.
 */
(function () {
    'use strict';

    const contexto = document.getElementById('cu-contexto');
    if (!contexto || contexto.dataset.autenticado !== '1') {
        return;
    }

    const minutos = Number.parseInt(contexto.dataset.inactividadMinutos, 10);
    if (!minutos || minutos <= 0) {
        return;
    }

    const limite = minutos * 60 * 1000;
    let temporizador = null;

    function expirar() {
        window.location.href = '/Sesion/Expirada';
    }

    function reiniciar() {
        clearTimeout(temporizador);
        temporizador = setTimeout(expirar, limite);
    }

    ['click', 'keydown', 'mousemove', 'scroll', 'touchstart'].forEach((evento) => {
        document.addEventListener(evento, reiniciar, { passive: true });
    });

    reiniciar();
})();
