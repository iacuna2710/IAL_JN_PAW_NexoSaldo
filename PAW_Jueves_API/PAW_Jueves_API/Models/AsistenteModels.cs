using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>Cuerpo de POST /api/asistente/mensajes.</summary>
    public class MensajeChatRequest
    {
        [Required, StringLength(1000, MinimumLength = 1)]
        public string Texto { get; set; } = string.Empty;
    }

    /// <summary>Un mensaje del historial del chat.</summary>
    public class MensajeChatResponse
    {
        /// <summary>"user" o "model".</summary>
        public string Rol { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    /// <summary>Respuesta del chat y del resumen.</summary>
    public class AsistenteResponse : ResponseApi
    {
        public string? Texto { get; set; }

        /// <summary>True si el resumen salió de la caché (no se llamó a Gemini).</summary>
        public bool DesdeCache { get; set; }
    }

    /// <summary>GET /api/asistente/estado.</summary>
    public class EstadoAsistenteResponse
    {
        public bool Habilitado { get; set; }
        public string Modelo { get; set; } = string.Empty;
    }

    /// <summary>Fila de sp_ConsultarResumenIA.</summary>
    public class ResumenIAFila
    {
        public string Hash { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
    }
}
