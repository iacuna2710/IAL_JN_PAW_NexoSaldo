/*
  Tarjeta "Resumen IA" del dashboard. Se carga DESPUÉS de mostrar la página (no la hace esperar).
  La API guarda el resumen en caché: solo se vuelve a llamar a Gemini si cambian los números
  del mes o si el usuario presiona "Regenerar".
*/
(function () {
  'use strict';

  const tarjeta = document.getElementById('resumen-ia');
  if (!tarjeta) return;

  const texto = document.getElementById('resumen-ia-texto');
  const boton = document.getElementById('resumen-ia-regenerar');
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

  async function cargar(regenerar) {
    boton.disabled = true;
    texto.classList.add('text-secondary');
    texto.textContent = regenerar ? 'Generando un resumen nuevo…' : 'Preparando el resumen…';

    try {
      const url = tarjeta.dataset.url + '?periodo=' + encodeURIComponent(tarjeta.dataset.periodo) + '&regenerar=' + regenerar;
      const respuesta = await fetch(url, { method: 'POST', headers: { 'RequestVerificationToken': token } });
      const datos = await respuesta.json().catch(function () { return {}; });

      if (respuesta.status === 401 && datos.redirigir) {
        window.location.href = datos.redirigir;
        return;
      }

      // textContent: el texto de la IA nunca se interpreta como HTML
      texto.textContent = respuesta.ok && datos.texto ? datos.texto : (datos.mensaje || 'No se pudo generar el resumen.');
      texto.classList.toggle('text-secondary', !respuesta.ok);
    } catch (e) {
      texto.textContent = 'No se pudo conectar con el servidor.';
    } finally {
      boton.disabled = false;
    }
  }

  boton.addEventListener('click', function () { cargar(true); });
  cargar(false);
})();
