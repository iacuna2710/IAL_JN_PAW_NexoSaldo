namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Respuesta de POST /api/Home/Login cuando las credenciales son correctas.
    /// JSON: { "token": "eyJ...", "idUsuario": 5, "nombreCompleto": "Ana Pérez", "mensaje": "" }
    /// La WEB guarda el token en la sesión y lo manda en cada llamada a endpoints protegidos.
    /// </summary>
    public class LoginResponse : ResponseApi
    {
        public string Token { get; set; } = string.Empty;
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
    }
}
