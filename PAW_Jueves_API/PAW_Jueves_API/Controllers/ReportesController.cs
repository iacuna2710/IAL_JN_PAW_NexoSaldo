using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAW_Jueves_API.Models;
using PAW_Jueves_API.Services;

namespace PAW_Jueves_API.Controllers
{
    /// <summary>
    /// Reportes para el dashboard (api/reportes). Todos los totales se calculan en SQL Server
    /// (ver ReportesService). El asistente con IA usa estos mismos números: la IA nunca suma.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/reportes")]
    public class ReportesController(IReportesService _reportes) : ControllerBase
    {
        /// <summary>GET /api/reportes/resumen-mensual?anno=2026&amp;mes=10. Por defecto, el mes actual.</summary>
        [HttpGet("resumen-mensual")]
        public async Task<IActionResult> ResumenMensual([FromQuery] int? anno, [FromQuery] int? mes, CancellationToken cancellationToken)
        {
            if (!PeriodoValido(anno, mes, out var a, out var m))
                return BadRequest(new ResponseApi { Mensaje = "Año o mes no válido" });

            return Ok(await _reportes.ResumenMensualAsync(User.ObtenerIdUsuario(), a, m, cancellationToken));
        }

        /// <summary>GET /api/reportes/tendencia?anno=2026&amp;mes=10&amp;meses=6. Ingresos y gastos por mes.</summary>
        [HttpGet("tendencia")]
        public async Task<IActionResult> Tendencia([FromQuery] int? anno, [FromQuery] int? mes, [FromQuery] int meses = 6, CancellationToken cancellationToken = default)
        {
            if (!PeriodoValido(anno, mes, out var a, out var m))
                return BadRequest(new ResponseApi { Mensaje = "Año o mes no válido" });

            return Ok(await _reportes.TendenciaAsync(User.ObtenerIdUsuario(), a, m, meses, cancellationToken));
        }

        /// <summary>GET /api/reportes/quincenas?anno=2026&amp;mes=10. Totales del 1 al 15 y del 16 a fin de mes.</summary>
        [HttpGet("quincenas")]
        public async Task<IActionResult> Quincenas([FromQuery] int? anno, [FromQuery] int? mes, CancellationToken cancellationToken)
        {
            if (!PeriodoValido(anno, mes, out var a, out var m))
                return BadRequest(new ResponseApi { Mensaje = "Año o mes no válido" });

            return Ok(await _reportes.QuincenasAsync(User.ObtenerIdUsuario(), a, m, cancellationToken));
        }

        /// <summary>Si no se indica año y mes, usa el mes actual de Costa Rica.</summary>
        internal static bool PeriodoValido(int? anno, int? mes, out int a, out int m)
        {
            var hoy = ReglasGastos.HoyCostaRica();
            (a, m) = (anno ?? hoy.Year, mes ?? hoy.Month);
            return m is >= 1 and <= 12 && a is >= 2000 and <= 2100;
        }
    }
}
