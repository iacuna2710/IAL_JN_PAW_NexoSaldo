namespace PAW_Jueves_API.Models
{
    /// <summary>Datos del usuario autenticado que devuelve GET /api/Usuario/Perfil.</summary>
    public class PerfilResponse : ResponseApi
    {
        public int IdUsuario { get; set; }
        public string Identificacion { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string CorreoElectronico { get; set; } = string.Empty;
    }
}
