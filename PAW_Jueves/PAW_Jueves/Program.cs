// Program.cs de la WEB (ASP.NET Core MVC): es lo primero que se ejecuta al iniciar el proyecto.
// Tiene dos partes:
//   1. Registro de servicios: qué objetos puede crear ASP.NET Core y entregar a los controladores.
//   2. Pipeline: la lista ordenada de pasos (middlewares) por los que pasa CADA petición HTTP.
// Usa "top-level statements": el compilador genera la clase Program y el método Main.
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.DataProtection;
using PAW_Jueves.Services;

var builder = WebApplication.CreateBuilder(args);

// ==================== 1. Registro de servicios ====================

// Activa el patrón MVC: controladores (HomeController, ErrorController) + vistas Razor (.cshtml)
builder.Services.AddControllersWithViews();
// ApiClient: cliente para llamar a la API. Su dirección base es Variables:ApiBaseUrl
// (por ejemplo https://localhost:7122/api/), así los controladores solo escriben "Home/Login".
// Necesita IHttpContextAccessor para leer el token de la sesión y mandarlo a la API.
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ApiClient>(cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Variables:ApiBaseUrl")
        ?? throw new InvalidOperationException("Falta Variables:ApiBaseUrl en appsettings.json"));
    cliente.Timeout = TimeSpan.FromSeconds(30);
});

// Permite guardar datos por usuario entre peticiones (HttpContext.Session): token, IdUsuario, nombre.
// Los datos se guardan en la memoria del servidor y el navegador solo guarda una cookie con el id de sesión.
builder.Services.AddSession(opciones =>
{
    // Sin actividad durante 30 minutos, la sesión se borra y hay que volver a iniciar sesión
    opciones.IdleTimeout = TimeSpan.FromMinutes(30);
    opciones.Cookie.Name = ".PAW_Jueves.Sesion";
    // JavaScript no puede leer la cookie (protege contra robo de sesión por XSS)
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
    // El navegador no manda la cookie en peticiones que vienen de otros sitios (protege contra CSRF)
    opciones.Cookie.SameSite = SameSiteMode.Strict;
    // Por HTTPS la cookie se marca Secure. Se deja SameAsRequest porque docker-compose usa http.
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Límite de intentos: máximo 5 POST por minuto por IP a Login y Register ([EnableRateLimiting("login")]).
// Si se supera, vuelve al Login con el aviso "Demasiados intentos".
builder.Services.AddRateLimiter(opciones =>
{
    opciones.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    opciones.OnRejected = (contexto, _) =>
    {
        contexto.HttpContext.Response.Redirect("/Home/Login?bloqueado=true");
        return ValueTask.CompletedTask;
    };
});

// Data Protection cifra el token antiforgery de los formularios y la cookie de sesión.
// Por defecto sus llaves viven solo dentro del contenedor: al recrearlo se pierden y los
// formularios abiertos fallan con "The key {...} was not found in the key ring".
// Se guardan en una carpeta fija (App_Data/keys, o la de Variables:DataProtectionKeysPath)
// que en docker-compose es un volumen, para que sobrevivan a los reinicios.
var rutaLlaves = builder.Configuration.GetValue<string>("Variables:DataProtectionKeysPath")
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(rutaLlaves))
    .SetApplicationName("PAW_Jueves");

var app = builder.Build();

// ==================== 2. Pipeline (el orden importa) ====================

if (!app.Environment.IsDevelopment())
{
    // Fuera de desarrollo: le indica al navegador que use siempre HTTPS con este sitio (HSTS)
    app.UseHsts();
}

// Los formularios mandan los números con punto decimal (input type="number": 5000.50). Si el
// servidor usa una cultura como es-CR (coma decimal), "5000.50" se leería mal. Por eso los datos
// que llegan se interpretan con la cultura invariante; para MOSTRAR montos se usa FormatoMoneda (es-CR).
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = [CultureInfo.InvariantCulture],
    SupportedUICultures = [CultureInfo.InvariantCulture]
});

// Carga la sesión del usuario al inicio de cada petición. Debe ir ANTES de cualquier código que
// use HttpContext.Session (controladores y vistas como Error.cshtml).
app.UseSession();

// Si una acción lanza una excepción no controlada, se re-ejecuta en /Error/CapturarError,
// que muestra la vista de error amigable (ver ErrorController).
app.UseExceptionHandler("/Error/CapturarError");

// Si alguien entra por http://, se le redirige a https://
app.UseHttpsRedirection();

// MapStaticAssets (más abajo) solo sirve los archivos que existían al compilar. Las fotos de
// perfil se suben mientras la aplicación corre (wwwroot/uploads), por eso también se usa
// UseStaticFiles. "nosniff" evita que el navegador interprete un archivo como otro tipo.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = archivo => archivo.Context.Response.Headers.XContentTypeOptions = "nosniff"
});

// Decide qué controlador y acción atienden la URL (usa la ruta definida más abajo)
app.UseRouting();

// Aplica el límite de intentos ([EnableRateLimiting("login")]). Va después de UseRouting
// porque necesita saber qué acción se va a ejecutar.
app.UseRateLimiter();

// Revisa los atributos [Authorize]. La WEB usa su propio filtro [Autenticado] (ver Filters/).
app.UseAuthorization();

// Sirve los archivos estáticos de wwwroot (css, js, imágenes, lib)
app.MapStaticAssets();

// Ruta por defecto: /{controlador}/{acción}/{id opcional}
//  - "/"                          → Home/Login (valores por defecto)
//  - "/Home/Register"             → HomeController.Register()
//  - "/Home/ChangePassword/abc"   → HomeController.ChangePassword(id: "abc")
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}")
    .WithStaticAssets();

// Arranca el servidor (https://localhost:7280 en el perfil https)
app.Run();
