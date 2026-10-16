using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>Cuerpo de POST /api/movimientos y PUT /api/movimientos/{id}.</summary>
    public class MovimientoRequest
    {
        [Range(1, int.MaxValue)]
        public int IdCategoria { get; set; }

        [Required, RegularExpression("^[IG]$")]
        public string Tipo { get; set; } = "G";

        public decimal Monto { get; set; }

        public DateOnly Fecha { get; set; }

        [StringLength(250)]
        public string? Descripcion { get; set; }
    }
}
