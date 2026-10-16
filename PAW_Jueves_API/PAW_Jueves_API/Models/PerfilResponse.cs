namespace PAW_Jueves_API.Models
{
    /// <summary>Datos del usuario autenticado que devuelve GET /api/Usuario/Perfil.</summary>
    public class PerfilResponse : ResponseApi
    {
        public int IdUsuario { get; set; }
        public string Identificacion { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string CorreoElectronico { get; set; } = string.Empty;
        public string? Telefono { get; set; }

        /// <summary>Moneda preferida para mostrar montos: CRC, USD o EUR.</summary>
        public string Moneda { get; set; } = "CRC";

        /// <summary>Ruta de la foto en la WEB (por ejemplo /uploads/perfiles/abc.png) o null si no tiene.</summary>
        public string? FotoPerfil { get; set; }
    }
}
