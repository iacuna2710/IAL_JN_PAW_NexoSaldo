using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PAW_Jueves_API.Options;

namespace PAW_Jueves_API.Services
{
    /// <summary>
    /// Implementación de <see cref="IGeminiService"/> con la API REST de Gemini
    /// (POST {BaseUrl}/models/{Modelo}:generateContent). No usa SDK: solo HttpClient y JSON.
    ///
    /// La clave va en el header "x-goog-api-key", nunca en la URL (las URLs quedan en logs).
    /// En el log se escribe el modelo, la duración y los tokens usados; nunca la clave ni el texto.
    /// </summary>
    public sealed class GeminiService(HttpClient _http, IOptions<GeminiOptions> opciones, ILogger<GeminiService> _logger) : IGeminiService
    {
        private readonly GeminiOptions _opciones = opciones.Value;

        public bool Habilitado => _opciones.Habilitado;

        public string Modelo => _opciones.Modelo;

        public async Task<ResultadoIA> GenerarAsync(string instruccionSistema, IReadOnlyList<MensajeIA> mensajes, CancellationToken cancellationToken = default)
        {
            if (!Habilitado)
                return new ResultadoIA(null, ErrorGemini.NoConfigurado);

            var cuerpo = new SolicitudGemini(
                new Contenido(null, [new Parte(instruccionSistema)]),
                mensajes.Select(m => new Contenido(m.Rol, [new Parte(m.Texto)])).ToList(),
                new ConfiguracionGeneracion(0.4, _opciones.MaxTokensRespuesta));

            using var peticion = new HttpRequestMessage(HttpMethod.Post,
                $"{_opciones.BaseUrl.TrimEnd('/')}/models/{Uri.EscapeDataString(_opciones.Modelo)}:generateContent")
            {
                Content = JsonContent.Create(cuerpo, options: Json)
            };
            peticion.Headers.Add("x-goog-api-key", _opciones.ApiKey);

            var cronometro = Stopwatch.StartNew();
            try
            {
                using var respuesta = await _http.SendAsync(peticion, cancellationToken);

                if (!respuesta.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini respondió {Estado} ({Modelo}) en {Ms} ms",
                        (int)respuesta.StatusCode, _opciones.Modelo, cronometro.ElapsedMilliseconds);

                    return new ResultadoIA(null, respuesta.StatusCode switch
                    {
                        HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable => ErrorGemini.LimiteCuota,
                        HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ErrorGemini.ClaveInvalida,
                        _ => ErrorGemini.Error
                    });
                }

                var datos = await respuesta.Content.ReadFromJsonAsync<RespuestaGemini>(Json, cancellationToken);
                var texto = string.Concat(datos?.Candidates?.FirstOrDefault()?.Content?.Parts?.Select(p => p.Text) ?? []).Trim();

                _logger.LogInformation("Gemini {Modelo}: {Ms} ms, {Tokens} tokens",
                    _opciones.Modelo, cronometro.ElapsedMilliseconds, datos?.UsageMetadata?.TotalTokenCount);

                return string.IsNullOrWhiteSpace(texto)
                    ? new ResultadoIA(null, ErrorGemini.SinRespuesta)
                    : new ResultadoIA(texto, ErrorGemini.Ninguno);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;

                _logger.LogWarning(ex, "No se pudo consultar a Gemini ({Modelo})", _opciones.Modelo);
                return new ResultadoIA(null, ErrorGemini.Error);
            }
        }

        // ---------- Formato JSON de la API de Gemini (solo los campos que se usan) ----------

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private sealed record Parte(string? Text);
        private sealed record Contenido(string? Role, List<Parte>? Parts);
        private sealed record ConfiguracionGeneracion(double Temperature, int MaxOutputTokens);
        private sealed record SolicitudGemini(Contenido SystemInstruction, List<Contenido> Contents, ConfiguracionGeneracion GenerationConfig);

        private sealed record Candidato(Contenido? Content);
        private sealed record Uso(int TotalTokenCount);
        private sealed record RespuestaGemini(List<Candidato>? Candidates, Uso? UsageMetadata);
    }
}
