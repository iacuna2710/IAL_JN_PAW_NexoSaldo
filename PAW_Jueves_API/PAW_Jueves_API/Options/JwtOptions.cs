using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Options
{
    /// <summary>
    /// Configuración del token JWT que entrega la API al iniciar sesión. Se llena con la sección
    /// "Jwt" de appsettings (Options pattern), en vez de leer cada valor con GetValue por todo el código.
    ///
    /// La Clave NUNCA va en appsettings.json: en desarrollo está en appsettings.Development.json y
    /// en cualquier otro ambiente se pasa por variable de entorno (Jwt__Clave). Si falta, la API
    /// no arranca (ValidateOnStart en Program.cs).
    /// </summary>
    public sealed class JwtOptions
    {
        public const string Seccion = "Jwt";

        /// <summary>Quién emite el token (esta API).</summary>
        [Required]
        public string Emisor { get; init; } = string.Empty;

        /// <summary>Para quién es el token (la WEB).</summary>
        [Required]
        public string Audiencia { get; init; } = string.Empty;

        /// <summary>Clave secreta para firmar el token (HMAC-SHA256). Mínimo 32 caracteres.</summary>
        [Required, MinLength(32)]
        public string Clave { get; init; } = string.Empty;

        /// <summary>Minutos que dura el token. Igual que la sesión de la WEB.</summary>
        [Range(5, 1440)]
        public int MinutosExpiracion { get; init; } = 60;
    }
}
