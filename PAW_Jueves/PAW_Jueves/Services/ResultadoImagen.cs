namespace PAW_Jueves.Services
{
    /// <summary>Resultado de validar una imagen subida. Extension es la extensión real detectada (.jpg, .png, .webp).</summary>
    public sealed record ResultadoImagen(bool EsValida, string Extension, string Error)
    {
        public static ResultadoImagen Invalida(string error) => new(false, string.Empty, error);
    }
}
