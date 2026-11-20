/*
 * Ventanas emergentes de CampesinApp.
 *
 * Hay dos modales en el layout: #modalPrincipal (formularios y listas) y #modalSuperpuesta (detalles, avisos y
 * confirmaciones que se muestran por encima). El servidor responde con HTML parcial y cabeceras que indican qué hacer:
 *   X-Modal-Destino: principal | superpuesta   -> en cuál modal se pinta el HTML
 *   X-Cerrar-Modal:  superpuesta | todas       -> cierra sin pintar nada
 *   X-Redirigir:     url                       -> navega a una página completa
 *
 * Atributos que activan el comportamiento (delegados, funcionan con el contenido que se inserta después):
 *   data-modal-url="/x/y"        abre la URL (GET) en un modal. Destino: la superpuesta si el elemento ya está
 *                                dentro de un modal, la principal si no. Se cambia con data-modal-destino.
 *   data-cerrar-origen           cierra el modal que contiene el elemento antes de abrir la URL.
 *   data-modal-post="/x/y"       envía un POST (con data-modal-datos='{"id":1}') y procesa la respuesta igual.
 *   form[data-ajax-form]         envía el formulario por fetch y pinta la respuesta en su mismo modal.
 *   data-modal-cerrar="..."      cierra: "" = su modal, "superpuesta", "todas". Con data-recargar-url vuelve a cargar
 *                                esa URL en la ventana principal; con data-redirigir navega a esa URL.
 *   #panelPagina                 (en pantallas completas) recibe el HTML cuando el servidor responde X-Modal-Destino: pagina,
 *                                o cuando el formulario / enlace que lo pidió está dentro del panel.
 *   data-modal-estatico          (en el contenido) el modal no se puede cerrar con ESC, clic fuera ni "X".
 *   data-confirmar="texto"       (en data-modal-post) pide confirmación antes de enviar, con una ventana de CampesinApp
 *                                (Aceptar / Cancelar) y no con el cuadro del navegador.
 *   select[data-copiar-extra]    al elegir una opción copia su data-extra en el campo indicado (selector CSS).
 *   data-autoredirigir="ms"      (con data-url) navega a la URL pasado ese tiempo; data-redirigir-ahora lo adelanta.
 */
