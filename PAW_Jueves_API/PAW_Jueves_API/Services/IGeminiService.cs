namespace PAW_Jueves_API.Services
{
    /// <summary>Un mensaje de la conversación. Rol: "user" (el usuario) o "model" (la IA).</summary>
    public sealed record MensajeIA(string Rol, string Texto);

    /// <summary>Por qué no se obtuvo respuesta de Gemini.</summary>
    public enum ErrorGemini
    {
        Ninguno,
        /// <summary>No hay ApiKey configurada.</summary>
        NoConfigurado,
        /// <summary>Se acabó la cuota gratuita (429) o el servicio está saturado (503).</summary>
        LimiteCuota,
        /// <summary>Clave inválida o proyecto sin acceso (400/401/403).</summary>
        ClaveInvalida,
        /// <summary>Gemini bloqueó la respuesta o no devolvió texto.</summary>
        SinRespuesta,
        /// <summary>Error de red, tiempo agotado u otro código HTTP.</summary>
        Error
    }

    public sealed record ResultadoIA(string? Texto, ErrorGemini Error)
    {
        public bool Exitoso => Error == ErrorGemini.Ninguno && !string.IsNullOrWhiteSpace(Texto);
    }

    /// <summary>
    /// Cliente de la API de Gemini (Google). Solo la usa la API: la clave nunca llega al navegador.
    /// </summary>
    public interface IGeminiService
    {
        bool Habilitado { get; }

        string Modelo { get; }

        /// <summary>
        /// Envía la conversación a Gemini y devuelve el texto de la respuesta.
        /// </summary>
        /// <param name="instruccionSistema">Reglas para la IA (rol, temas permitidos, datos del mes).</param>
        /// <param name="mensajes">Conversación en orden, terminando con el mensaje del usuario.</param>
        Task<ResultadoIA> GenerarAsync(string instruccionSistema, IReadOnlyList<MensajeIA> mensajes, CancellationToken cancellationToken = default);
    }
}
