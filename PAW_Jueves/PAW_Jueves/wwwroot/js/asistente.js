/*
  Chat del asistente IA. Envía la pregunta con fetch a /Asistente/Enviar (que la pasa a la API)
  y agrega las burbujas. Seguridad: el texto se pone con textContent, NUNCA con innerHTML, así
  una respuesta de la IA con HTML o <script> se muestra como texto y no se ejecuta (XSS).
*/
(function () {
  'use strict';

  const formulario = document.getElementById('chat-formulario');
  if (!formulario) return;

  const contenedor = document.getElementById('chat-mensajes');
  const campo = document.getElementById('chat-texto');
  const boton = document.getElementById('chat-enviar');
  const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

  function agregar(texto, clase) {
    document.getElementById('chat-vacio')?.remove();
    const burbuja = document.createElement('div');
    burbuja.className = 'chat-burbuja ' + clase;
    burbuja.textContent = texto;
    contenedor.appendChild(burbuja);
    contenedor.scrollTop = contenedor.scrollHeight;
    return burbuja;
  }

  async function enviar(texto) {
    texto = texto.trim();
    if (!texto || boton.disabled) return;

    agregar(texto, 'chat-usuario');
    campo.value = '';
    boton.disabled = true;
    const pensando = agregar('Pensando…', 'chat-ia text-secondary');

    try {
      const respuesta = await fetch(formulario.dataset.url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
        body: JSON.stringify({ texto: texto })
      });
      const datos = await respuesta.json().catch(function () { return {}; });

      if (respuesta.status === 401 && datos.redirigir) {
        window.location.href = datos.redirigir;
        return;
      }

      pensando.remove();
      if (respuesta.ok && datos.texto) {
        agregar(datos.texto, 'chat-ia');
      } else {
        agregar(datos.mensaje || 'No se pudo obtener respuesta. Intente de nuevo.', 'chat-error');
      }
    } catch (e) {
      pensando.remove();
      agregar('No se pudo conectar con el servidor.', 'chat-error');
    } finally {
      boton.disabled = false;
      campo.focus();
    }
  }

  formulario.addEventListener('submit', function (evento) {
    evento.preventDefault();
    enviar(campo.value);
  });

  // Enter envía; Shift+Enter hace un salto de línea
  campo.addEventListener('keydown', function (evento) {
    if (evento.key === 'Enter' && !evento.shiftKey) {
      evento.preventDefault();
      enviar(campo.value);
    }
  });

  document.querySelectorAll('[data-sugerencia]').forEach(function (b) {
    b.addEventListener('click', function () { enviar(b.textContent); });
  });

  contenedor.scrollTop = contenedor.scrollHeight;
})();
