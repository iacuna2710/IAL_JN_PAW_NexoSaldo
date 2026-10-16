using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PAW_Jueves.Controllers
{
    /// <summary>
    /// Punto único donde termina cualquier error inesperado de la WEB.
    ///
    /// Cómo llega una petición aquí: en Program.cs está la línea
    ///     app.UseExceptionHandler("/Error/CapturarError");
    /// Si una acción lanza una excepción que nadie atrapó, ASP.NET Core detiene esa petición y
    /// la vuelve a ejecutar internamente contra /Error/CapturarError. Caso típico: la API está
    /// apagada, PostAsJsonAsync no logra conectarse y lanza HttpRequestException.
    /// </summary>
    public class ErrorController(ILogger<ErrorController> _logger) : Controller
    {
        /// <summary>
        /// /Error/CapturarError. Muestra Views/Shared/Error.cshtml ("Ha ocurrido un problema").
        /// Esa vista decide el diseño según la sesión: si la variable "Autenticado" vale 1 usa
        /// _Layout (con menú); si no, usa _LayoutExterno (pantalla sin menú, como el Login).
        /// </summary>
        public IActionResult CapturarError()
        {
            // ex contiene la excepción original (ex.Error) y la ruta donde ocurrió (ex.Path).
            var ex = HttpContext.Features.Get<IExceptionHandlerFeature>();

            // El detalle técnico va al log del servidor, nunca a la pantalla.
            _logger.LogError(ex?.Error, "Error no controlado en {Ruta}", ex?.Path);

            // HttpRequestException = la WEB no logró conectarse con la API (apagada, puerto
            // equivocado en Variables:ApiBaseUrl, etc.). Se avisa con un texto específico para
            // no confundirlo con un error de datos. Error.cshtml lo muestra si viene con texto.
            // Si algún código usa .Result, la excepción real llega envuelta en una AggregateException,
            // por eso se revisa también la excepción interna.
            var error = ex?.Error is AggregateException agregada ? agregada.InnerException : ex?.Error;
            if (error is HttpRequestException)
                ViewBag.Mensaje = "No se pudo conectar con el servidor (API). Verifique que esté levantado e intente de nuevo.";

            return View("Error");
        }
    }
}
