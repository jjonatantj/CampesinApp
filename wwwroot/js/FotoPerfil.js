
// Función para validar y mostrar la vista previa de la foto de perfil seleccionada
function mostrarVistaPrevia(event) {
    const input = event.target;
    const archivo = input.files && input.files[0];
    const output = document.getElementById('previewFoto');
    const placeholder = document.getElementById('placeholderText');
    const spanError = document.getElementById('errorFotoPerfil') || document.querySelector('[data-valmsg-for="FotoPerfil"]');

    function limpiarVistaPrevia(mensajeError) {
        input.value = '';
        if (output) {
            output.src = '#';
            output.style.display = 'none';
        }
        if (placeholder) {
            placeholder.style.display = 'block';
        }
        if (spanError) {
            spanError.textContent = mensajeError || '';
        }
    }

    if (!archivo) {
        limpiarVistaPrevia('');
        return;
    }

    const extensionesPermitidas = ['.jpg', '.jpeg', '.png', '.webp'];
    const mimesPermitidos = ['image/jpeg', 'image/png', 'image/webp', 'image/pjpeg', 'image/x-png'];
    const nombreArchivo = archivo.name.toLowerCase();
    const tieneExtensionValida = extensionesPermitidas.some(ext => nombreArchivo.endsWith(ext));
    const tieneMimeValido = mimesPermitidos.includes((archivo.type || '').toLowerCase());

    if (!tieneExtensionValida || !tieneMimeValido) {
        limpiarVistaPrevia('Formato de archivo no válido. Solo se permiten imágenes JPG, JPEG, PNG o WebP.');
        return;
    }

    const tamanoMaximoBytes = 5 * 1024 * 1024; // 5 MB
    if (archivo.size > tamanoMaximoBytes) {
        limpiarVistaPrevia('La imagen de perfil no debe superar el tamaño máximo de 5 MB.');
        return;
    }

    if (spanError) {
        spanError.textContent = '';
    }

    const reader = new FileReader();
    reader.onload = function () {
        if (output) {
            output.src = reader.result;
            output.style.display = 'block';
        }
        if (placeholder) {
            placeholder.style.display = 'none';
        }
    };
    reader.readAsDataURL(archivo);
}