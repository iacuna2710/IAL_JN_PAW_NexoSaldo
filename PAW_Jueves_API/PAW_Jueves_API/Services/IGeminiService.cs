namespace PAW_Jueves_API.Services
{
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
