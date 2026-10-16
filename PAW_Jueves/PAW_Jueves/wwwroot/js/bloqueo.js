/*
  Bloqueo automático de pantalla por inactividad.
  Si el usuario no mueve el mouse, no escribe ni hace scroll durante los minutos indicados en
  data-minutos-inactividad (formulario #form-bloqueo del _Layout), se envía ese formulario, que
  bloquea la pantalla (POST /Bloqueo/Bloquear). El tiempo se reinicia con cualquier actividad.
*/
(function () {
  'use strict';

  const formulario = document.getElementById('form-bloqueo');
  if (!formulario) return;

  const minutos = Number(formulario.dataset.minutosInactividad) || 10;
  const espera = minutos * 60 * 1000;
  let temporizador;

  function reiniciar() {
    clearTimeout(temporizador);
    temporizador = setTimeout(function () { formulario.submit(); }, espera);
  }

  ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart'].forEach(function (evento) {
    window.addEventListener(evento, reiniciar, { passive: true });
  });

  reiniciar();
})();
