using PAW_Jueves.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;

namespace PAW_Jueves.Controllers
{
    /// <summary>
    /// Controlador MVC principal de la WEB. Muestra las pantallas de Login, Registro,
    /// Olvido de contraseña, Cambio de contraseña e Inicio.
    ///
    /// La WEB no se conecta a la BD: envía los datos de cada formulario a la API
    /// (Variables:ApiBaseUrl en appsettings.json) y según el código HTTP de la respuesta
    /// decide si redirige a otra pantalla o muestra un mensaje en la misma vista.
    ///
    /// Recibe por inyección de dependencias:
    ///  - _httpClient: cliente HTTP para llamar a la API (registrado con AddHttpClient)
    ///  - _configuration: para leer la URL base de la API
    /// </summary>
    public class HomeController(HttpClient _httpClient, IConfiguration _configuration) : Controller
    {
        #region Inicio de Sesión

        /// <summary>
        /// GET /Home/Login. Muestra el formulario de inicio de sesión.
        /// Es la página de inicio de la aplicación (ruta por defecto en Program.cs).
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// POST /Home/Login. Envía correo y contraseña a la API.
        /// Si la API responde 200, marca la sesión como autenticada y va al Index;
        /// si no, vuelve a mostrar el Login con el mensaje que devolvió la API.
        /// </summary>
        /// <param name="model">Datos escritos en el formulario.</param>
        [ValidateAntiForgeryToken] // Valida el token del formulario (evita ataques CSRF)
        [HttpPost]
        public IActionResult Login(LoginRequestModel model)
        {
            using var client = _httpClient;
            var url = _configuration.GetValue<string>("Variables:ApiBaseUrl") + "Home/Login";

            // Envía el modelo como JSON a la API y espera la respuesta
            var response = client.PostAsJsonAsync(url, model).Result;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                // Variable de sesión para saber que el usuario ya inició sesión
                HttpContext.Session.SetInt32("Autenticado", 1);
                return RedirectToAction("Index", "Home");
            }

            // Se lee el mensaje de error de la API para mostrarlo en la vista
            var datos = response.Content.ReadFromJsonAsync<UsuarioResponse>().Result;
            ViewBag.Mensaje = datos?.Mensaje;

            return View();
        }

        #endregion

        #region Registro de usuarios

        /// <summary>
        /// GET /Home/Register. Muestra el formulario de registro.
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// POST /Home/Register. Envía los datos del nuevo usuario a la API.
        /// Si el registro funciona, redirige al Login; si no, muestra el mensaje de la API.
        /// </summary>
        /// <param name="model">Identificación, nombre, correo y contraseña.</param>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Register(RegisterRequestModel model)
        {
            using var client = _httpClient;
            var url = _configuration.GetValue<string>("Variables:ApiBaseUrl") + "Home/Register";

            var response = client.PostAsJsonAsync(url, model).Result;

            if(response.StatusCode == HttpStatusCode.OK)
                return RedirectToAction("Login", "Home");

            var datos = response.Content.ReadFromJsonAsync<UsuarioResponse>().Result;
            ViewBag.Mensaje = datos?.Mensaje;

            return View();
        }

        #endregion

        #region Olvido de Contraseña

        /// <summary>
        /// GET /Home/Forgot. Muestra el formulario donde el usuario escribe su correo
        /// para recuperar el acceso.
        /// </summary>
        [HttpGet]
        public IActionResult Forgot()
        {
            return View();
        }

        /// <summary>
        /// POST /Home/Forgot. Envía el correo a la API, que valida que exista y genera
        /// el link cifrado de recuperación. Si la API responde 200, regresa al Login;
        /// si el correo no existe, muestra el mensaje en la misma pantalla.
        /// </summary>
        /// <param name="model">Correo del usuario.</param>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Forgot(ForgotRequestModel model)
        {
            using var client = _httpClient;
            var url = _configuration.GetValue<string>("Variables:ApiBaseUrl") + "Home/Forgot";

            var response = client.PostAsJsonAsync(url, model).Result;

            if (response.StatusCode == HttpStatusCode.OK)
                return RedirectToAction("Login", "Home");

            var datos = response.Content.ReadFromJsonAsync<UsuarioResponse>().Result;
            ViewBag.Mensaje = datos?.Mensaje;

            return View();
        }

        #endregion

        /// <summary>
        /// GET /Home/ChangePassword/{id}. Página a la que lleva el link de recuperación.
        /// El "id" es el token cifrado que generó la API (PasswordService.Cifrar).
        /// Por ahora solo muestra la vista "EN CONSTRUCCION"; el cambio de contraseña
        /// se completa en una próxima clase.
        /// </summary>
        /// <param name="id">Token cifrado que viene en el link.</param>
        public IActionResult ChangePassword(string id)
        {
            return View();
        }

        /// <summary>
        /// GET /Home/Index. Pantalla principal después de iniciar sesión (usa _Layout).
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }
    }
}
