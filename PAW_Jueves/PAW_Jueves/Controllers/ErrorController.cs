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
    public class ErrorController : Controller
    {
        /// <summary>
        /// /Error/CapturarError. Muestra Views/Shared/Error.cshtml ("Ha ocurrido un problema").
        /// Esa vista decide el diseño según la sesión: si la variable "Autenticado" vale 1 usa
        /// _Layout (con menú); si no, usa _LayoutExterno (pantalla sin menú, como el Login).
        /// </summary>
        public IActionResult CapturarError()
        {
            // ex contiene la excepción original (ex.Error) y la ruta donde ocurrió (ex.Path).
            // Por ahora no se usa; queda disponible para guardarla en una bitácora más adelante.
            var ex = HttpContext.Features.Get<IExceptionHandlerFeature>();

            return View("Error");
        }
    }
}
