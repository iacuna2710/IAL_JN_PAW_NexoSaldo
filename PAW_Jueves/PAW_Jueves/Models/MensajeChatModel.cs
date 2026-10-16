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
}
