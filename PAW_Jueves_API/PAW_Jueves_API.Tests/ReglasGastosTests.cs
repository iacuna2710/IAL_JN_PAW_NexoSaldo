using PAW_Jueves_API.Models;
using PAW_Jueves_API.Services;

namespace PAW_Jueves_API.Tests
{
    /// <summary>Pruebas de las reglas de negocio de gastos: validación de movimientos y alertas de presupuesto.</summary>
    public class ReglasGastosTests
    {
        private static readonly DateOnly Hoy = new(2026, 10, 16);

        private static CategoriaResponse Comida(bool activa = true, decimal? presupuesto = null) =>
            new() { IdCategoria = 1, Nombre = "Comida", Tipo = "G", Estado = activa, PresupuestoMensual = presupuesto };

        private static MovimientoRequest Gasto(decimal monto = 5000m, DateOnly? fecha = null) =>
            new() { IdCategoria = 1, Tipo = "G", Monto = monto, Fecha = fecha ?? Hoy };

        [Fact]
        public void MovimientoValido_NoTieneError() =>
            Assert.Null(ReglasGastos.ValidarMovimiento(Gasto(), Comida(), Hoy));

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void MontoCeroONegativo_EsInvalido(decimal monto) =>
            Assert.Equal("El monto debe ser mayor que 0", ReglasGastos.ValidarMovimiento(Gasto(monto), Comida(), Hoy));

        [Fact]
        public void MontoConTresDecimales_EsInvalido() =>
            Assert.NotNull(ReglasGastos.ValidarMovimiento(Gasto(10.555m), Comida(), Hoy));

        [Fact]
        public void MontoGigante_EsInvalido() =>
            Assert.NotNull(ReglasGastos.ValidarMovimiento(Gasto(ReglasGastos.MontoMaximo + 1), Comida(), Hoy));

        [Fact]
        public void FechaFutura_EsInvalida() =>
            Assert.Equal("La fecha no puede ser futura", ReglasGastos.ValidarMovimiento(Gasto(fecha: Hoy.AddDays(1)), Comida(), Hoy));

        [Fact]
        public void FechaDeHaceMasDe5Annos_EsInvalida() =>
            Assert.NotNull(ReglasGastos.ValidarMovimiento(Gasto(fecha: Hoy.AddYears(-5).AddDays(-1)), Comida(), Hoy));

        [Fact]
        public void CategoriaAjenaOInexistente_EsInvalida() =>
            Assert.Equal("La categoría no existe", ReglasGastos.ValidarMovimiento(Gasto(), null, Hoy));

        [Fact]
        public void CategoriaDesactivada_EsInvalida() =>
            Assert.Equal("La categoría está desactivada", ReglasGastos.ValidarMovimiento(Gasto(), Comida(activa: false), Hoy));

        [Fact]
        public void GastoEnCategoriaDeIngresos_EsInvalido()
        {
            var salario = new CategoriaResponse { IdCategoria = 2, Nombre = "Salario", Tipo = "I", Estado = true };

            Assert.Contains("categoría de gastos", ReglasGastos.ValidarMovimiento(Gasto(), salario, Hoy));
        }

        [Theory]
        [InlineData(0, null, null)]
        [InlineData(50000, null, null)]          // sin presupuesto: nunca alerta
        [InlineData(79999, 100000.0, null)]        // 79,999 %
        [InlineData(80000, 100000.0, "advertencia")]
        [InlineData(99999.99, 100000.0, "advertencia")]
        [InlineData(100000, 100000.0, "excedido")]
        [InlineData(150000, 100000.0, "excedido")]
        public void AlertaPresupuesto_SegunPorcentaje(decimal gastado, double? presupuesto, string? esperada) =>
            Assert.Equal(esperada, ReglasGastos.AlertaPresupuesto(gastado, (decimal?)presupuesto));

        [Fact]
        public void MensajeAlerta_Excedido_MencionaLaCategoria() =>
            Assert.Contains("Superó el presupuesto de Comida", ReglasGastos.MensajeAlerta("excedido", "Comida", 120m, 100m));
    }
}
