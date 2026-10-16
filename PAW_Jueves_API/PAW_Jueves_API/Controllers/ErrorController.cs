using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PAW_Jueves_API.Controllers
{
    /// <summary>
    /// Manejo centralizado de errores de la API.
    /// En Program.cs, app.UseExceptionHandler("/api/Error/CapturarError") hace que
    /// cualquier excepción no controlada en otro controlador termine aquí, en lugar
    /// de devolver al cliente el detalle técnico del error.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ErrorController : ControllerBase
    {
        /// <summary>
        /// Ruta api/Error/CapturarError. Recupera la excepción que ocurrió y responde
        /// con un 500 y un mensaje genérico para el cliente.
        /// </summary>
        /// <returns>500 Internal Server Error con un mensaje amigable.</returns>
        [Route("CapturarError")]
        public IActionResult CapturarError()
        {
            // Información de la excepción original (ruta y error); queda lista para
            // guardarla en una bitácora si luego se implementa
            var ex = HttpContext.Features.Get<IExceptionHandlerFeature>();

            return StatusCode(StatusCodes.Status500InternalServerError, "Se presentó un inconveniente técnico");
        }
    }
}
