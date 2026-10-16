using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
    /// <summary>Cuerpo de PUT /api/Usuario/Perfil. El correo y la identificación no se pueden cambiar.</summary>
    public class ActualizarPerfilRequest
    {
        [Required, StringLength(250)]
        public string NombreCompleto { get; set; } = string.Empty;

        [StringLength(20), RegularExpression(@"^[0-9+\-\s()]*$")]
        public string? Telefono { get; set; }
    }

    /// <summary>Cuerpo de PUT /api/Usuario/Preferencias.</summary>
    public class PreferenciasRequest
    {
        [Required, RegularExpression("^(CRC|USD|EUR)$")]
        public string Moneda { get; set; } = "CRC";
    }

    /// <summary>
    /// Cuerpo de PUT /api/Usuario/Foto. La WEB ya guardó el archivo; aquí solo llega su ruta.
    /// La expresión regular solo acepta rutas que genera la WEB (/uploads/perfiles/{guid}.ext):
    /// así nadie puede guardar como "foto" una URL de otro sitio o una ruta rara.
    /// </summary>
    public class ActualizarFotoRequest
    {
        [Required, RegularExpression(@"^/uploads/perfiles/[a-f0-9]{32}\.(jpg|png|webp)$")]
        public string FotoPerfil { get; set; } = string.Empty;
    }

    /// <summary>Respuesta de PUT /api/Usuario/Foto: la foto que había antes, para que la WEB la borre.</summary>
    public class FotoResponse : ResponseApi
    {
        public string? FotoAnterior { get; set; }
    }

    /// <summary>Cuerpo de PUT /api/Usuario/Contrasenna.</summary>
    public class CambiarContrasennaRequest
    {
        [Required]
        public string ContrasennaActual { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string ContrasennaNueva { get; set; } = string.Empty;
    }
}
