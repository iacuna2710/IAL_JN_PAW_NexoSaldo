namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Fila que devuelve sp_ConsultarCredenciales. Es de uso INTERNO de la API: trae la contraseña
    /// guardada (hash), así que nunca se devuelve tal cual al cliente.
    /// </summary>
    public class CredencialesUsuario
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? FotoPerfil { get; set; }
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Contrasenna { get; set; } = string.Empty;
    }
}
