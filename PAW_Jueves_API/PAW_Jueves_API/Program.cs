// Program.cs de la API: es lo primero que se ejecuta al iniciar el proyecto.
// Tiene dos partes:
//   1. Registro de servicios: se le dice a ASP.NET Core qué objetos puede crear y entregar
//      a los controladores (inyección de dependencias).
//   2. Pipeline: la lista ordenada de pasos (middlewares) por los que pasa CADA petición HTTP.
// Usa "top-level statements": no hace falta escribir la clase Program ni el método Main,
// el compilador los genera a partir de estas líneas.
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PAW_Jueves_API.Models;
using PAW_Jueves_API.Options;
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
builder.Services.AddSingleton<IHashService, HashService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
// BitacoraService lee la IP del usuario de la petición actual: necesita IHttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<IReportesService, ReportesService>();

// ---------- Gemini (asistente con IA) ----------
// La clave NO va en appsettings: user-secrets en desarrollo o la variable de entorno Gemini__ApiKey.
// Es opcional: sin clave la API arranca igual y el asistente responde "no configurado".
builder.Services.AddOptions<GeminiOptions>()
    .Bind(builder.Configuration.GetSection(GeminiOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient<IGeminiService, GeminiService>((servicios, cliente) =>
{
    var gemini = servicios.GetRequiredService<Microsoft.Extensions.Options.IOptions<GeminiOptions>>().Value;
    cliente.Timeout = TimeSpan.FromSeconds(gemini.TimeoutSegundos);
});

// ---------- Token JWT (quién es el usuario en cada petición) ----------
// Options pattern: la sección "Jwt" de appsettings se convierte en un objeto JwtOptions.
// ValidateOnStart: si falta la Clave (o es muy corta), la API no arranca y avisa en la consola,
// en vez de fallar más tarde en el primer login.
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>() ?? new JwtOptions();

// Los endpoints con [Authorize] exigen el header "Authorization: Bearer {token}".
// Se valida la firma, el emisor, la audiencia y que no haya vencido.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        // Deja los nombres de claims tal cual vienen en el token ("sub", "name")
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// ---------- Límite de intentos (contra fuerza bruta) ----------
// Política "credenciales": máximo 20 peticiones por minuto por IP a Login y Register.
// La WEB tiene su propio límite más estricto por navegador (5 por minuto); este es un
// respaldo por si alguien llama a la API directamente.
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.OnRejected = async (contexto, cancellationToken) =>
        await contexto.HttpContext.Response.WriteAsJsonAsync(
            new ResponseApi { Mensaje = "Demasiados intentos. Espere un minuto e intente de nuevo." },
            cancellationToken);

    // Política "asistente": máximo 10 consultas a la IA por minuto por usuario (cuida la cuota gratuita
    // de Gemini). Se identifica al usuario por el claim "sub" del token.
    opciones.AddPolicy("asistente", http => RateLimitPartition.GetFixedWindowLimiter(
        http.User.FindFirst("sub")?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    opciones.AddPolicy("credenciales", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

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

// Lee el token del header Authorization y llena User con sus claims (IdUsuario, nombre)
app.UseAuthentication();

// Revisa los atributos [Authorize]: sin token válido responde 401 (ver UsuarioController)
app.UseAuthorization();

// Aplica los límites de intentos ([EnableRateLimiting]). Va DESPUÉS de UseAuthentication para que
// la política "asistente" pueda limitar por usuario (necesita el claim "sub" del token).
app.UseRateLimiter();

// Conecta cada URL con el método del controlador que tiene ese [Route]
app.MapControllers();

// Arranca el servidor y queda escuchando peticiones (https://localhost:7122 en el perfil https)
app.Run();
