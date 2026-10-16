// Punto de entrada de la API. Usa "top-level statements": no hay clase Program
// ni método Main explícitos; el compilador los genera a partir de este archivo.
using PAW_Jueves_API.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Registro de servicios (contenedor de dependencias) ----------

// Habilita los controladores tipo API ([ApiController])
builder.Services.AddControllers();

// Genera el documento OpenAPI que describe los endpoints
builder.Services.AddOpenApi();

// Inversion de Dependencias
// Cada vez que un controlador pida IPasswordService, recibirá un PasswordService.
// "Scoped" = se crea una instancia nueva por cada petición HTTP.
builder.Services.AddScoped<IPasswordService, PasswordService>();

var app = builder.Build();

// ---------- Pipeline: middlewares por los que pasa cada petición ----------

if (app.Environment.IsDevelopment())
{
    // Solo en desarrollo: expone /openapi/v1.json y la interfaz de pruebas Scalar (/scalar)
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Si ocurre una excepción no controlada, se redirige a ErrorController.CapturarError
app.UseExceptionHandler("/api/Error/CapturarError");

// Redirige las peticiones HTTP a HTTPS
app.UseHttpsRedirection();

app.UseAuthorization();

// Conecta las rutas definidas con [Route] en los controladores
app.MapControllers();

app.Run();
