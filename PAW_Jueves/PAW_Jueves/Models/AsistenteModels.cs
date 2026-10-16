using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>Lo que manda el navegador al enviar una pregunta al chat (JSON).</summary>
    public class MensajeChatModel
    {
        [Required(ErrorMessage = "Escriba una pregunta")]
        [StringLength(1000, ErrorMessage = "La pregunta puede tener como máximo 1000 caracteres")]
        public string Texto { get; set; } = string.Empty;
    }

    /// <summary>Un mensaje del historial (Rol: "user" o "model").</summary>
    public class MensajeChatItem
    {
        public string Rol { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    /// <summary>Respuesta de la API para el chat y el resumen.</summary>
    public class AsistenteRespuestaModel
    {
        public string? Texto { get; set; }
        public bool DesdeCache { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class EstadoAsistenteModel
    {
        public bool Habilitado { get; set; }
        public string Modelo { get; set; } = string.Empty;
    }

    /// <summary>Pantalla del asistente.</summary>
    public class AsistenteViewModel
    {
        public bool Habilitado { get; set; }
        public string Modelo { get; set; } = string.Empty;
        public List<MensajeChatItem> Mensajes { get; set; } = [];
    }
}
