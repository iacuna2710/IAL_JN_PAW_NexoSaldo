using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PAW_Jueves_API.Models;
using PAW_Jueves_API.Services;

namespace PAW_Jueves_API.Controllers
{
    /// <summary>
    /// Reportes para el dashboard (api/reportes). Todos los totales se calculan en SQL Server.
    /// La fase 6 (Gemini) usa estos mismos números para redactar el resumen: la IA nunca suma.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/reportes")]
    public class ReportesController(IConfiguration _configuration) : ControllerBase
    {
        /// <summary>GET /api/reportes/resumen-mensual?anno=2026&amp;mes=10. Por defecto, el mes actual.</summary>
        [HttpGet("resumen-mensual")]
        public async Task<IActionResult> ResumenMensual([FromQuery] int? anno, [FromQuery] int? mes, CancellationToken cancellationToken)
        {
            var hoy = ReglasGastos.HoyCostaRica();
            var (a, m) = (anno ?? hoy.Year, mes ?? hoy.Month);
            if (m is < 1 or > 12 || a is < 2000 or > 2100)
                return BadRequest(new ResponseApi { Mensaje = "Año o mes no válido" });

            await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));
            using var resultados = await context.QueryMultipleAsync(new CommandDefinition(
                "sp_ResumenMensual",
                new { IdUsuario = User.ObtenerIdUsuario(), Anno = a, Mes = m },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            var totales = await resultados.ReadSingleAsync<TotalesMes>();
            var porCategoria = await resultados.ReadAsync<GastoCategoriaResponse>();

            return Ok(new ResumenMensualResponse
            {
                Anno = a,
                Mes = m,
                Ingresos = totales.Ingresos,
                Gastos = totales.Gastos,
                CantidadMovimientos = totales.CantidadMovimientos,
                PorCategoria = porCategoria
            });
        }

        /// <summary>GET /api/reportes/tendencia?anno=2026&amp;mes=10&amp;meses=6. Ingresos y gastos por mes.</summary>
        [HttpGet("tendencia")]
        public async Task<IActionResult> Tendencia([FromQuery] int? anno, [FromQuery] int? mes, [FromQuery] int meses = 6, CancellationToken cancellationToken = default)
        {
            var hoy = ReglasGastos.HoyCostaRica();
            var (a, m) = (anno ?? hoy.Year, mes ?? hoy.Month);
            if (m is < 1 or > 12 || a is < 2000 or > 2100)
                return BadRequest(new ResponseApi { Mensaje = "Año o mes no válido" });

            await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));
            var datos = await context.QueryAsync<TendenciaMesResponse>(new CommandDefinition(
                "sp_TendenciaMensual",
                new { IdUsuario = User.ObtenerIdUsuario(), Anno = a, Mes = m, Meses = Math.Clamp(meses, 1, 24) },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            return Ok(datos);
        }
    }
}
