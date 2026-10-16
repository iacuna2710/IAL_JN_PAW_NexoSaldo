using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>Cuerpo de PUT /api/Usuario/Contrasenna.</summary>
    public class CambiarContrasennaRequest
    {
        [Required]
        public string ContrasennaActual { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string ContrasennaNueva { get; set; } = string.Empty;
    }
}
