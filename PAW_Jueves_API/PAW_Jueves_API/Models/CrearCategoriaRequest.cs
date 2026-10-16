using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>Cuerpo de POST /api/categorias.</summary>
    public class CrearCategoriaRequest
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required, RegularExpression("^[IG]$")]
        public string Tipo { get; set; } = "G";

        [Required, RegularExpression("^#[0-9a-fA-F]{6}$")]
        public string Color { get; set; } = "#6c757d";

        [Range(0.01, 999_999_999_999)]
        public decimal? PresupuestoMensual { get; set; }
    }
}
