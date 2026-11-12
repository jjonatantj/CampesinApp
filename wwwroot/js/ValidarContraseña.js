
// valida la contraseña ingresada en el formulario de registro antes de que se envie que cumpla con los criterios de seguridad: 
//entre 6 y 15 caracteres, al menos una letra, un número y un carácter especial

document.addEventListener("DOMContentLoaded", function () {
    const form = document.querySelector("form");
    if (!form) return;

    form.addEventListener("submit", function (event) {
        const passwordInput = document.getElementById('txtContrasena');
        if (!passwordInput) return;

        const password = passwordInput.value;
        const regex = /^(?=.*[A-Za-z])(?=.*\d)(?=.*[\W_]).{6,15}$/;

        let errorSpan = document.getElementById("error-password-custom");
        if (!errorSpan) {
            errorSpan = document.createElement("span");
            errorSpan.id = "error-password-custom";
            errorSpan.className = "text-danger small d-block mt-1";
            passwordInput.closest('.col-md-6').appendChild(errorSpan);
        }

        if (!regex.test(password)) {
            event.preventDefault();
            errorSpan.textContent = "La contraseña debe tener entre 6 y 15 caracteres, e incluir al menos una letra, un número y un carácter especial.";
            passwordInput.focus();
        } else {
            errorSpan.textContent = "";
        }
    });
});