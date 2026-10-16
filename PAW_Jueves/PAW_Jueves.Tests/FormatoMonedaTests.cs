using PAW_Jueves.Services;

namespace PAW_Jueves.Tests
{
    /// <summary>Pruebas del formato de montos, meses y del filtro de periodo.</summary>
    public class FormatoMonedaTests
    {
        private static readonly DateOnly Hoy = new(2026, 10, 16);

        [Theory]
        [InlineData("CRC", "₡")]
        [InlineData("USD", "$")]
        [InlineData("EUR", "€")]
        [InlineData(null, "₡")]
        public void Simbolo_SegunMoneda(string? moneda, string simbolo) =>
            Assert.Equal(simbolo, FormatoMoneda.Simbolo(moneda));

        [Fact]
        public void Mostrar_UsaFormatoDeCostaRica()
        {
            var texto = FormatoMoneda.Mostrar(12500.5m, "CRC");

            Assert.StartsWith("₡", texto);
            Assert.EndsWith(",50", texto); // coma decimal
            Assert.Contains("12", texto);
        }

        [Fact]
        public void Mostrar_Negativo_LlevaSignoAntesDelSimbolo() =>
            Assert.StartsWith("-$", FormatoMoneda.Mostrar(-10m, "USD"));

        [Fact]
        public void NombreMes_EnEspannolConMayuscula() =>
            Assert.Equal("Octubre 2026", FormatoMoneda.NombreMes(2026, 10));

        [Theory]
        [InlineData("2026-03", 2026, 3)]
        [InlineData("2025-12", 2025, 12)]
        [InlineData(null, 2026, 10)]
        [InlineData("", 2026, 10)]
        [InlineData("basura", 2026, 10)]
        [InlineData("2026-13", 2026, 10)]
        [InlineData("1990-01", 2026, 10)]
        public void LeerPeriodo_ValidoOMesActual(string? periodo, int anno, int mes) =>
            Assert.Equal((anno, mes), FormatoMoneda.LeerPeriodo(periodo, Hoy));
    }
}
