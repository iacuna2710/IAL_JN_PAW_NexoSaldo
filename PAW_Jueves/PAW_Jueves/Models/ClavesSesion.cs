namespace PAW_Jueves.Models
{
    /// <summary>
    /// Nombres de las variables que se guardan en HttpContext.Session. Están en un solo lugar para
    /// no escribir el mismo texto a mano en controladores, filtros y vistas (y equivocarse).
    /// </summary>
    public static class ClavesSesion
    {
        /// <summary>Token JWT que entregó la API; se manda en cada llamada protegida.</summary>
        public const string Token = "Token";

        /// <summary>Id del usuario que inició sesión. Si no existe, no hay sesión.</summary>
        public const string IdUsuario = "IdUsuario";

        /// <summary>Nombre que se muestra en la barra superior.</summary>
        public const string NombreCompleto = "NombreCompleto";

        /// <summary>Vale 1 con sesión iniciada. Lo usa Error.cshtml para elegir el layout.</summary>
        public const string Autenticado = "Autenticado";
    }
}
