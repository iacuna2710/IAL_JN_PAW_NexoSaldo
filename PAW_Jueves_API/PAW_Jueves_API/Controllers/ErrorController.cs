using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PAW_Jueves_API.Controllers
{
    /// <summary>
    /// Punto único donde termina cualquier error inesperado de la API.
    ///
    /// Cómo llega una petición aquí: en Program.cs está la línea
    ///     app.UseExceptionHandler("/api/Error/CapturarError");
    /// Si cualquier controlador lanza una excepción que nadie atrapó (por ejemplo, SQL Server
    /// está apagado y SqlConnection falla), ASP.NET Core detiene esa petición y la vuelve a
    /// ejecutar internamente contra /api/Error/CapturarError. El cliente nunca ve el detalle
    /// técnico de la excepción (stack trace, nombres de tablas, etc.).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ErrorController : ControllerBase
    {
        /// <summary>
        /// Ruta /api/Error/CapturarError. Responde a quien hizo la petición original
        /// (normalmente la WEB) con un error 500 y un texto genérico.
        /// </summary>
        /// <returns>
        /// 500 Internal Server Error con el texto "Se presentó un inconveniente técnico".
        /// </returns>
        [Route("CapturarError")]
        public IActionResult CapturarError()
        {
            // ex contiene la excepción original (ex.Error) y la ruta donde ocurrió (ex.Path).
            // Por ahora no se usa; queda disponible para guardarla en una bitácora más adelante.
            var ex = HttpContext.Features.Get<IExceptionHandlerFeature>();

            return StatusCode(StatusCodes.Status500InternalServerError, "Se presentó un inconveniente técnico");
        }
    }
}
