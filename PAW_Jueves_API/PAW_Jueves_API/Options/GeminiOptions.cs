using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Options
{
    /// <summary>
    /// Configuración de Gemini (sección "Gemini" de appsettings).
    ///
    /// La ApiKey NUNCA va en appsettings.json ni en el repositorio:
    ///  - En la PC (perfiles http/https y Container de Visual Studio): dotnet user-secrets.
    ///  - En docker-compose: variable de entorno Gemini__ApiKey, leída de un archivo .env ignorado por git.
    /// La ApiKey es opcional: si falta, la API arranca igual y el asistente responde "no disponible".
    /// Así el proyecto funciona para quien no tenga clave (compañeros, el CI).
    /// </summary>
    public sealed class GeminiOptions
    {
        public const string Seccion = "Gemini";

        /// <summary>Clave de Google AI Studio. Vacía = asistente deshabilitado.</summary>
        public string? ApiKey { get; init; }

        /// <summary>
        /// Modelo a usar. Se puede cambiar sin tocar código (ver los disponibles en AI Studio).
        /// Por defecto el alias "-latest" de Flash-Lite: Google lo actualiza solo, tiene más cuota
        /// gratuita y suele estar menos saturado que Flash (que a veces responde 503 por demanda).
        /// </summary>
        [Required]
        public string Modelo { get; init; } = "gemini-flash-lite-latest";

        [Required, Url]
        public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta";

        [Range(5, 120)]
        public int TimeoutSegundos { get; init; } = 30;

        /// <summary>Máximo de tokens de cada respuesta (limita el largo y el consumo de cuota).</summary>
        [Range(64, 8192)]
        public int MaxTokensRespuesta { get; init; } = 1024;

        public bool Habilitado => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
