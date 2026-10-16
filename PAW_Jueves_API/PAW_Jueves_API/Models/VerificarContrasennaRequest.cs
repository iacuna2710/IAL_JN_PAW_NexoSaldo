namespace PAW_Jueves_API.Models
{
    /// <summary>Cuerpo de POST /api/Usuario/VerificarContrasenna (desbloqueo de pantalla).</summary>
    public class VerificarContrasennaRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Contrasenna { get; set; } = string.Empty;
    }
}
