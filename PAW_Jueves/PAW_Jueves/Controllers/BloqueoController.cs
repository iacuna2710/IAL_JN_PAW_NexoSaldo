using System.Net;
using Microsoft.AspNetCore.Mvc;
using PAW_Jueves.Filters;
using PAW_Jueves.Models;
using PAW_Jueves.Services;

namespace PAW_Jueves.Controllers
{
    /// <summary>
    /// Lock Screen: bloquea la pantalla sin cerrar la sesión. Mientras está bloqueada, el filtro
    /// [Autenticado] manda todas las pantallas a LockScreen; para volver hay que escribir la
    /// contraseña. Con 5 intentos fallidos se cierra la sesión.
    ///
    /// Se bloquea con "Lock Screen" en el menú del perfil o solo, después de 10 minutos sin
    /// actividad (wwwroot/js/bloqueo.js).
    /// </summary>
    [Autenticado(PermitirBloqueado = true)]
    public class BloqueoController(ApiClient _api) : Controller
    {
        /// <summary>Intentos de desbloqueo permitidos antes de cerrar la sesión.</summary>
        public const int IntentosMaximos = 5;

        /// <summary>POST /Bloqueo/Bloquear. Marca la sesión como bloqueada y lo anota en la bitácora.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Bloquear(CancellationToken cancellationToken)
        {
            HttpContext.Session.SetInt32(ClavesSesion.Bloqueado, 1);
            HttpContext.Session.SetInt32(ClavesSesion.IntentosDesbloqueo, 0);

            // Solo para la bitácora: si falla, el bloqueo igual queda hecho en la sesión
            await _api.PostAsync<UsuarioResponse>("Usuario/Bloquear", cancellationToken);

            return RedirectToAction(nameof(LockScreen));
        }

        /// <summary>GET /Bloqueo/LockScreen. Pantalla con la foto, el nombre y el campo de contraseña.</summary>
        [HttpGet]
        public IActionResult LockScreen()
        {
            if (HttpContext.Session.GetInt32(ClavesSesion.Bloqueado) != 1)
                return RedirectToAction("Index", "Home");

            return View();
        }

        /// <summary>
        /// POST /Bloqueo/Desbloquear. La API verifica la contraseña:
        ///  - Correcta: se quita el bloqueo y vuelve al inicio.
        ///  - Incorrecta: suma un intento; al llegar a 5 cierra la sesión y manda al Login.
        /// No usa [EnableRateLimiting("login")]: ese límite es por IP y se comparte con el Login, así
        /// que podía cortar el desbloqueo sin mensaje. Aquí el límite es el contador de la sesión.
        /// </summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Desbloquear(string? contrasenna, CancellationToken cancellationToken)
        {
            if (HttpContext.Session.GetInt32(ClavesSesion.Bloqueado) != 1)
                return RedirectToAction("Index", "Home");

            if (string.IsNullOrEmpty(contrasenna))
            {
                ViewBag.Mensaje = "Escriba su contraseña";
                return View(nameof(LockScreen));
            }

            var respuesta = await _api.PostAsync<UsuarioResponse>("Usuario/VerificarContrasenna",
                new { Contrasenna = contrasenna }, cancellationToken);

            if (respuesta.Estado == HttpStatusCode.Unauthorized)
                return CerrarSesion("Su sesión venció. Inicie sesión de nuevo.");

            if (respuesta.Exitosa)
            {
                HttpContext.Session.Remove(ClavesSesion.Bloqueado);
                HttpContext.Session.Remove(ClavesSesion.IntentosDesbloqueo);
                return RedirectToAction("Index", "Home");
            }

            var intentos = (HttpContext.Session.GetInt32(ClavesSesion.IntentosDesbloqueo) ?? 0) + 1;
            if (intentos >= IntentosMaximos)
                return CerrarSesion("Se cerró la sesión por demasiados intentos de desbloqueo.");

            HttpContext.Session.SetInt32(ClavesSesion.IntentosDesbloqueo, intentos);
            var restantes = IntentosMaximos - intentos;
            ViewBag.Mensaje = restantes == 1
                ? "Contraseña incorrecta. Le queda 1 intento."
                : $"Contraseña incorrecta. Le quedan {restantes} intentos.";
            return View(nameof(LockScreen));
        }

        private IActionResult CerrarSesion(string mensaje)
        {
            HttpContext.Session.Clear();
            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Login", "Home");
        }
    }
}
