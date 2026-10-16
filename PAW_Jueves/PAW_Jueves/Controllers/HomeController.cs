using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PAW_Jueves.Filters;
using PAW_Jueves.Models;
using PAW_Jueves.Services;

namespace PAW_Jueves.Controllers
{
    /// <summary>
    /// Controlador MVC principal de la WEB. Atiende las pantallas Login, Register, Forgot,
    /// ChangePassword, Index y la acción Logout. Cada acción devuelve una vista de Views/Home con el
    /// mismo nombre del método (por ejemplo, Login() muestra Views/Home/Login.cshtml).
    ///
    /// La WEB NO se conecta a la base de datos. Para cada formulario hace esto:
    ///  1. Recibe los datos del formulario en un *RequestModel y revisa sus validaciones
    ///     (ModelState.IsValid); si falta algo, vuelve a mostrar el formulario sin llamar a la API.
    ///  2. Los envía como JSON a la API usando ApiClient ("Home/Login", "Home/Register"...).
    ///  3. Si la API responde 200 OK, sigue a la siguiente pantalla. Si no, toma el Mensaje del
    ///     JSON de error, lo guarda en ViewBag.Mensaje y la vista lo pinta en una alerta.
    ///
    /// Las acciones son async: mientras se espera la respuesta de la API, el servidor puede
    /// atender otras peticiones (antes se usaba .Result, que bloquea).
    /// </summary>
    public class HomeController(ApiClient _api) : Controller
    {
        #region Inicio de Sesión

        /// <summary>
        /// GET /Home/Login (y también "/"). Muestra el formulario de inicio de sesión.
        /// Si ya hay sesión, va directo a Index. Si viene ?bloqueado=true es porque se superó el
        /// límite de intentos (ver AddRateLimiter en Program.cs).
        /// </summary>
        [HttpGet]
        public IActionResult Login(bool bloqueado = false)
        {
            if (HttpContext.Session.GetInt32(ClavesSesion.IdUsuario) != null)
                return RedirectToAction("Index", "Home");

            if (bloqueado)
                ViewBag.Mensaje = "Demasiados intentos de inicio de sesión. Espere un minuto e intente de nuevo.";
            else if (TempData["Mensaje"] is string mensaje)
                ViewBag.Mensaje = mensaje; // Por ejemplo, el Lock Screen cerró la sesión por intentos fallidos

            return View();
        }

        /// <summary>
        /// POST /Home/Login. Resultados posibles:
        ///  - API responde 200 OK: guarda el token y los datos del usuario en la sesión y va a Index.
        ///  - API responde 404: "No se validó la información correctamente".
        ///  - API responde 429: demasiados intentos.
        ///  - API apagada: termina en la página de error con "No se pudo conectar con el servidor".
        /// [EnableRateLimiting("login")]: máximo 5 intentos por minuto por navegador (IP).
        /// </summary>
        [ValidateAntiForgeryToken] // Rechaza el POST si no trae el token oculto del formulario (protección CSRF)
        [EnableRateLimiting("login")]
        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);

            var respuesta = await _api.PostAsync<UsuarioResponse>("Home/Login", model, cancellationToken);

            if (respuesta.Estado == HttpStatusCode.OK && respuesta.Datos != null)
            {
                // Se borra cualquier dato de una sesión anterior antes de guardar el usuario nuevo
                HttpContext.Session.Clear();
                HttpContext.Session.SetString(ClavesSesion.Token, respuesta.Datos.Token);
                HttpContext.Session.SetInt32(ClavesSesion.IdUsuario, respuesta.Datos.IdUsuario);
                HttpContext.Session.SetString(ClavesSesion.NombreCompleto, respuesta.Datos.NombreCompleto);
                HttpContext.Session.SetString(ClavesSesion.FotoPerfil, respuesta.Datos.FotoPerfil ?? string.Empty);
                HttpContext.Session.SetString(ClavesSesion.Moneda, respuesta.Datos.Moneda);
                HttpContext.Session.SetInt32(ClavesSesion.Autenticado, 1);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Mensaje = MensajeDeError(respuesta);
            return View(model);
        }

