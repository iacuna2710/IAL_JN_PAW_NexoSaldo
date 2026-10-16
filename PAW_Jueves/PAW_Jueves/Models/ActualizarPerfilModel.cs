using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>Formulario "Datos personales" de Mi cuenta.</summary>
    public class ActualizarPerfilModel
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(250, ErrorMessage = "El nombre no puede tener más de 250 caracteres")]
        public string NombreCompleto { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "El teléfono no puede tener más de 20 caracteres")]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "El teléfono solo puede tener números, espacios, +, - y paréntesis")]
        public string? Telefono { get; set; }
    }
}
