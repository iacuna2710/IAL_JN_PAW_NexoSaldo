using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using PAW_Jueves_API.Models;
using PAW_Jueves_API.Services;

namespace PAW_Jueves_API.Controllers
{
    /// <summary>
    /// Asistente con IA (api/asistente): chat de finanzas personales y resumen del dashboard.
    ///  - La clave de Gemini solo existe en la API; la WEB y el navegador nunca la ven.
    ///  - A Gemini se le mandan solo totales agregados (ver PromptsAsistente), nunca datos personales.
    ///  - [EnableRateLimiting("asistente")]: máximo 10 consultas por minuto por usuario, para cuidar la
    ///    cuota gratuita.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/asistente")]
    public class AsistenteController(
        IConfiguration _configuration,
        IGeminiService _gemini,
        IReportesService _reportes) : ControllerBase
    {
        /// <summary>Cuántos mensajes anteriores se le mandan a Gemini como contexto.</summary>
        private const int MensajesDeContexto = 10;

        private SqlConnection Conexion() => new(_configuration.GetValue<string>("Variables:ConnectionString"));

        /// <summary>GET /api/asistente/estado. Si el asistente está configurado y qué modelo usa.</summary>
        [HttpGet("estado")]
        public IActionResult Estado() =>
            Ok(new EstadoAsistenteResponse { Habilitado = _gemini.Habilitado, Modelo = _gemini.Modelo });

        /// <summary>GET /api/asistente/mensajes. Historial del chat (últimos 50, el más viejo primero).</summary>
        [HttpGet("mensajes")]
        public async Task<IActionResult> Historial(CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            var mensajes = await context.QueryAsync<MensajeChatResponse>(new CommandDefinition(
                "sp_ConsultarChat",
                new { IdUsuario = User.ObtenerIdUsuario(), Top = 50 },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            return Ok(mensajes);
        }

        /// <summary>
        /// POST /api/asistente/mensajes. Envía una pregunta: arma el contexto (últimos mensajes y los
        /// números del mes), consulta a Gemini y guarda la pregunta y la respuesta.
        /// </summary>
        /// <returns>200 con la respuesta; 503 si no hay clave; 429 si se acabó la cuota; 502 si Gemini falló.</returns>
        [HttpPost("mensajes")]
        [EnableRateLimiting("asistente")]
        public async Task<IActionResult> Enviar(MensajeChatRequest model, CancellationToken cancellationToken)
        {
            if (!_gemini.Habilitado)
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new AsistenteResponse { Mensaje = PromptsAsistente.MensajeError(ErrorGemini.NoConfigurado) });

            var idUsuario = User.ObtenerIdUsuario();
            var hoy = ReglasGastos.HoyCostaRica();
            var pregunta = model.Texto.Trim();

            await using var context = Conexion();
            var historial = (await context.QueryAsync<MensajeChatResponse>(new CommandDefinition(
                "sp_ConsultarChat",
                new { IdUsuario = idUsuario, Top = MensajesDeContexto },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken))).ToList();

            var resumen = await _reportes.ResumenMensualAsync(idUsuario, hoy.Year, hoy.Month, cancellationToken);
            var tendencia = await _reportes.TendenciaAsync(idUsuario, hoy.Year, hoy.Month, 3, cancellationToken);
            var moneda = await _reportes.MonedaAsync(idUsuario, cancellationToken);

            var conversacion = historial.Select(m => new MensajeIA(m.Rol, m.Texto)).ToList();
            conversacion.Add(new MensajeIA("user", pregunta));

            var resultado = await _gemini.GenerarAsync(
                PromptsAsistente.InstruccionChat(resumen, tendencia, moneda), conversacion, cancellationToken);

            if (!resultado.Exitoso)
                return RespuestaDeError(resultado.Error);

            var respuesta = resultado.Texto!.Length > 4000 ? resultado.Texto[..4000] : resultado.Texto;
            await GuardarMensajeAsync(context, idUsuario, "user", pregunta, cancellationToken);
            await GuardarMensajeAsync(context, idUsuario, "model", respuesta, cancellationToken);

            return Ok(new AsistenteResponse { Texto = respuesta });
        }

        /// <summary>DELETE /api/asistente/mensajes. Borra el historial del chat del usuario.</summary>
        [HttpDelete("mensajes")]
        public async Task<IActionResult> Limpiar(CancellationToken cancellationToken)
        {
            await using var context = Conexion();
            await context.ExecuteAsync(new CommandDefinition(
                "sp_LimpiarChat", new { IdUsuario = User.ObtenerIdUsuario() },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            return NoContent();
        }

        /// <summary>
        /// POST /api/asistente/resumen?anno=2026&amp;mes=10&amp;regenerar=false. Resumen del mes escrito por la IA.
        /// Se guarda en caché con un hash de los números: mientras los números no cambien (o no se pida
        /// regenerar), se devuelve el guardado sin llamar a Gemini.
        /// </summary>
        [HttpPost("resumen")]
        [EnableRateLimiting("asistente")]
        public async Task<IActionResult> Resumen([FromQuery] int? anno, [FromQuery] int? mes, [FromQuery] bool regenerar = false,
            CancellationToken cancellationToken = default)
        {
            if (!ReportesController.PeriodoValido(anno, mes, out var a, out var m))
                return BadRequest(new AsistenteResponse { Mensaje = "Año o mes no válido" });

            var idUsuario = User.ObtenerIdUsuario();
            var resumen = await _reportes.ResumenMensualAsync(idUsuario, a, m, cancellationToken);

            if (resumen.CantidadMovimientos == 0)
                return Ok(new AsistenteResponse { Texto = "Todavía no hay movimientos este mes. Registre sus ingresos y gastos para recibir un resumen." });

            var tendencia = await _reportes.TendenciaAsync(idUsuario, a, m, 3, cancellationToken);
            var quincenas = await _reportes.QuincenasAsync(idUsuario, a, m, cancellationToken);
            var moneda = await _reportes.MonedaAsync(idUsuario, cancellationToken);
            var hoy = ReglasGastos.HoyCostaRica();

            // El estado de cada quincena ("en curso", "todavía no empieza") también forma parte del hash:
            // cuando empieza la segunda quincena, el resumen se vuelve a generar aunque no haya movimientos nuevos.
            var estado = quincenas.Select(q => hoy < DateOnly.FromDateTime(q.Desde) ? "pendiente"
                : hoy <= DateOnly.FromDateTime(q.Hasta) ? "en curso" : "cerrada");
            var hash = CalcularHash(new { resumen, tendencia, quincenas, estado, moneda });

            await using var context = Conexion();
            var guardado = await context.QueryFirstOrDefaultAsync<ResumenIAFila>(new CommandDefinition(
                "sp_ConsultarResumenIA", new { IdUsuario = idUsuario, Anno = a, Mes = m },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            if (guardado != null && guardado.Hash == hash && !regenerar)
                return Ok(new AsistenteResponse { Texto = guardado.Texto, DesdeCache = true });

            if (!_gemini.Habilitado)
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new AsistenteResponse { Mensaje = PromptsAsistente.MensajeError(ErrorGemini.NoConfigurado) });

            var resultado = await _gemini.GenerarAsync(
                PromptsAsistente.InstruccionResumen(resumen, tendencia, quincenas, moneda, hoy),
                [new MensajeIA("user", "Escribe el resumen de este mes.")],
                cancellationToken);

            if (!resultado.Exitoso)
                return RespuestaDeError(resultado.Error);

            var texto = resultado.Texto!.Length > 4000 ? resultado.Texto[..4000] : resultado.Texto;
            await context.ExecuteAsync(new CommandDefinition(
                "sp_GuardarResumenIA", new { IdUsuario = idUsuario, Anno = a, Mes = m, Hash = hash, Texto = texto },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

            return Ok(new AsistenteResponse { Texto = texto });
        }

        private IActionResult RespuestaDeError(ErrorGemini error) => StatusCode(error switch
        {
            ErrorGemini.NoConfigurado => StatusCodes.Status503ServiceUnavailable,
            ErrorGemini.LimiteCuota => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status502BadGateway
        }, new AsistenteResponse { Mensaje = PromptsAsistente.MensajeError(error) });

        private static Task GuardarMensajeAsync(SqlConnection context, int idUsuario, string rol, string texto, CancellationToken cancellationToken) =>
            context.ExecuteAsync(new CommandDefinition(
                "sp_GuardarMensajeChat", new { IdUsuario = idUsuario, Rol = rol, Texto = texto },
                commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));

        /// <summary>SHA-256 de los datos del resumen: si cambia un monto, cambia el hash y se regenera el resumen.</summary>
        private static string CalcularHash(object datos)
        {
            var json = JsonSerializer.Serialize(datos);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        }
    }
}
