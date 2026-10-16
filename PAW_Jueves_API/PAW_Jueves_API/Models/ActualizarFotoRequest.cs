using System.ComponentModel.DataAnnotations;

namespace PAW_Jueves_API.Models
{
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
}
