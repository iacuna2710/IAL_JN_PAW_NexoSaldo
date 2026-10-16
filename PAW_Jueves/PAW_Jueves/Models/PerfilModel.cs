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
}
