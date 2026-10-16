namespace PAW_Jueves.Models
{
    /// <summary>Respuesta de la API para el chat y el resumen.</summary>
    public class AsistenteRespuestaModel
    {
        public string? Texto { get; set; }
        public bool DesdeCache { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
