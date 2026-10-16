namespace PAW_Jueves_API.Services
{
    /// <summary>Un mensaje de la conversación. Rol: "user" (el usuario) o "model" (la IA).</summary>
    public sealed record MensajeIA(string Rol, string Texto);
}
