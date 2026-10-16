namespace PAW_Jueves.Services
{
    /// <summary>Resultado de validar una imagen subida. Extension es la extensión real detectada (.jpg, .png, .webp).</summary>
    public sealed record ResultadoImagen(bool EsValida, string Extension, string Error)
    {
        public static ResultadoImagen Invalida(string error) => new(false, string.Empty, error);
    }

    /// <summary>
    /// Valida la foto de perfil antes de guardarla. No alcanza con mirar la extensión (cualquiera puede
    /// renombrar un .exe a .png), así que también se leen los primeros bytes del archivo ("firma" o
    /// magic bytes), que identifican el formato real:
    ///  - JPEG: FF D8 FF
    ///  - PNG:  89 50 4E 47 0D 0A 1A 0A
    ///  - WEBP: "RIFF" (4 bytes) + tamaño (4 bytes) + "WEBP"
    /// </summary>
    public static class ValidadorImagen
    {
        /// <summary>Tamaño máximo permitido: 2 MB.</summary>
        public const long TamannoMaximo = 2 * 1024 * 1024;

        // Extensión del nombre del archivo → extensión con la que se guarda
        private static readonly Dictionary<string, string> Extensiones = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ".jpg",
            [".jpeg"] = ".jpg",
            [".png"] = ".png",
            [".webp"] = ".webp"
        };

        public static async Task<ResultadoImagen> ValidarAsync(IFormFile? archivo, CancellationToken cancellationToken = default)
        {
            if (archivo == null || archivo.Length == 0)
                return ResultadoImagen.Invalida("Seleccione una imagen");

            if (archivo.Length > TamannoMaximo)
                return ResultadoImagen.Invalida("La imagen no puede pesar más de 2 MB");

            if (!Extensiones.TryGetValue(Path.GetExtension(archivo.FileName), out var extension))
                return ResultadoImagen.Invalida("Formato no permitido. Use JPG, PNG o WEBP");

            var cabecera = new byte[12];
            await using (var stream = archivo.OpenReadStream())
            {
                var leidos = await stream.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false, cancellationToken);
                if (leidos < cabecera.Length)
                    return ResultadoImagen.Invalida("El archivo no es una imagen válida");
            }

            var detectada = DetectarFormato(cabecera);
            if (detectada == null || detectada != extension)
                return ResultadoImagen.Invalida("El archivo no es una imagen válida");

            return new ResultadoImagen(true, detectada, string.Empty);
        }

        /// <summary>Devuelve ".jpg", ".png" o ".webp" según los primeros bytes, o null si no es ninguno.</summary>
        public static string? DetectarFormato(ReadOnlySpan<byte> cabecera)
        {
            if (cabecera.Length >= 3 && cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF)
                return ".jpg";

            if (cabecera.Length >= 8 && cabecera[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                return ".png";

            if (cabecera.Length >= 12 && cabecera[..4].SequenceEqual("RIFF"u8) && cabecera[8..12].SequenceEqual("WEBP"u8))
                return ".webp";

            return null;
        }
    }
}
