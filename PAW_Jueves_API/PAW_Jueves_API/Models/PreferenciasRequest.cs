using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>Cuerpo de PUT /api/Usuario/Preferencias.</summary>
    public class PreferenciasRequest
    {
        [Required, RegularExpression("^(CRC|USD|EUR)$")]
        public string Moneda { get; set; } = "CRC";
    }
}
