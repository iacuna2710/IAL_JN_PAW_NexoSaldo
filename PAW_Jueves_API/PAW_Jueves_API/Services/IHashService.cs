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

    /// <summary>
    /// Hashea y verifica contraseñas. Se usa BCrypt: cada hash lleva su propia "sal" aleatoria,
    /// así que dos usuarios con la misma contraseña tienen hashes distintos, y es lento a
    /// propósito para que adivinar contraseñas por fuerza bruta sea muy costoso.
    /// </summary>
    public interface IHashService
    {
        /// <summary>Devuelve el hash de BCrypt (60 caracteres, empieza con "$2").</summary>
        string Hashear(string contrasenna);

        /// <summary>Compara la contraseña escrita con lo que hay guardado en la BD.</summary>
        ResultadoVerificacion Verificar(string contrasenna, string guardada);
    }
}
