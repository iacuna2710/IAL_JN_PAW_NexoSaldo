// Punto de entrada de la WEB (ASP.NET Core MVC). Usa "top-level statements":
// el compilador genera la clase Program y el método Main a partir de este archivo.
var builder = WebApplication.CreateBuilder(args);

// ---------- Registro de servicios (contenedor de dependencias) ----------

// Habilita controladores con vistas Razor (.cshtml)
builder.Services.AddControllersWithViews();
// Registra HttpClient para inyectarlo en los controladores y llamar a la API
builder.Services.AddHttpClient();
// Habilita las variables de sesión (HttpContext.Session)
builder.Services.AddSession();

var app = builder.Build();

// ---------- Pipeline: middlewares por los que pasa cada petición ----------

if (!app.Environment.IsDevelopment())
{
    // En producción obliga al navegador a usar siempre HTTPS
    app.UseHsts();
}

// Activa la sesión para cada petición (debe ir antes de usar HttpContext.Session)
app.UseSession();

// Las excepciones no controladas se redirigen a ErrorController.CapturarError
app.UseExceptionHandler("/Error/CapturarError");

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

// Sirve los archivos de wwwroot (css, js, imágenes, lib)
app.MapStaticAssets();

// Ruta por defecto: si la URL no indica controlador/acción, abre Home/Login.
// {id?} es opcional; ChangePassword lo usa para recibir el token del link.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