        /// <summary>
        /// POST /Home/Logout. Borra todos los datos de la sesión (token incluido) y vuelve al Login.
        /// Es POST con antiforgery para que otro sitio no pueda cerrar la sesión con un simple link.
        /// </summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Home");
        }

        #endregion

        #region Registro de usuarios

        /// <summary>GET /Home/Register. Muestra el formulario de registro vacío.</summary>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// POST /Home/Register. Resultados posibles:
        ///  - API responde 200 OK: el usuario se creó; redirige a /Home/Login.
        ///  - API responde 409: la identificación o el correo ya existían; se muestra el mensaje.
        /// </summary>
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterRequestModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);

            var respuesta = await _api.PostAsync<UsuarioResponse>("Home/Register", model, cancellationToken);

            if (respuesta.Estado == HttpStatusCode.OK)
                return RedirectToAction("Login", "Home");

            ViewBag.Mensaje = MensajeDeError(respuesta);
            return View(model);
        }

        #endregion

        #region Olvido de Contraseña

        /// <summary>GET /Home/Forgot. Formulario para recuperar el acceso con el correo.</summary>
        [HttpGet]
        public IActionResult Forgot()
        {
            return View();
        }

        /// <summary>
        /// POST /Home/Forgot. Si la API responde 200 OK (el correo existe y se generó el link de
        /// recuperación) vuelve al Login; si no, muestra el mensaje de la API.
        /// </summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Forgot(ForgotRequestModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);

            var respuesta = await _api.PostAsync<UsuarioResponse>("Home/Forgot", model, cancellationToken);

            if (respuesta.Estado == HttpStatusCode.OK)
                return RedirectToAction("Login", "Home");

            ViewBag.Mensaje = MensajeDeError(respuesta);
            return View(model);
        }

        #endregion

        /// <summary>
        /// GET /Home/ChangePassword/{id}. Página a la que apuntará el link de recuperación que genera
        /// la API. Estado actual: el id todavía no se usa y la vista dice "EN CONSTRUCCION".
        /// </summary>
        /// <param name="id">Token cifrado que viene al final del link de recuperación.</param>
        public IActionResult ChangePassword(string id)
        {
            return View();
        }

        /// <summary>
        /// GET /Home/Index?periodo=2026-10. Dashboard: totales del mes, gasto por categoría, tendencia
        /// de 6 meses, presupuestos y actividad reciente. [Autenticado] redirige al Login si no hay sesión.
        /// </summary>
        [Autenticado]
        public async Task<IActionResult> Index(string? periodo, CancellationToken cancellationToken)
        {
            var (anno, mes) = FormatoMoneda.LeerPeriodo(periodo, FormatoMoneda.HoyCostaRica());

            // Las tres consultas son independientes: se lanzan a la vez y se espera a que terminen todas
            var resumenTarea = _api.GetAsync<ResumenMensualModel>($"reportes/resumen-mensual?anno={anno}&mes={mes}", cancellationToken);
            var tendenciaTarea = _api.GetAsync<List<TendenciaMesModel>>($"reportes/tendencia?anno={anno}&mes={mes}&meses=6", cancellationToken);
            var actividadTarea = _api.GetAsync<List<ActividadModel>>("Usuario/Actividad?top=5", cancellationToken);
            await Task.WhenAll(resumenTarea, tendenciaTarea, actividadTarea);

            if (resumenTarea.Result.Estado == HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Home");
            }

            return View(new DashboardViewModel
            {
                Anno = anno,
                Mes = mes,
                Moneda = HttpContext.Session.GetString(ClavesSesion.Moneda) ?? "CRC",
                Resumen = resumenTarea.Result.Datos ?? new ResumenMensualModel { Anno = anno, Mes = mes },
                Tendencia = tendenciaTarea.Result.Datos ?? [],
                Actividad = actividadTarea.Result.Datos ?? []
            });
        }

        /// <summary>
        /// Texto a mostrar cuando la API no respondió 200: el Mensaje que mandó la API o, si no
        /// mandó ninguno, uno genérico según el código HTTP.
        /// </summary>
        private static string MensajeDeError(RespuestaApi<UsuarioResponse> respuesta)
        {
            if (!string.IsNullOrWhiteSpace(respuesta.Datos?.Mensaje))
                return respuesta.Datos.Mensaje;

            return respuesta.Estado == HttpStatusCode.TooManyRequests
                ? "Demasiados intentos. Espere un minuto e intente de nuevo."
                : "No se pudo procesar la solicitud. Intente de nuevo.";
        }
    }
}
