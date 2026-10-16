namespace PAW_Jueves_API.Models
{
    /// <summary>Una fila de la bitácora, tal como la devuelve GET /api/Usuario/Actividad.</summary>
    public class ActividadResponse
    {
        public string Accion { get; set; } = string.Empty;
        public string? Detalle { get; set; }
        public string? Ip { get; set; }

        /// <summary>Fecha y hora en UTC.</summary>
        public DateTime Fecha { get; set; }
    }

    /// <summary>Cuerpo de POST /api/Usuario/VerificarContrasenna (desbloqueo de pantalla).</summary>
    public class VerificarContrasennaRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Contrasenna { get; set; } = string.Empty;
    }
}
