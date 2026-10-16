namespace PAW_Jueves.Models
{
    /// <summary>Respuesta de PUT /api/Usuario/Foto.</summary>
    public class FotoResponse
    {
        public string? FotoAnterior { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}
