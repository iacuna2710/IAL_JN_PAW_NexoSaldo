using PAW_Jueves_API.Models;

namespace PAW_Jueves_API.Services
{
    /// <summary>
    /// Reglas de negocio del módulo de gastos. Son funciones puras (no tocan la BD), por eso se
    /// pueden probar con pruebas unitarias (PAW_Jueves_API.Tests/ReglasGastosTests.cs).
    /// </summary>
    public static class ReglasGastos
    {
        /// <summary>Porcentaje del presupuesto a partir del cual se avisa.</summary>
        public const decimal UmbralAdvertencia = 0.8m;

        /// <summary>Monto máximo de un movimiento (evita errores de digitación como 1e12).</summary>
        public const decimal MontoMaximo = 999_999_999m;

        // Costa Rica no tiene horario de verano: UTC-6 todo el año
        private static readonly TimeZoneInfo ZonaCostaRica = BuscarZona();

        /// <summary>Fecha de hoy en Costa Rica (no en UTC: a las 7 p. m. en CR ya es "mañana" en UTC).</summary>
        public static DateOnly HoyCostaRica() =>
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaCostaRica));

        /// <summary>
        /// Valida un movimiento contra su categoría. Devuelve null si es válido o el mensaje de error.
        /// Reglas:
        ///  - El monto es mayor que 0 y no supera el máximo.
        ///  - La fecha no es futura ni de hace más de 5 años.
        ///  - La categoría existe, es del usuario (si no, llega null) y está activa.
        ///  - El tipo del movimiento coincide con el de la categoría (no hay gastos en "Salario").
        /// </summary>
        public static string? ValidarMovimiento(MovimientoRequest movimiento, CategoriaResponse? categoria, DateOnly hoy)
        {
            if (movimiento.Monto <= 0)
                return "El monto debe ser mayor que 0";

            if (movimiento.Monto > MontoMaximo)
                return "El monto es demasiado grande";

            if (decimal.Round(movimiento.Monto, 2) != movimiento.Monto)
                return "El monto puede tener como máximo 2 decimales";

            if (movimiento.Fecha > hoy)
                return "La fecha no puede ser futura";

            if (movimiento.Fecha < hoy.AddYears(-5))
                return "La fecha no puede tener más de 5 años";

            if (categoria == null)
                return "La categoría no existe";

            if (!categoria.Estado)
                return "La categoría está desactivada";

            if (categoria.Tipo != movimiento.Tipo)
                return movimiento.Tipo == "G"
                    ? "La categoría es de ingresos; elija una categoría de gastos"
                    : "La categoría es de gastos; elija una categoría de ingresos";

            return null;
        }

        /// <summary>
        /// Alerta de presupuesto para una categoría de gasto:
        ///  - null: no tiene presupuesto o va por debajo del 80 %.
        ///  - "advertencia": gastó el 80 % o más.
        ///  - "excedido": gastó el 100 % o más.
        /// </summary>
        public static string? AlertaPresupuesto(decimal gastado, decimal? presupuesto)
        {
            if (presupuesto is not > 0)
                return null;

            if (gastado >= presupuesto)
                return "excedido";

            return gastado >= presupuesto * UmbralAdvertencia ? "advertencia" : null;
        }

        /// <summary>Texto de la alerta para mostrar al usuario (montos con formato de Costa Rica: 85 000,00).</summary>
        public static string? MensajeAlerta(string? alerta, string categoria, decimal gastado, decimal? presupuesto) => alerta switch
        {
            "excedido" => string.Format(CulturaCostaRica, "Superó el presupuesto de {0}: lleva {1:N2} de {2:N2} este mes.", categoria, gastado, presupuesto),
            "advertencia" => string.Format(CulturaCostaRica, "Atención: ya usó el {0:P0} del presupuesto de {1} ({2:N2} de {3:N2}).", gastado / presupuesto!.Value, categoria, gastado, presupuesto),
            _ => null
        };

        private static readonly System.Globalization.CultureInfo CulturaCostaRica = System.Globalization.CultureInfo.GetCultureInfo("es-CR");

        private static TimeZoneInfo BuscarZona()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("America/Costa_Rica", TimeSpan.FromHours(-6), "Costa Rica", "Costa Rica");
            }
        }
    }
}
