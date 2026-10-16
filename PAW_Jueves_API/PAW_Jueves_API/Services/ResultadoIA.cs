namespace PAW_Jueves_API.Services
{
    public sealed record ResultadoIA(string? Texto, ErrorGemini Error)
    {
        public bool Exitoso => Error == ErrorGemini.Ninguno && !string.IsNullOrWhiteSpace(Texto);
    }
}
