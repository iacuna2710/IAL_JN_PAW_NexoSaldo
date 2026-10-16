using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves.Models
{
    public class ForgotRequestModel
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        public string CorreoElectronico { get; set; } = string.Empty;
    }
}
