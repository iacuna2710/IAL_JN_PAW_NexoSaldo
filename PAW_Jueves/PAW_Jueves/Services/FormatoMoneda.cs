using System.Globalization;

namespace PAW_Jueves.Services
{
    /// <summary>
    /// Formatea montos y meses para mostrarlos. Siempre usa la cultura es-CR (separador de miles con
    /// espacio y decimales con coma) sin importar la configuración del servidor. Los montos NO se
    /// convierten entre monedas: la moneda preferida solo cambia el símbolo.
    /// </summary>
    public static class FormatoMoneda
    {
        private static readonly CultureInfo CulturaCostaRica = CultureInfo.GetCultureInfo("es-CR");

        public static string Simbolo(string? moneda) => moneda switch
        {
            "USD" => "$",
            "EUR" => "€",
            _ => "₡"
        };

        /// <summary>Ejemplo: Mostrar(12500.5m, "CRC") → "₡12 500,50".</summary>
        public static string Mostrar(decimal monto, string? moneda) =>
            (monto < 0 ? "-" : "") + Simbolo(moneda) + Math.Abs(monto).ToString("N2", CulturaCostaRica);

        /// <summary>Ejemplo: NombreMes(2026, 10) → "Octubre 2026".</summary>
        public static string NombreMes(int anno, int mes)
        {
            var texto = new DateTime(anno, mes, 1).ToString("MMMM yyyy", CulturaCostaRica);
            return char.ToUpper(texto[0], CulturaCostaRica) + texto[1..];
        }

        /// <summary>Nombre corto del mes para los gráficos. Ejemplo: "oct".</summary>
        public static string MesCorto(int anno, int mes) =>
            new DateTime(anno, mes, 1).ToString("MMM", CulturaCostaRica).TrimEnd('.');

        /// <summary>
        /// Convierte "2026-10" (valor de un input type="month") en año y mes. Si viene vacío o mal
        /// formado, devuelve el mes de hoy.
        /// </summary>
        public static (int Anno, int Mes) LeerPeriodo(string? periodo, DateOnly hoy)
        {
            if (!string.IsNullOrWhiteSpace(periodo)
                && DateOnly.TryParseExact(periodo + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
                && fecha.Year is >= 2000 and <= 2100)
                return (fecha.Year, fecha.Month);

            return (hoy.Year, hoy.Month);
        }

        /// <summary>Fecha de hoy en Costa Rica.</summary>
        public static DateOnly HoyCostaRica() => DateOnly.FromDateTime(FormatoFecha.ALocal(DateTime.UtcNow));
    }
}
