function abrirModalCancelar() {
    var modalElement = document.getElementById('modalCancelarAdmin');
    if (modalElement) {
        var modalCancelar = new bootstrap.Modal(modalElement);
        modalCancelar.show();
    }
}

// Redirección al perfil tras hacer clic en Aceptar en el modal de éxito
function redirigirPerfil(urlPerfil) {
    window.location.href = urlPerfil;
}
