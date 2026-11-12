
// funcion para mostrar la vista previa de la foto de perfil seleccionada
function mostrarVistaPrevia(event) {
    const reader = new FileReader();
    reader.onload = function() {
        const output = document.getElementById('previewFoto');
        output.src = reader.result;
        output.style.display = 'block';
        document.getElementById('placeholderText').style.display = 'none';
    };
    if (event.target.files[0]) {
        reader.readAsDataURL(event.target.files[0]);
    }
}