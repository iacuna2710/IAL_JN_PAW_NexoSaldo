using PAW_Jueves.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;

namespace PAW_Jueves.Controllers
{
    /// <summary>
    /// Controlador MVC principal de la WEB. Atiende las pantallas Login, Register, Forgot,
    /// ChangePassword e Index. Cada acción devuelve una vista de Views/Home con el mismo nombre
    /// del método (por ejemplo, Login() muestra Views/Home/Login.cshtml).
    ///
    /// La WEB NO se conecta a la base de datos. Para cada formulario hace esto:
    ///  1. Recibe los datos del formulario en un *RequestModel.
    ///  2. Los envía como JSON a la API, a la URL Variables:ApiBaseUrl de appsettings.json
    ///     (https://localhost:7122/api/) + "Home/Login", "Home/Register" o "Home/Forgot".
    ///  3. Si la API responde 200 OK, redirige a la siguiente pantalla.
    ///     Si responde cualquier otro código, lee el campo Mensaje del JSON de error, lo guarda
    ///     en ViewBag.Mensaje y vuelve a mostrar la misma vista, que lo pinta en una alerta.
    ///
    /// Sobre los campos vacíos: estos métodos NO revisan ModelState.IsValid, así que aunque el
    /// formulario llegue vacío, igual se llama a la API. La API responde 400 sin campo "mensaje",
    /// por lo que ViewBag.Mensaje queda vacío y no se muestra alerta. Los textos rojos debajo de
    /// cada campo ("El correo electrónico es obligatorio") salen de las validaciones [Required]
    /// de los modelos de la WEB, que ASP.NET Core evalúa al recibir el POST y que la vista muestra
    /// con &lt;span asp-validation-for&gt;.
    ///
    /// Sobre .Result: PostAsJsonAsync y ReadFromJsonAsync son métodos asíncronos; .Result hace que
    /// el método espere (bloqueado) hasta tener la respuesta antes de seguir con la línea siguiente.
    ///
    /// Dependencias que recibe en el constructor primario:
    ///  - _httpClient: objeto para hacer peticiones HTTP a la API (registrado con AddHttpClient).
    ///  - _configuration: lee appsettings.json para obtener Variables:ApiBaseUrl.
    /// </summary>
    public class HomeController(HttpClient _httpClient, IConfiguration _configuration) : Controller
    {
        #region Inicio de Sesión

        /// <summary>
        /// GET /Home/Login (y también "/", porque Program.cs define Home/Login como ruta por defecto).
        /// Muestra el formulario de inicio de sesión vacío.
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// POST /Home/Login. Se ejecuta al presionar "Procesar" en el formulario de inicio de sesión.
        /// Resultados posibles:
        ///  - API responde 200 OK: guarda Autenticado = 1 en la sesión y redirige a /Home/Index.
        ///  - API responde 404: vuelve a mostrar Login con la alerta
        ///    "No se validó la información correctamente" (correo o contraseña incorrectos).
        ///  - Campos vacíos: vuelve a mostrar Login con los mensajes rojos debajo de cada campo.
        ///  - API apagada o inalcanzable: PostAsJsonAsync lanza una excepción y el usuario
        ///    termina en la página de error (ErrorController.CapturarError).
        /// </summary>
        /// <param name="model">Correo y contraseña escritos en el formulario.</param>
        [ValidateAntiForgeryToken] // Rechaza el POST si no trae el token oculto que genera @Html.AntiForgeryToken() (protección CSRF)
        [HttpPost]
        public IActionResult Login(LoginRequestModel model)
        {
            // "using var" libera el HttpClient al terminar el método
            using var client = _httpClient;
            // Ejemplo de URL final: https://localhost:7122/api/Home/Login
            var url = _configuration.GetValue<string>("Variables:ApiBaseUrl") + "Home/Login";

            // Convierte model a JSON, lo envía por POST y espera la respuesta de la API
            var response = client.PostAsJsonAsync(url, model).Result;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                // Guarda en la sesión del navegador la variable "Autenticado" con valor 1.
                // Views/Shared/Error.cshtml la lee: si vale 1 usa _Layout (con menú),
                // si no existe usa _LayoutExterno (sin menú).
                HttpContext.Session.SetInt32("Autenticado", 1);
                return RedirectToAction("Index", "Home");
            }

            // Respuesta distinta de 200: se convierte el JSON de error a UsuarioResponse y se
            // toma su Mensaje. El "?." evita un error si la API no devolvió cuerpo (datos = null).
            var datos = response.Content.ReadFromJsonAsync<UsuarioResponse>().Result;
            ViewBag.Mensaje = datos?.Mensaje;

            // Vuelve a mostrar Login.cshtml; la vista pinta la alerta si ViewBag.Mensaje tiene texto
            return View();
        }

        #endregion

        #region Registro de usuarios

        /// <summary>
        /// GET /Home/Register. Muestra el formulario de registro vacío.
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// POST /Home/Register. Se ejecuta al presionar "Procesar" en el formulario de registro.
        /// Resultados posibles:
        ///  - API responde 200 OK: el usuario se creó; redirige a /Home/Login para que inicie sesión.
        ///  - API responde 422: la identificación o el correo ya existían; vuelve a mostrar Register
        ///    con la alerta "No se registró la información correctamente".
        ///  - Campos vacíos: vuelve a mostrar Register con los mensajes rojos debajo de cada campo.
        /// </summary>
        /// <param name="model">Identificación, nombre completo, correo y contraseña del formulario.</param>
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
        /// GET /Home/Forgot. Muestra el formulario donde el usuario escribe su correo para
        /// recuperar el acceso. Se llega desde el enlace "Olvidó su contraseña?" del Login.
        /// </summary>
        [HttpGet]
        public IActionResult Forgot()
        {
            return View();
        }

        /// <summary>
        /// POST /Home/Forgot. Se ejecuta al presionar "Procesar" en el formulario de recuperación.
        /// Resultados posibles:
        ///  - API responde 200 OK: el correo existe y la API generó el link cifrado de recuperación;
        ///    redirige a /Home/Login. (Hoy no se muestra ningún aviso de "revise su correo" y el
        ///    correo todavía no se envía; eso está pendiente en la API.)
        ///  - API responde 404: el correo no existe o el usuario está inactivo; vuelve a mostrar
        ///    Forgot con la alerta "No se validó la información correctamente".
        ///  - Campo vacío: vuelve a mostrar Forgot con "El correo electrónico es obligatorio".
        /// </summary>
        /// <param name="model">Correo escrito en el formulario.</param>
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
        /// GET /Home/ChangePassword/{id}. Es la página a la que apuntará el link de recuperación
        /// que genera la API, por ejemplo https://localhost:7280/Home/ChangePassword/Xy7_aB-...
        /// La parte final de la URL llega en el parámetro id gracias al patrón de ruta
        /// "{controller}/{action}/{id?}" definido en Program.cs.
        ///
        /// Estado actual: el id NO se usa todavía (no se descifra ni se valida) y la vista solo
        /// muestra el texto "EN CONSTRUCCION". El cambio de contraseña se completará más adelante.
        /// </summary>
        /// <param name="id">Token cifrado que viene al final del link de recuperación.</param>
        public IActionResult ChangePassword(string id)
        {
            return View();
        }

        /// <summary>
        /// GET /Home/Index. Pantalla principal que se muestra después de iniciar sesión.
        /// Usa el layout interno _Layout.cshtml (con menú lateral y barra superior).
        /// Nota: hoy no verifica la variable de sesión, así que también se puede abrir
        /// escribiendo la URL directamente sin haber iniciado sesión.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }
    }
}
