namespace PAW_Jueves_API.Models
{
    /// <summary>Respuesta de PUT /api/Usuario/Foto: la foto que había antes, para que la WEB la borre.</summary>
    public class FotoResponse : ResponseApi
    {
        public string? FotoAnterior { get; set; }
    }
}
