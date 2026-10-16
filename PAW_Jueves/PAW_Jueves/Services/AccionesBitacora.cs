namespace PAW_Jueves.Services
{
    /// <summary>
    /// Traduce el código de cada acción de la bitácora (lo guarda la API) a un texto y un ícono de
    /// Bootstrap Icons para mostrarlo. Si llega un código desconocido se muestra tal cual.
    /// </summary>
    public static class AccionesBitacora
    {
        private static readonly Dictionary<string, (string Texto, string Icono, bool Alerta)> Acciones = new()
        {
            ["InicioSesion"] = ("Inicio de sesión", "bi-box-arrow-in-right", false),
            ["InicioSesionFallido"] = ("Intento de inicio de sesión fallido", "bi-exclamation-triangle", true),
            ["CambioPerfil"] = ("Actualizó sus datos personales", "bi-person", false),
            ["CambioFoto"] = ("Cambió su foto de perfil", "bi-image", false),
            ["CambioPreferencias"] = ("Cambió sus preferencias", "bi-sliders", false),
            ["CambioContrasenna"] = ("Cambió su contraseña", "bi-key", false),
            ["CambioContrasennaFallido"] = ("Intento fallido de cambio de contraseña", "bi-exclamation-triangle", true),
            ["Bloqueo"] = ("Bloqueó la pantalla", "bi-lock", false),
            ["Desbloqueo"] = ("Desbloqueó la pantalla", "bi-unlock", false),
            ["DesbloqueoFallido"] = ("Intento fallido de desbloqueo", "bi-exclamation-triangle", true),
            ["MovimientoCreado"] = ("Registró un movimiento", "bi-plus-circle", false),
            ["MovimientoEliminado"] = ("Eliminó un movimiento", "bi-trash", false)
        };

        public static string Texto(string accion) => Acciones.TryGetValue(accion, out var a) ? a.Texto : accion;

        public static string Icono(string accion) => Acciones.TryGetValue(accion, out var a) ? a.Icono : "bi-dot";

        /// <summary>True para acciones que conviene resaltar (intentos fallidos).</summary>
        public static bool EsAlerta(string accion) => Acciones.TryGetValue(accion, out var a) && a.Alerta;
    }

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
