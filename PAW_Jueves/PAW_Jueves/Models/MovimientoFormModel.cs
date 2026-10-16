using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>Formulario de crear o editar un movimiento.</summary>
    public class MovimientoFormModel
    {
        public int IdMovimiento { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Elija una categoría")]
        public int IdCategoria { get; set; }

        [Required, RegularExpression("^[IG]$", ErrorMessage = "Tipo no válido")]
        public string Tipo { get; set; } = "G";

        [Range(0.01, 999_999_999, ErrorMessage = "El monto debe ser mayor que 0")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        public DateOnly Fecha { get; set; }

        [StringLength(250, ErrorMessage = "La descripción no puede tener más de 250 caracteres")]
        public string? Descripcion { get; set; }
    }
}
