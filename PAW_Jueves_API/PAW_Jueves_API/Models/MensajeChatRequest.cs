using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>Cuerpo de POST /api/asistente/mensajes.</summary>
    public class MensajeChatRequest
    {
        [Required, StringLength(1000, MinimumLength = 1)]
        public string Texto { get; set; } = string.Empty;
    }
}
