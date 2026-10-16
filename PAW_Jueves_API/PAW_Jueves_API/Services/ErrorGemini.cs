namespace PAW_Jueves_API.Services
{
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
}
