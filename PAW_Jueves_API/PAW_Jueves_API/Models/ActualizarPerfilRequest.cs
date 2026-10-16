using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>Cuerpo de PUT /api/Usuario/Perfil. El correo y la identificación no se pueden cambiar.</summary>
    public class ActualizarPerfilRequest
    {
        [Required, StringLength(250)]
        public string NombreCompleto { get; set; } = string.Empty;

        [StringLength(20), RegularExpression(@"^[0-9+\-\s()]*$")]
        public string? Telefono { get; set; }
    }
}
