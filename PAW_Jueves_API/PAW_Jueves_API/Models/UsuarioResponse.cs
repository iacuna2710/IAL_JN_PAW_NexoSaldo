namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Datos del usuario que la API devuelve en Login, Register y Forgot.
    /// Hereda Mensaje de <see cref="ResponseApi"/>.
    /// Dapper llena las propiedades automáticamente cuando el nombre coincide
    /// con el de una columna que devuelve el procedimiento almacenado.
    /// </summary>
    public class UsuarioResponse : ResponseApi
    {
        /// <summary>
        /// Llave primaria del usuario en la tabla Usuario. Se agregó para poder
        /// cifrarla en el link de recuperación de acceso.
        /// </summary>
        public int IdUsuario { get; set; }

        /// <summary>Nombre completo del usuario.</summary>
        public string NombreCompleto { get; set; } = string.Empty;
    }
}
