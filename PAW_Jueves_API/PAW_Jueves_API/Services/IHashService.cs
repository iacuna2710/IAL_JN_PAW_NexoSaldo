namespace PAW_Jueves_API.Services
{
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
