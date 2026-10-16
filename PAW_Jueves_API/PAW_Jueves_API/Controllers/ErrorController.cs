using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PAW_Jueves_API.Models;

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
    public class ErrorController(ILogger<ErrorController> _logger) : ControllerBase
    {
        /// <summary>
        /// Ruta /api/Error/CapturarError. Responde a quien hizo la petición original
        /// (normalmente la WEB) con un error 500 y un texto genérico.
        /// </summary>
        /// <returns>
        /// 500 Internal Server Error con un JSON { "mensaje": "..." }, el mismo formato que usan
        /// los demás endpoints, para que la WEB pueda mostrar el texto en la alerta del formulario.
        ///  - Si el error vino de SQL Server (por ejemplo, el contenedor de la BD está apagado),
        ///    el mensaje lo dice, para no confundirlo con "usuario o contraseña incorrectos".
        ///  - Cualquier otro error: "Se presentó un inconveniente técnico".
        /// </returns>
        [Route("CapturarError")]
        public IActionResult CapturarError()
        {
            // ex contiene la excepción original (ex.Error) y la ruta donde ocurrió (ex.Path).
            var ex = HttpContext.Features.Get<IExceptionHandlerFeature>();

            // El detalle técnico se escribe en el log del servidor (consola / docker logs),
            // nunca en la respuesta al cliente.
            _logger.LogError(ex?.Error, "Error no controlado en {Ruta}", ex?.Path);

            var mensaje = ex?.Error is SqlException
                ? "No se pudo conectar con la base de datos. Verifique que SQL Server esté levantado."
                : "Se presentó un inconveniente técnico";

            return StatusCode(StatusCodes.Status500InternalServerError, new ResponseApi { Mensaje = mensaje });
        }
    }
}
