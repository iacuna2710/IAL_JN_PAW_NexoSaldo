namespace PAW_Jueves_API.Services
{
    /// <summary>Resultado de comparar la contraseña escrita con la guardada en la BD.</summary>
    public enum ResultadoVerificacion
    {
        /// <summary>No coincide.</summary>
        Incorrecta,

        /// <summary>Coincide con el hash de BCrypt guardado.</summary>
        Correcta,

        /// <summary>
        /// Coincide, pero la BD todavía la tenía en texto plano (usuarios creados antes de BCrypt).
        /// Quien llama debe guardar el hash nuevo para dejar de tener la contraseña en claro.
        /// </summary>
        CorrectaRequiereHash
    }
}
