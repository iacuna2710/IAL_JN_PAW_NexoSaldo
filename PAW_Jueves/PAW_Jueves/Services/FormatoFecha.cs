namespace PAW_Jueves.Services
{
    /// <summary>Convierte fechas UTC (como se guardan en la BD) a la hora local de Costa Rica.</summary>
    public static class FormatoFecha
    {
        // "America/Costa_Rica" funciona en Windows y Linux (.NET usa ICU para traducir el nombre)
        private static readonly TimeZoneInfo ZonaLocal = BuscarZona("America/Costa_Rica");

        public static DateTime ALocal(DateTime fechaUtc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc), ZonaLocal);

        /// <summary>Ejemplo: "16/10/2026 08:15".</summary>
        public static string Mostrar(DateTime fechaUtc) => ALocal(fechaUtc).ToString("dd/MM/yyyy HH:mm");

        private static TimeZoneInfo BuscarZona(string id)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Respaldo: Costa Rica es UTC-6 todo el año (no tiene horario de verano)
                return TimeZoneInfo.CreateCustomTimeZone(id, TimeSpan.FromHours(-6), id, id);
            }
        }
    }
}
