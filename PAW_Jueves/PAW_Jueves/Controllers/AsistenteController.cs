using System.Net;
using Microsoft.AspNetCore.Mvc;
using PAW_Jueves.Filters;
using PAW_Jueves.Models;
using PAW_Jueves.Services;

namespace PAW_Jueves.Controllers
{
    /// <summary>
    /// Asistente con IA. La WEB no habla con Gemini: le pasa la pregunta a la API, que es la única
    /// que tiene la clave. Enviar y Resumen responden JSON porque los llama JavaScript (fetch) sin
    /// recargar la página; llevan el token antiforgery en el header "RequestVerificationToken".
    /// </summary>
    [Autenticado]
    public class AsistenteController(ApiClient _api) : Controller
    {
        /// <summary>GET /Asistente/Index. Chat con el historial guardado.</summary>
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var estadoTarea = _api.GetAsync<EstadoAsistenteModel>("asistente/estado", cancellationToken);
            var mensajesTarea = _api.GetAsync<List<MensajeChatItem>>("asistente/mensajes", cancellationToken);
            await Task.WhenAll(estadoTarea, mensajesTarea);

            if (estadoTarea.Result.Estado == HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Home");
            }

            return View(new AsistenteViewModel
            {
                Habilitado = estadoTarea.Result.Datos?.Habilitado ?? false,
                Modelo = estadoTarea.Result.Datos?.Modelo ?? string.Empty,
                Mensajes = mensajesTarea.Result.Datos ?? []
            });
        }

        /// <summary>POST /Asistente/Enviar (JSON). Devuelve { texto } o { mensaje } con el código de error.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Enviar([FromBody] MensajeChatModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Texto))
                return BadRequest(new { mensaje = "Escriba una pregunta de hasta 1000 caracteres" });

            var respuesta = await _api.PostAsync<AsistenteRespuestaModel>("asistente/mensajes", new { model.Texto }, cancellationToken);
            return ResultadoJson(respuesta);
        }

        /// <summary>POST /Asistente/Resumen?periodo=2026-10&amp;regenerar=false (JSON). Resumen del mes para el dashboard.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Resumen(string? periodo, bool regenerar, CancellationToken cancellationToken)
        {
            var (anno, mes) = FormatoMoneda.LeerPeriodo(periodo, FormatoMoneda.HoyCostaRica());
            var respuesta = await _api.PostAsync<AsistenteRespuestaModel>(
                $"asistente/resumen?anno={anno}&mes={mes}&regenerar={regenerar.ToString().ToLowerInvariant()}", cancellationToken);
            return ResultadoJson(respuesta);
        }

        /// <summary>POST /Asistente/Limpiar. Borra el historial del chat.</summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Limpiar(CancellationToken cancellationToken)
        {
            await _api.DeleteAsync<AsistenteRespuestaModel>("asistente/mensajes", cancellationToken);
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Convierte la respuesta de la API en JSON para el navegador (nunca con detalles técnicos).</summary>
        private IActionResult ResultadoJson(RespuestaApi<AsistenteRespuestaModel> respuesta)
        {
            if (respuesta.Estado == HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Clear();
                return StatusCode(401, new { mensaje = "Su sesión venció. Inicie sesión de nuevo.", redirigir = Url.Action("Login", "Home") });
            }

            if (respuesta.Exitosa && !string.IsNullOrWhiteSpace(respuesta.Datos?.Texto))
                return Json(new { texto = respuesta.Datos.Texto, desdeCache = respuesta.Datos.DesdeCache });

            var mensaje = respuesta.Datos?.Mensaje is { Length: > 0 } m ? m : "El asistente no está disponible en este momento.";
            return StatusCode(respuesta.Exitosa ? 502 : (int)respuesta.Estado, new { mensaje });
        }
    }
}
