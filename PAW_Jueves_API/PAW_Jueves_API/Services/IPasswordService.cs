namespace PAW_Jueves_API.Services
{
    /// <summary>
    /// Contrato (interfaz) del servicio que genera y lee los tokens cifrados
    /// usados en el link de "Olvidé mi contraseña".
    ///
    /// Se usa una interfaz para aplicar Inversión de Dependencias: los controladores
    /// piden un IPasswordService en su constructor y ASP.NET Core les entrega la
    /// implementación registrada en Program.cs (PasswordService). Así el controlador
    /// no depende de la clase concreta y se podría cambiar por otra sin tocarlo.
    /// </summary>
    public interface IPasswordService
    {
        /// <summary>
        /// Cifra el id del usuario junto con una fecha de vencimiento y devuelve
        /// un token seguro para poner en una URL.
        /// </summary>
        /// <param name="userId">Id del usuario que pidió recuperar el acceso.</param>
        /// <param name="vigencia">Cuánto tiempo será válido el token (por ejemplo 1 hora).</param>
        /// <returns>Token cifrado en Base64 "URL-safe".</returns>
        string Cifrar(string userId, TimeSpan vigencia);

        /// <summary>
        /// Valida y descifra un token generado con <see cref="Cifrar"/>.
        /// </summary>
        /// <param name="token">Token recibido en el link.</param>
        /// <returns>
        /// El id del usuario si el token es auténtico y no ha vencido;
        /// string vacío si fue alterado, está mal formado o ya expiró.
        /// </returns>
        string Descifrar(string token);
    }
}
