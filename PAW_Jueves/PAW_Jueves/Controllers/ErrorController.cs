using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PAW_Jueves.Controllers
{
    /// <summary>
    /// Manejo centralizado de errores de la WEB.
    /// En Program.cs, app.UseExceptionHandler("/Error/CapturarError") envía aquí
    /// cualquier excepción no controlada (por ejemplo, si la API está apagada).
    /// </summary>
    public class ErrorController : Controller
    {
        /// <summary>
        /// /Error/CapturarError. Recupera la excepción ocurrida y muestra la vista
        /// Views/Shared/Error.cshtml con un mensaje amigable para el usuario.
        /// </summary>
        public IActionResult CapturarError()
        {
            // Detalle de la excepción original; disponible para registrarla si se necesita
            var ex = HttpContext.Features.Get<IExceptionHandlerFeature>();

            return View("Error");
        }
    }
}
