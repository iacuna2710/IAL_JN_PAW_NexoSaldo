using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
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
}
