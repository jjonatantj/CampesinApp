// Abrir modal para solicitar confirmación de eliminación de cuenta
function abrirModalConfirmarEliminar() {
    var codigoInput = document.getElementById('txtCodigoVerificacion');
    if (codigoInput && codigoInput.value.trim() === "") {
        alert("Por favor ingrese el código de verificación enviado a su correo.");
        return;
    }

    var modalElement = document.getElementById('modalConfirmarEliminacion');
    if (modalElement) {
        var modal = new bootstrap.Modal(modalElement);
        modal.show();
    }
}

// Abrir modal para cancelar proceso
function abrirModalCancelarEliminar() {
    var modalElement = document.getElementById('modalCancelarEliminar');
    if (modalElement) {
        var modal = new bootstrap.Modal(modalElement);
        modal.show();
    }
}

// Redireccionar a la pantalla de Perfil
function redirigirPerfil(urlPerfil) {
    window.location.href = urlPerfil;
}