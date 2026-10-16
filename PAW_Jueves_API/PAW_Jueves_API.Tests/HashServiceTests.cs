using PAW_Jueves_API.Services;

namespace PAW_Jueves_API.Tests
{
    /// <summary>Pruebas del hash de contraseñas con BCrypt y de la migración de usuarios viejos.</summary>
    public class HashServiceTests
    {
        private readonly HashService _servicio = new();

        [Fact]
        public void Hashear_NoGuardaLaContrasennaEnTextoPlano()
        {
            var hash = _servicio.Hashear("MiClave123");

            Assert.StartsWith("$2", hash);
            Assert.DoesNotContain("MiClave123", hash);
            Assert.True(hash.Length <= 100, "Debe caber en la columna Contrasenna NVARCHAR(100)");
        }

        [Fact]
        public void Hashear_DosVecesLaMismaClave_DaHashesDistintos()
        {
            Assert.NotEqual(_servicio.Hashear("igual"), _servicio.Hashear("igual"));
        }

        [Fact]
        public void Verificar_ClaveCorrectaContraHash_EsCorrecta()
        {
            var hash = _servicio.Hashear("MiClave123");

            Assert.Equal(ResultadoVerificacion.Correcta, _servicio.Verificar("MiClave123", hash));
        }

        [Fact]
        public void Verificar_ClaveIncorrectaContraHash_EsIncorrecta()
        {
            var hash = _servicio.Hashear("MiClave123");

            Assert.Equal(ResultadoVerificacion.Incorrecta, _servicio.Verificar("otra", hash));
        }

        [Fact]
        public void Verificar_UsuarioViejoEnTextoPlano_PideGuardarElHash()
        {
            Assert.Equal(ResultadoVerificacion.CorrectaRequiereHash, _servicio.Verificar("90415", "90415"));
        }

        [Theory]
        [InlineData("90416", "90415")]
        [InlineData("", "90415")]
        [InlineData("90415", "")]
        [InlineData("x", "$2esto-no-es-un-hash")]
        public void Verificar_CasosQueNoCoinciden_SonIncorrectos(string escrita, string guardada)
        {
            Assert.Equal(ResultadoVerificacion.Incorrecta, _servicio.Verificar(escrita, guardada));
        }
    }
}
