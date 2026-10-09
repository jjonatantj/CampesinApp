// Lógica para el temporizador y validación de códigos de verificación

document.addEventListener("DOMContentLoaded", function () {
    // Lógica del temporizador de 4:59
    let tiempoTotal = 4 * 60 + 59;
    const elementoTiempo = document.getElementById("tiempoRestante");
    const contenedorTemp = document.getElementById("temporizadorContainer");
    const btnFinalizar = document.getElementById("btnFinalizar");

    if (elementoTiempo && contenedorTemp && btnFinalizar) {
        const cuentaRegresiva = setInterval(() => {
            let minutos = Math.floor(tiempoTotal / 60);
            let segundos = tiempoTotal % 60;

            minutos = minutos < 10 ? "0" + minutos : minutos;
            segundos = segundos < 10 ? "0" + segundos : segundos;

            elementoTiempo.textContent = `${minutos}:${segundos}`;

            if (tiempoTotal <= 0) {
                clearInterval(cuentaRegresiva);
                contenedorTemp.innerHTML = `<span class="fw-bold text-danger" style="font-size: 0.95rem;">El código ha expirado. Por favor solicite uno nuevo.</span>`;
                btnFinalizar.setAttribute("disabled", "true");
            } else {
                tiempoTotal--;
            }
        }, 1000);
    }

    // Restricción: Solo permite escribir números y corta al llegar a 6 caracteres
    document.querySelectorAll("#txtCodigoCorreo, #txtCodigoTelefono").forEach(input => {
        input.addEventListener("input", function() {
            this.value = this.value.replace(/\D/g, '').slice(0, 6);
        });
    });

    // Control estricto al hacer clic en el botón de finalizar
    if (btnFinalizar) {
        btnFinalizar.addEventListener("click", function() {
            const inputCorreo = document.getElementById("txtCodigoCorreo");
            const inputTelefono = document.getElementById("txtCodigoTelefono");
            
            const errorCorreo = document.getElementById("errorCorreo");
            const errorTelefono = document.getElementById("errorTelefono");

            let valido = true;
            errorCorreo.textContent = "";
            errorTelefono.textContent = "";

            const valCorreo = inputCorreo.value.trim();
            const valTelefono = inputTelefono.value.trim();

            // Verificación que ambos tengan estrictamente 6 dígitos
            if (valCorreo.length !== 6) {
                errorCorreo.textContent = "El código del correo debe tener exactamente 6 dígitos.";
                valido = false;
            }

            if (valTelefono.length !== 6) {
                errorTelefono.textContent = "El código del teléfono debe tener exactamente 6 dígitos.";
                valido = false;
            }

            // Si ambos son válidos de 6 dígitos, se envía el formulario
            if (valido) {
                document.getElementById("formVerificacion").submit();
            } else {
                if (valCorreo.length !== 6) {
                    inputCorreo.focus();
                } else {
                    inputTelefono.focus();
                }
            }
        });
    }
});