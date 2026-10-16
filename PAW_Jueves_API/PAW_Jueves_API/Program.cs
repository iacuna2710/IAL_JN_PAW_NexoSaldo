// Program.cs de la API: es lo primero que se ejecuta al iniciar el proyecto.
// Tiene dos partes:
//   1. Registro de servicios: se le dice a ASP.NET Core qué objetos puede crear y entregar
//      a los controladores (inyección de dependencias).
//   2. Pipeline: la lista ordenada de pasos (middlewares) por los que pasa CADA petición HTTP.
// Usa "top-level statements": no hace falta escribir la clase Program ni el método Main,
// el compilador los genera a partir de estas líneas.
using PAW_Jueves_API.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ==================== 1. Registro de servicios ====================

// Busca y activa todas las clases que heredan de ControllerBase (HomeController, ErrorController)
builder.Services.AddControllers();

// Genera automáticamente un documento OpenAPI (JSON) que describe todos los endpoints,
// sus parámetros y respuestas. Lo usa Scalar para mostrar la página de pruebas.
builder.Services.AddOpenApi();

// Inversion de Dependencias
// Regla: "cuando un constructor pida IPasswordService, crear y entregar un PasswordService".
// AddScoped = se crea UNA instancia por cada petición HTTP y se descarta al terminarla.
// (Las otras opciones son AddTransient = una nueva cada vez que se pide, y
//  AddSingleton = una sola para toda la aplicación.)
builder.Services.AddScoped<IPasswordService, PasswordService>();

var app = builder.Build();

// ==================== 2. Pipeline (el orden importa) ====================

if (app.Environment.IsDevelopment())
{
    // Solo cuando ASPNETCORE_ENVIRONMENT = Development (así está en launchSettings.json):
    //  - /openapi/v1.json  → el documento OpenAPI en formato JSON
    //  - /scalar           → página web para probar los endpoints sin usar la WEB
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Si una petición lanza una excepción no controlada, se re-ejecuta en
// /api/Error/CapturarError, que responde 500 con un mensaje genérico (ver ErrorController).
app.UseExceptionHandler("/api/Error/CapturarError");

// Si alguien llama por http://, se le responde con una redirección a https://
app.UseHttpsRedirection();

// Revisa los atributos [Authorize]. Hoy ningún endpoint los usa, así que no bloquea nada.
app.UseAuthorization();

// Conecta cada URL con el método del controlador que tiene ese [Route]
app.MapControllers();

// Arranca el servidor y queda escuchando peticiones (https://localhost:7122 en el perfil https)
app.Run();
