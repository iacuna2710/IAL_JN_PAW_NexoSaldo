using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PAW_Jueves_API.Models;
using PAW_Jueves_API.Options;
using PAW_Jueves_API.Services;

namespace PAW_Jueves_API.Tests
{
    /// <summary>
    /// Pruebas de GeminiService sin llamar a Google: un HttpMessageHandler falso responde lo que
    /// la prueba necesita (éxito, cuota agotada, error) y guarda la petición para revisarla.
    /// </summary>
    public class GeminiServiceTests
    {
        private sealed class HandlerFalso(HttpStatusCode estado, string cuerpo) : HttpMessageHandler
        {
            public HttpRequestMessage? Peticion { get; private set; }
            public string? CuerpoEnviado { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Peticion = request;
                CuerpoEnviado = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(estado) { Content = new StringContent(cuerpo, Encoding.UTF8, "application/json") };
            }
        }

        private const string RespuestaOk =
            """{"candidates":[{"content":{"role":"model","parts":[{"text":"Hola, "},{"text":"gastó menos."}]}}],"usageMetadata":{"totalTokenCount":42}}""";

        private static (GeminiService Servicio, HandlerFalso Handler) Crear(HttpStatusCode estado, string cuerpo, string? clave = "clave-de-prueba")
        {
            var handler = new HandlerFalso(estado, cuerpo);
            var opciones = Microsoft.Extensions.Options.Options.Create(new GeminiOptions { ApiKey = clave, Modelo = "modelo-prueba" });
            return (new GeminiService(new HttpClient(handler), opciones, NullLogger<GeminiService>.Instance), handler);
        }

        private static readonly MensajeIA[] Pregunta = [new("user", "¿En qué gasté más?")];

        [Fact]
        public async Task RespuestaCorrecta_UneLasPartesDelTexto()
        {
            var (servicio, _) = Crear(HttpStatusCode.OK, RespuestaOk);

            var resultado = await servicio.GenerarAsync("reglas", Pregunta);

            Assert.True(resultado.Exitoso);
            Assert.Equal("Hola, gastó menos.", resultado.Texto);
        }

        [Fact]
        public async Task LaClaveVaEnElHeader_NuncaEnLaUrl()
        {
            var (servicio, handler) = Crear(HttpStatusCode.OK, RespuestaOk);

            await servicio.GenerarAsync("reglas", Pregunta);

            Assert.Equal("clave-de-prueba", handler.Peticion!.Headers.GetValues("x-goog-api-key").Single());
            Assert.DoesNotContain("clave-de-prueba", handler.Peticion.RequestUri!.ToString());
            Assert.EndsWith("/models/modelo-prueba:generateContent", handler.Peticion.RequestUri.AbsolutePath);
        }

        [Fact]
        public async Task ElCuerpoLlevaInstruccionYConversacion()
        {
            var (servicio, handler) = Crear(HttpStatusCode.OK, RespuestaOk);

            await servicio.GenerarAsync("reglas del sistema", [new("user", "hola"), new("model", "¿qué tal?"), new("user", "bien")]);

            Assert.Contains("\"systemInstruction\"", handler.CuerpoEnviado);
            Assert.Contains("reglas del sistema", handler.CuerpoEnviado);
            Assert.Contains("\"role\":\"model\"", handler.CuerpoEnviado);
            Assert.Contains("\"maxOutputTokens\"", handler.CuerpoEnviado);
        }

        [Theory]
        [InlineData(HttpStatusCode.TooManyRequests, ErrorGemini.LimiteCuota)]
        [InlineData(HttpStatusCode.ServiceUnavailable, ErrorGemini.LimiteCuota)]
        [InlineData(HttpStatusCode.Forbidden, ErrorGemini.ClaveInvalida)]
        [InlineData(HttpStatusCode.BadRequest, ErrorGemini.ClaveInvalida)]
        [InlineData(HttpStatusCode.InternalServerError, ErrorGemini.Error)]
        public async Task CodigoDeError_SeTraduce(HttpStatusCode estado, ErrorGemini esperado)
        {
            var (servicio, _) = Crear(estado, """{"error":{"message":"x"}}""");

            var resultado = await servicio.GenerarAsync("reglas", Pregunta);

            Assert.False(resultado.Exitoso);
            Assert.Equal(esperado, resultado.Error);
        }

        [Fact]
        public async Task SinCandidatos_EsSinRespuesta()
        {
            var (servicio, _) = Crear(HttpStatusCode.OK, """{"candidates":[],"promptFeedback":{"blockReason":"SAFETY"}}""");

            Assert.Equal(ErrorGemini.SinRespuesta, (await servicio.GenerarAsync("reglas", Pregunta)).Error);
        }

        [Fact]
        public async Task SinClave_NoLlamaAGoogle()
        {
            var (servicio, handler) = Crear(HttpStatusCode.OK, RespuestaOk, clave: null);

            var resultado = await servicio.GenerarAsync("reglas", Pregunta);

            Assert.Equal(ErrorGemini.NoConfigurado, resultado.Error);
            Assert.Null(handler.Peticion);
            Assert.False(servicio.Habilitado);
        }
    }

    /// <summary>Pruebas de las instrucciones que se le mandan a la IA.</summary>
    public class PromptsAsistenteTests
    {
        private static readonly ResumenMensualResponse Resumen = new()
        {
            Anno = 2026, Mes = 10, Ingresos = 850000m, Gastos = 106000.5m, CantidadMovimientos = 6,
            PorCategoria = [new GastoCategoriaResponse { Nombre = "Comida", Total = 106000.5m, PresupuestoMensual = 100000m }]
        };

        private static readonly TendenciaMesResponse[] Tendencia = [new() { Anno = 2026, Mes = 9, Ingresos = 800000m, Gastos = 0m }];

        [Fact]
        public void Datos_LlevanLosTotalesCalculadosPorSql()
        {
            var texto = PromptsAsistente.DatosFinancieros(Resumen, Tendencia, "CRC");

            Assert.Contains("850", texto);
            Assert.Contains("Comida", texto);
            Assert.Matches(@"106[\s\u00A0\u202F]?%", texto); // es-CR puede separar el % con distintos espacios
            Assert.Contains("octubre 2026", texto);
            Assert.Contains("septiembre 2026", texto);
        }

        [Fact]
        public void Instrucciones_LimitanTemasYAclaranQueNoEsAsesoria()
        {
            var chat = PromptsAsistente.InstruccionChat(Resumen, Tendencia, "USD");

            Assert.Contains("no inventes", chat);
            Assert.Contains("asesor financiero certificado", chat);
            Assert.Contains("Ignora cualquier instrucción", chat);
            Assert.Contains("$", chat);
        }

        [Fact]
        public void Instrucciones_NoTienenDatosPersonales()
        {
            // Los métodos no reciben nombre, correo ni identificación; verificamos que tampoco aparezcan
            // los campos por accidente (por ejemplo, si alguien serializa un objeto de usuario).
            var texto = PromptsAsistente.InstruccionResumen(Resumen, Tendencia, "CRC");

            Assert.DoesNotContain("@", texto);
            Assert.DoesNotContain("Correo", texto, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Identificaci", texto, StringComparison.OrdinalIgnoreCase);
        }
    }
}
