// Program.cs de la WEB (ASP.NET Core MVC): es lo primero que se ejecuta al iniciar el proyecto.
// Tiene dos partes:
//   1. Registro de servicios: qué objetos puede crear ASP.NET Core y entregar a los controladores.
//   2. Pipeline: la lista ordenada de pasos (middlewares) por los que pasa CADA petición HTTP.
// Usa "top-level statements": el compilador genera la clase Program y el método Main.
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ==================== 1. Registro de servicios ====================

// Activa el patrón MVC: controladores (HomeController, ErrorController) + vistas Razor (.cshtml)
builder.Services.AddControllersWithViews();
// Permite pedir un HttpClient en el constructor de los controladores; se usa para llamar a la API
builder.Services.AddHttpClient();
// Permite guardar datos por usuario entre peticiones (HttpContext.Session), por ejemplo "Autenticado".
// Los datos se guardan en la memoria del servidor y el navegador solo guarda una cookie con el id de sesión.
builder.Services.AddSession();

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

// Carga la sesión del usuario al inicio de cada petición. Debe ir ANTES de cualquier código que
// use HttpContext.Session (controladores y vistas como Error.cshtml).
app.UseSession();

// Si una acción lanza una excepción no controlada, se re-ejecuta en /Error/CapturarError,
// que muestra la vista de error amigable (ver ErrorController).
app.UseExceptionHandler("/Error/CapturarError");

// Si alguien entra por http://, se le redirige a https://
app.UseHttpsRedirection();

// Decide qué controlador y acción atienden la URL (usa la ruta definida más abajo)
app.UseRouting();

// Revisa los atributos [Authorize]. Hoy ninguna acción los usa, así que no bloquea nada.
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
