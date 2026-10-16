namespace PAW_Jueves_API.Services
{
    /// <summary>
    /// Interfaz (contrato) del servicio que convierte el id de un usuario en un token cifrado
    /// para el link de "Olvidé mi contraseña", y que luego convierte ese token de vuelta en el id.
    ///
    /// Una interfaz solo declara QUÉ métodos existen, no CÓMO funcionan. La clase que hace el
    /// trabajo real es PasswordService. La relación entre ambas se configura en Program.cs con:
    ///     builder.Services.AddScoped&lt;IPasswordService, PasswordService&gt;();
    /// Gracias a eso, cuando un controlador escribe IPasswordService en su constructor,
    /// ASP.NET Core le entrega automáticamente un objeto PasswordService. A esto se le llama
    /// Inversión de Dependencias: el controlador depende de la interfaz y no de la clase concreta,
    /// así que se podría cambiar PasswordService por otra implementación sin tocar el controlador.
    /// </summary>
    public interface IPasswordService
    {
        /// <summary>
        /// Convierte el id del usuario en un token cifrado que vence después del tiempo indicado.
        /// </summary>
        /// <param name="userId">Id del usuario como texto, por ejemplo "5".</param>
        /// <param name="vigencia">
        /// Tiempo durante el cual el token será válido, contado desde el momento en que se llama
        /// a este método. Ejemplo: TimeSpan.FromHours(1) = válido por 1 hora.
        /// </param>
        /// <returns>
        /// Token de texto que se puede poner directamente al final de una URL
        /// (solo contiene letras, números, '-' y '_').
        /// </returns>
        string Cifrar(string userId, TimeSpan vigencia);

        /// <summary>
        /// Hace lo contrario de <see cref="Cifrar"/>: recibe el token y devuelve el id del usuario,
        /// pero solo si el token es válido.
        /// </summary>
        /// <param name="token">El token que venía al final del link de recuperación.</param>
        /// <returns>
        /// - El id del usuario (por ejemplo "5") si el token fue generado por este sistema,
        ///   nadie lo modificó y todavía no venció.
        /// - Un string vacío ("") en cualquier otro caso: token modificado, incompleto, inventado
        ///   o vencido. Quien llame a este método debe revisar si el resultado está vacío.
        /// </returns>
        string Descifrar(string token);
    }
}
