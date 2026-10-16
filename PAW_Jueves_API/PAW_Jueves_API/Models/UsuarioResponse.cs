namespace PAW_Jueves_API.Models
{
    /// <summary>
    /// Respuesta que devuelven los endpoints Login, Register y Forgot de la API.
    /// Hereda de <see cref="ResponseApi"/>, por eso además de estas propiedades tiene Mensaje.
    ///
    /// Se usa de dos formas:
    ///  1. Dapper la llena con el resultado de un procedimiento almacenado: copia cada columna a
    ///     la propiedad que se llame igual (columna IdUsuario → propiedad IdUsuario). Las columnas
    ///     que no tienen propiedad con su nombre (Identificacion, Estado...) simplemente se ignoran.
    ///  2. El controlador la crea a mano solo con Mensaje para responder errores, por ejemplo
    ///     new UsuarioResponse { Mensaje = "..." }.
    ///
    /// Al convertirse a JSON los nombres empiezan en minúscula:
    ///     { "idUsuario": 5, "nombreCompleto": "Ana Pérez", "mensaje": "" }
    /// </summary>
    public class UsuarioResponse : ResponseApi
    {
        /// <summary>
        /// Llave primaria del usuario en la tabla dbo.Usuario. Se agregó en esta clase para que
        /// Forgot pueda cifrarla y ponerla en el link de recuperación. Vale 0 cuando la respuesta
        /// es de error (porque int no puede ser vacío y su valor por defecto es 0).
        /// </summary>
        public int IdUsuario { get; set; }

        /// <summary>Nombre completo del usuario, tal como está guardado en la BD.</summary>
        public string NombreCompleto { get; set; } = string.Empty;
    }
}
