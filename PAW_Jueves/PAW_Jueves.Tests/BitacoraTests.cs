using PAW_Jueves.Services;

namespace PAW_Jueves.Tests
{
    /// <summary>Pruebas de cómo se muestran las acciones de la bitácora y sus fechas.</summary>
    public class BitacoraTests
    {
        [Fact]
        public void FechaUtc_SeMuestraEnHoraDeCostaRica()
        {
            // 16/10/2026 14:30 UTC = 08:30 en Costa Rica (UTC-6, sin horario de verano)
            Assert.Equal("16/10/2026 08:30", FormatoFecha.Mostrar(new DateTime(2026, 10, 16, 14, 30, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void FechaSinZona_SeTrataComoUtc()
        {
            Assert.Equal("01/01/2026 18:00", FormatoFecha.Mostrar(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Unspecified)));
        }

        [Theory]
        [InlineData("InicioSesion", "Inicio de sesión", false)]
        [InlineData("InicioSesionFallido", "Intento de inicio de sesión fallido", true)]
        [InlineData("DesbloqueoFallido", "Intento fallido de desbloqueo", true)]
        [InlineData("CambioFoto", "Cambió su foto de perfil", false)]
        public void AccionConocida_TieneTextoYAlerta(string accion, string texto, bool alerta)
        {
            Assert.Equal(texto, AccionesBitacora.Texto(accion));
            Assert.Equal(alerta, AccionesBitacora.EsAlerta(accion));
            Assert.StartsWith("bi-", AccionesBitacora.Icono(accion));
        }

        [Fact]
        public void AccionDesconocida_SeMuestraTalCual()
        {
            Assert.Equal("AccionNueva", AccionesBitacora.Texto("AccionNueva"));
            Assert.False(AccionesBitacora.EsAlerta("AccionNueva"));
        }
    }
}
