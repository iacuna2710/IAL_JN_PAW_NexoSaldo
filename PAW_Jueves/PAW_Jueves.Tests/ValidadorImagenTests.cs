using Microsoft.AspNetCore.Http;
using PAW_Jueves.Services;

namespace PAW_Jueves.Tests
{
    /// <summary>Pruebas de la validación de la foto de perfil (tipo real, extensión y tamaño).</summary>
    public class ValidadorImagenTests
    {
        private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1];
        private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D];
        private static readonly byte[] Webp = [.. "RIFF"u8, 0x24, 0, 0, 0, .. "WEBP"u8];
        private static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0, 3, 0, 0, 0, 4, 0, 0, 0];

        private static IFormFile Archivo(string nombre, byte[] cabecera, long tamannoTotal = 0)
        {
            var contenido = new byte[Math.Max(tamannoTotal, cabecera.Length)];
            cabecera.CopyTo(contenido, 0);
            return new FormFile(new MemoryStream(contenido), 0, contenido.Length, "foto", nombre);
        }

        [Theory]
        [InlineData("foto.jpg", ".jpg")]
        [InlineData("FOTO.JPEG", ".jpg")]
        public async Task Jpeg_EsValido(string nombre, string extension)
        {
            var resultado = await ValidadorImagen.ValidarAsync(Archivo(nombre, Jpeg));

            Assert.True(resultado.EsValida);
            Assert.Equal(extension, resultado.Extension);
        }

        [Fact]
        public async Task Png_EsValido() =>
            Assert.True((await ValidadorImagen.ValidarAsync(Archivo("foto.png", Png))).EsValida);

        [Fact]
        public async Task Webp_EsValido() =>
            Assert.True((await ValidadorImagen.ValidarAsync(Archivo("foto.webp", Webp))).EsValida);

        [Fact]
        public async Task ExeRenombradoAPng_SeRechaza()
        {
            var resultado = await ValidadorImagen.ValidarAsync(Archivo("virus.png", Exe));

            Assert.False(resultado.EsValida);
            Assert.Equal("El archivo no es una imagen válida", resultado.Error);
        }

        [Fact]
        public async Task PngConExtensionJpg_SeRechaza() =>
            Assert.False((await ValidadorImagen.ValidarAsync(Archivo("foto.jpg", Png))).EsValida);

        [Theory]
        [InlineData("foto.gif")]
        [InlineData("foto.svg")]
        [InlineData("foto")]
        public async Task ExtensionNoPermitida_SeRechaza(string nombre) =>
            Assert.False((await ValidadorImagen.ValidarAsync(Archivo(nombre, Png))).EsValida);

        [Fact]
        public async Task MasDe2MB_SeRechaza()
        {
            var resultado = await ValidadorImagen.ValidarAsync(Archivo("grande.png", Png, ValidadorImagen.TamannoMaximo + 1));

            Assert.False(resultado.EsValida);
            Assert.Contains("2 MB", resultado.Error);
        }

        [Fact]
        public async Task SinArchivo_SeRechaza() =>
            Assert.False((await ValidadorImagen.ValidarAsync(null)).EsValida);
    }
}