(function () {
    'use strict';

    const modales = {
        principal: document.getElementById('modalPrincipal'),
        superpuesta: document.getElementById('modalSuperpuesta')
    };

    if (!modales.principal || !modales.superpuesta || typeof bootstrap === 'undefined') {
        return;
    }

    // Panel de una pantalla completa (#panelPagina): recibe el mismo HTML parcial que las ventanas, pero dentro de la página.
    const panelPagina = document.getElementById('panelPagina');

    const instancias = {
        principal: new bootstrap.Modal(modales.principal),
        superpuesta: new bootstrap.Modal(modales.superpuesta)
    };

    let temporizadorRedireccion = null;

    const contenido = (nombre) => modales[nombre].querySelector('.modal-content');
    const tokenCsrf = () => document.querySelector('meta[name="csrf-token"]')?.content ?? '';

    function modalDe(elemento) {
        if (elemento.closest('#modalSuperpuesta')) return 'superpuesta';
        if (elemento.closest('#modalPrincipal')) return 'principal';
        if (elemento.closest('#panelPagina')) return 'pagina';
        return null;
    }

    function cerrar(nombre) {
        if (!modales[nombre]) return; // el panel de página no se cierra: se navega con data-redirigir
        modales[nombre].dataset.forzarCierre = '1';
        instancias[nombre].hide();
    }

    function cerrarVarios(cuales) {
        if (cuales === 'todas') {
            cerrar('superpuesta');
            cerrar('principal');
        } else if (modales[cuales]) {
            cerrar(cuales);
        }
    }

    function escapar(texto) {
        const div = document.createElement('div');
        div.textContent = texto;
        return div.innerHTML;
    }

    function avisoGenerico(mensaje) {
        return '<div class="modal-body text-center p-4 aviso aviso-error">' +
            '<p class="fw-semibold mb-3">' + escapar(mensaje) + '</p>' +
            '<button type="button" class="btn btn-dark" data-modal-cerrar="superpuesta">Aceptar</button></div>';
    }

    function renderizar(nombre, html) {
        clearTimeout(temporizadorRedireccion);

        if (nombre === 'pagina') {
            if (panelPagina) {
                panelPagina.innerHTML = html;
                const autoPagina = panelPagina.querySelector('[data-autoredirigir]');
                if (autoPagina) {
                    temporizadorRedireccion = setTimeout(
                        () => window.location.assign(autoPagina.dataset.url),
                        Number.parseInt(autoPagina.dataset.autoredirigir, 10) || 6000);
                }
                return;
            }
            nombre = 'principal';
        }

        contenido(nombre).innerHTML = html;

        if (!modales[nombre].classList.contains('show')) {
            delete modales[nombre].dataset.forzarCierre;
            instancias[nombre].show();
        }

        const auto = contenido(nombre).querySelector('[data-autoredirigir]');
        if (auto) {
            temporizadorRedireccion = setTimeout(
                () => window.location.assign(auto.dataset.url),
                Number.parseInt(auto.dataset.autoredirigir, 10) || 6000);
        }
    }

    // Confirmación (Aceptar / Cancelar) con el diseño de CampesinApp, en la ventana de encima.
    let resolverConfirmacion = null;

    function preguntar(texto) {
        return new Promise((resolver) => {
            resolverConfirmacion?.(false);
            resolverConfirmacion = resolver;
            renderizar('superpuesta',
                '<div class="modal-body text-center p-4 aviso aviso-advertencia" role="alertdialog" aria-live="assertive">' +
                '<div class="aviso-icono"><i class="bi bi-question-lg"></i></div>' +
                '<p class="aviso-mensaje fw-semibold mb-3">' + escapar(texto) + '</p>' +
                '<div class="d-flex justify-content-center gap-2">' +
                '<button type="button" class="btn btn-dark px-4" data-confirmar-respuesta="si">Aceptar</button>' +
                '<button type="button" class="btn btn-outline-dark px-4" data-confirmar-respuesta="no">Cancelar</button>' +
                '</div></div>');
        });
    }

    async function procesar(respuesta, destinoPorDefecto) {
        const redirigir = respuesta.headers.get('X-Redirigir');
        if (redirigir) {
            window.location.assign(redirigir);
            return;
        }

        const cerrarHeader = respuesta.headers.get('X-Cerrar-Modal');
        if (cerrarHeader) {
            cerrarVarios(cerrarHeader);
            return;
        }

        const destino = respuesta.headers.get('X-Modal-Destino') || destinoPorDefecto;
        renderizar(destino, await respuesta.text());
    }

    async function pedir(url, opciones, destinoPorDefecto) {
        const metodo = (opciones.method || 'GET').toUpperCase();
        const cabeceras = Object.assign({ 'X-Requested-With': 'XMLHttpRequest' }, opciones.headers || {});
        if (metodo !== 'GET') {
            cabeceras['RequestVerificationToken'] = tokenCsrf();
        }

        try {
            const respuesta = await fetch(url, Object.assign({}, opciones, { method: metodo, headers: cabeceras, credentials: 'same-origin' }));
            const conInstruccion = respuesta.headers.get('X-Modal-Destino') || respuesta.headers.get('X-Cerrar-Modal') || respuesta.headers.get('X-Redirigir');
            if (!respuesta.ok && !conInstruccion) {
                renderizar('superpuesta', avisoGenerico(respuesta.status === 400
                    ? 'La solicitud no es válida. Recargue la página e intente de nuevo.'
                    : 'No se pudo completar la operación. Intenta de nuevo más tarde.'));
                return;
            }
            await procesar(respuesta, destinoPorDefecto);
        } catch (error) {
            renderizar('superpuesta', avisoGenerico('No se pudo conectar con el servidor. Intenta de nuevo más tarde.'));
        }
    }

    // El modal estático (calificación obligatoria, alerta de reporte vencido) solo se cierra por código.
    Object.keys(modales).forEach((nombre) => {
        modales[nombre].addEventListener('hide.bs.modal', (evento) => {
            if (modales[nombre].querySelector('[data-modal-estatico]') && !modales[nombre].dataset.forzarCierre) {
                evento.preventDefault();
            }
        });
        modales[nombre].addEventListener('hidden.bs.modal', () => {
            if (nombre === 'superpuesta' && resolverConfirmacion) {
                // Se cerró la pregunta (ESC o clic fuera) sin responder: equivale a Cancelar.
                const resolver = resolverConfirmacion;
                resolverConfirmacion = null;
                resolver(false);
            }
            delete modales[nombre].dataset.forzarCierre;
            clearTimeout(temporizadorRedireccion);
            contenido(nombre).innerHTML = '';
        });
    });

    document.addEventListener('click', (evento) => {
        const cierre = evento.target.closest('[data-modal-cerrar]');
        if (cierre) {
            const cuales = cierre.dataset.modalCerrar || modalDe(cierre) || 'superpuesta';
            cerrarVarios(cuales);
            if (cierre.dataset.recargarUrl) {
                void pedir(cierre.dataset.recargarUrl, { method: 'GET' }, 'principal');
            }
            if (cierre.dataset.redirigir) {
                window.location.assign(cierre.dataset.redirigir);
            }
            return;
        }

        const ahora = evento.target.closest('[data-redirigir-ahora]');
        if (ahora) {
            window.location.assign(ahora.dataset.url);
            return;
        }

        const enlace = evento.target.closest('[data-modal-url]');
        if (enlace) {
            evento.preventDefault();
            const origen = modalDe(enlace);
            if (enlace.hasAttribute('data-cerrar-origen') && origen) {
                cerrar(origen);
            }
            void pedir(enlace.dataset.modalUrl, { method: 'GET' }, enlace.dataset.modalDestino || (origen ? 'superpuesta' : 'principal'));
            return;
        }

        const respuesta = evento.target.closest('[data-confirmar-respuesta]');
        if (respuesta) {
            evento.preventDefault();
            const acepto = respuesta.dataset.confirmarRespuesta === 'si';
            const resolver = resolverConfirmacion;
            resolverConfirmacion = null;
            if (!acepto) cerrar('superpuesta');
            resolver?.(acepto);
            return;
        }

        const envio = evento.target.closest('[data-modal-post]');
        if (envio) {
            evento.preventDefault();
            const origen = modalDe(envio) || 'principal';
            const enviar = () => {
                const datos = new URLSearchParams(JSON.parse(envio.dataset.modalDatos || '{}'));
                void pedir(envio.dataset.modalPost, { method: 'POST', body: datos }, origen);
            };
            if (envio.dataset.confirmar) {
                void preguntar(envio.dataset.confirmar).then((acepto) => { if (acepto) enviar(); });
            } else {
                enviar();
            }
            return;
        }

        const estrella = evento.target.closest('.estrella[data-valor]');
        if (estrella) {
            const contenedor = estrella.closest('[data-estrellas]');
            const valor = Number.parseInt(estrella.dataset.valor, 10);
            contenedor.querySelector('input[type="hidden"]').value = valor;
            contenedor.querySelectorAll('.estrella').forEach((e) => {
                e.classList.toggle('activa', Number.parseInt(e.dataset.valor, 10) <= valor);
                e.setAttribute('aria-pressed', Number.parseInt(e.dataset.valor, 10) === valor ? 'true' : 'false');
            });
            contenedor.classList.remove('campo-error');
        }
    });

    document.addEventListener('submit', (evento) => {
        const formulario = evento.target.closest('form[data-ajax-form]');
        if (!formulario) return;

        evento.preventDefault();
        const nombreModal = modalDe(formulario) || 'principal';
        const destino = formulario.dataset.modalDestino || nombreModal;
        const boton = evento.submitter;
        const datos = new FormData(formulario, boton || undefined);
        const metodo = (formulario.getAttribute('method') || 'get').toUpperCase();

        if (metodo === 'GET') {
            const separador = formulario.action.includes('?') ? '&' : '?';
            void pedir(formulario.action + separador + new URLSearchParams(datos).toString(), { method: 'GET' }, destino);
            return;
        }

        if (boton) boton.disabled = true;
        void pedir(formulario.action, { method: 'POST', body: datos }, destino).finally(() => {
            if (boton) boton.disabled = false;
        });
    });

    document.addEventListener('change', (evento) => {
        const lista = evento.target.closest('select[data-copiar-extra]');
        if (!lista) return;
        const destino = lista.closest('form').querySelector(lista.dataset.copiarExtra);
        const opcion = lista.selectedOptions[0];
        if (destino && opcion) destino.value = opcion.dataset.extra || '';
    });

    window.CampesinApp = Object.assign(window.CampesinApp || {}, {
        abrir: (url, destino) => pedir(url, { method: 'GET' }, destino || 'principal'),
        estaAbierto: (nombre) => modales[nombre].classList.contains('show')
    });
})();
