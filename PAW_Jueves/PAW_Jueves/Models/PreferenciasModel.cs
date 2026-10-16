using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>Formulario "Preferencias" de Settings.</summary>
    public class PreferenciasModel
    {
        [Required, RegularExpression("^(CRC|USD|EUR)$", ErrorMessage = "Moneda no válida")]
        public string Moneda { get; set; } = "CRC";
    }
}
