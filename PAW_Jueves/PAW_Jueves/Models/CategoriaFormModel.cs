using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>Formulario de crear o editar una categoría.</summary>
    public class CategoriaFormModel
    {
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required, RegularExpression("^[IG]$", ErrorMessage = "Tipo no válido")]
        public string Tipo { get; set; } = "G";

        [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Color no válido")]
        public string Color { get; set; } = "#4dabf7";

        [Range(0.01, 999_999_999_999, ErrorMessage = "El presupuesto debe ser mayor que 0")]
        public decimal? PresupuestoMensual { get; set; }

        public bool Estado { get; set; } = true;
    }
}
