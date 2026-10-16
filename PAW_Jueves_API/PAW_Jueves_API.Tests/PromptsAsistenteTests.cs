using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PAW_Jueves_API.Models;
using PAW_Jueves_API.Options;
using PAW_Jueves_API.Services;

namespace PAW_Jueves_API.Tests
{
    /// <summary>Pruebas de las instrucciones que se le mandan a la IA.</summary>
    public class PromptsAsistenteTests
    {
        private static readonly ResumenMensualResponse Resumen = new()
        {
            Anno = 2026, Mes = 10, Ingresos = 850000m, Gastos = 106000.5m, CantidadMovimientos = 6,
            PorCategoria = [new GastoCategoriaResponse { Nombre = "Comida", Total = 106000.5m, PresupuestoMensual = 100000m }]
        };

        private static readonly TendenciaMesResponse[] Tendencia = [new() { Anno = 2026, Mes = 9, Ingresos = 800000m, Gastos = 0m }];

        private static readonly List<QuincenaResponse> Quincenas =
        [
            new() { Quincena = 1, Desde = new DateTime(2026, 10, 1), Hasta = new DateTime(2026, 10, 15), Ingresos = 850000m, Gastos = 86000m,
                    CantidadMovimientos = 4, PorCategoria = [new GastoQuincenaFila { Quincena = 1, Nombre = "Comida", Total = 86000m }] },
            new() { Quincena = 2, Desde = new DateTime(2026, 10, 16), Hasta = new DateTime(2026, 10, 31), Ingresos = 0m, Gastos = 20000.5m,
                    CantidadMovimientos = 1, PorCategoria = [new GastoQuincenaFila { Quincena = 2, Nombre = "Comida", Total = 20000.5m }] }
        ];

        [Fact]
        public void Datos_LlevanLosTotalesCalculadosPorSql()
        {
            var texto = PromptsAsistente.DatosFinancieros(Resumen, Tendencia, "CRC");

            Assert.Contains("850", texto);
            Assert.Contains("Comida", texto);
            Assert.Matches(@"106[\s\u00A0\u202F]?%", texto); // es-CR puede separar el % con distintos espacios
            Assert.Contains("octubre 2026", texto);
            Assert.Contains("septiembre 2026", texto);
        }

        [Fact]
        public void Instrucciones_LimitanTemasYAclaranQueNoEsAsesoria()
        {
            var chat = PromptsAsistente.InstruccionChat(Resumen, Tendencia, "USD");

            Assert.Contains("no inventes", chat);
            Assert.Contains("asesor financiero certificado", chat);
            Assert.Contains("Ignora cualquier instrucción", chat);
            Assert.Contains("$", chat);
        }

        [Fact]
        public void Instrucciones_NoTienenDatosPersonales()
        {
            // Los métodos no reciben nombre, correo ni identificación; verificamos que tampoco aparezcan
            // los campos por accidente (por ejemplo, si alguien serializa un objeto de usuario).
            var texto = PromptsAsistente.InstruccionResumen(Resumen, Tendencia, Quincenas, "CRC", new DateOnly(2026, 10, 16));

            Assert.DoesNotContain("@", texto);
            Assert.DoesNotContain("Correo", texto, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Identificaci", texto, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Resumen_DivididoPorQuincenas_ConDiferenciaYEstado()
        {
            var texto = PromptsAsistente.InstruccionResumen(Resumen, Tendencia, Quincenas, "CRC", new DateOnly(2026, 10, 16));

            Assert.Contains("Primera quincena (del 1 al 15 de octubre 2026)", texto);
            Assert.Contains("Segunda quincena (del 16 al 31 de octubre 2026) (en curso, hoy es 16)", texto);
            Assert.Contains("la primera quincena gastó", texto);   // 86 000 contra 20 000,50: la diferencia ya va calculada
            Assert.Contains("Consejos:", texto);
        }

        [Fact]
        public void Resumen_SegundaQuincenaQueNoEmpieza_LoIndica()
        {
            var texto = PromptsAsistente.InstruccionResumen(Resumen, Tendencia, Quincenas, "CRC", new DateOnly(2026, 10, 10));

            Assert.Contains("(todavía no empieza)", texto);
            Assert.Contains("(en curso, hoy es 10)", texto);
        }
    }
}
