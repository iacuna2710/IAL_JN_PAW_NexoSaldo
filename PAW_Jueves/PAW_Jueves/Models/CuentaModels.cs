using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    /// <summary>Perfil que devuelve GET /api/Usuario/Perfil. Se muestra en Mi cuenta y en Settings.</summary>
    public class PerfilModel
    {
        public int IdUsuario { get; set; }
        public string Identificacion { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string CorreoElectronico { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Moneda { get; set; } = "CRC";
        public string? FotoPerfil { get; set; }
    }

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

    /// <summary>Formulario "Cambiar contraseña" de Settings.</summary>
    public class CambiarContrasennaModel
    {
        [Required(ErrorMessage = "Escriba su contraseña actual")]
        public string ContrasennaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escriba la contraseña nueva")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña nueva debe tener al menos 8 caracteres")]
        public string ContrasennaNueva { get; set; } = string.Empty;

        [Compare(nameof(ContrasennaNueva), ErrorMessage = "La confirmación no coincide con la contraseña nueva")]
        public string ConfirmarContrasenna { get; set; } = string.Empty;
    }

    /// <summary>Formulario "Preferencias" de Settings.</summary>
    public class PreferenciasModel
    {
        [Required, RegularExpression("^(CRC|USD|EUR)$", ErrorMessage = "Moneda no válida")]
        public string Moneda { get; set; } = "CRC";
    }

    /// <summary>Respuesta de PUT /api/Usuario/Foto.</summary>
    public class FotoResponse
    {
        public string? FotoAnterior { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
