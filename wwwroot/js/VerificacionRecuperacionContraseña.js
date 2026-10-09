document.addEventListener("DOMContentLoaded", function () {
    // Lógica del temporizador de 4:59
    let tiempoTotal = 4 * 60 + 59;
    const elementoTiempo = document.getElementById("tiempoRestante");
    const contenedorTemp = document.getElementById("temporizadorContainer");
    const btnVerificar = document.getElementById("btnVerificar");

    if (elementoTiempo && contenedorTemp && btnVerificar) {
        const cuentaRegresiva = setInterval(() => {
            let minutos = Math.floor(tiempoTotal / 60);
            let segundos = tiempoTotal % 60;

            minutos = minutos < 10 ? "0" + minutos : minutos;
            segundos = segundos < 10 ? "0" + segundos : segundos;

            elementoTiempo.textContent = `${minutos}:${segundos}`;

            if (tiempoTotal <= 0) {
                clearInterval(cuentaRegresiva);
                contenedorTemp.innerHTML = `<span class="fw-bold text-danger" style="font-size: 0.90rem;">El código ha expirado. Por favor solicite uno nuevo.</span>`;
                btnVerificar.setAttribute("disabled", "true");
            } else {
                tiempoTotal--;
            }
        }, 1000);
    }

    // Restricción: Solo permite números y corta al llegar a 6 caracteres en la recuperación
    const inputCodigo = document.getElementById("txtCodigoVerificacion");
    if (inputCodigo) {
        inputCodigo.addEventListener("input", function() {
            this.value = this.value.replace(/\D/g, '').slice(0, 6);
        });
    }
});