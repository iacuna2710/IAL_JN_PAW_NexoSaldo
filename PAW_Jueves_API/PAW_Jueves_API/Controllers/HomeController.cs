using System.Data;
using Dapper;
using Microsoft.AspNetCore.RateLimiting;
using PAW_Jueves_API.Models;
using PAW_Jueves_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace PAW_Jueves_API.Controllers
{
    /// <summary>
    /// Controlador de la API que atiende todo lo relacionado con la cuenta del usuario:
    /// inicio de sesión (Login), registro (Register) y recuperación de acceso (Forgot).
    ///
    /// Rutas: el atributo [Route("api/[controller]")] arma la ruta base con el nombre de la
    /// clase sin la palabra "Controller", o sea /api/Home. Cada método agrega su parte con
    /// [Route("...")], por ejemplo POST https://localhost:7122/api/Home/Login.
    ///
    /// [ApiController] activa dos comportamientos automáticos:
    ///  1. Los parámetros de tipo clase (los *RequestModel) se leen del cuerpo JSON de la petición.
    ///  2. Si el modelo no cumple sus validaciones ([Required] vacío), ASP.NET Core responde
    ///     400 Bad Request por su cuenta y el método del controlador NUNCA se ejecuta.
    ///
    /// Acceso a datos: no usa Entity Framework. Abre una SqlConnection y con Dapper ejecuta
    /// procedimientos almacenados de la BD JN_BD. Dapper copia cada columna del resultado a la
    /// propiedad de UsuarioResponse que tenga el mismo nombre (IdUsuario, NombreCompleto...).
    ///
    /// Dependencias que recibe en el constructor primario (las entrega ASP.NET Core):
    ///  - _configuration: lee appsettings.json. Se usan "Variables:ConnectionString" (cadena de
    ///    conexión a SQL Server) y "Variables:LinkForgot" (inicio del link de recuperación).
    ///  - _password: servicio IPasswordService (implementado por PasswordService) que cifra el
    ///    id del usuario para el link de recuperación. Se registró en Program.cs.
    ///  - _hash: IHashService, hashea y verifica contraseñas con BCrypt.
    ///  - _token: ITokenService, genera el token JWT que se entrega al iniciar sesión.
    ///  - _bitacora: IBitacoraService, anota los inicios de sesión (correctos y fallidos).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController(IConfiguration _configuration, IPasswordService _password,
        IHashService _hash, ITokenService _token, IBitacoraService _bitacora) : ControllerBase
    {
        /// <summary>
        /// POST /api/Home/Login. Verifica el correo y la contraseña de un usuario ACTIVO.
        ///  1. sp_ConsultarCredenciales trae el usuario por correo, con su contraseña guardada.
        ///  2. IHashService compara la contraseña escrita con la guardada (hash de BCrypt).
        ///  3. Si el usuario todavía tenía la contraseña en texto plano (usuarios viejos), se
        ///     reemplaza por el hash en este mismo momento (sp_ActualizarContrasenna).
        ///  4. Se genera un token JWT con el IdUsuario, que la WEB guarda en su sesión.
        /// [EnableRateLimiting("credenciales")] limita los intentos por IP (ver Program.cs).
        /// </summary>
        /// <param name="model">JSON con CorreoElectronico y Contrasenna enviado por la WEB.</param>
        /// <returns>
        /// - 200 OK: credenciales correctas. Cuerpo: token, idUsuario y nombreCompleto.
        /// - 404 Not Found: correo o contraseña incorrectos, o usuario inactivo.
        ///   Mensaje = "No se validó la información correctamente" (no se dice cuál de los dos falló).
        /// - 400 Bad Request: algún campo llegó vacío (lo responde [ApiController] solo).
        /// - 429 Too Many Requests: demasiados intentos desde la misma IP.
        /// </returns>
        [HttpPost]
        [Route("Login")]
        [EnableRateLimiting("credenciales")]
        public async Task<IActionResult> Login(LoginRequestModel model, CancellationToken cancellationToken)
        {
            await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            var usuario = await context.QueryFirstOrDefaultAsync<CredencialesUsuario>(new CommandDefinition(
                "sp_ConsultarCredenciales",
                new { model.CorreoElectronico },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            var resultado = usuario == null
                ? ResultadoVerificacion.Incorrecta
                : _hash.Verificar(model.Contrasenna, usuario.Contrasenna);

            if (usuario == null || resultado == ResultadoVerificacion.Incorrecta)
            {
                // Solo se anota si el correo existe: así el dueño de la cuenta ve que alguien intentó entrar
                if (usuario != null)
                    await _bitacora.RegistrarAsync(usuario.IdUsuario, AccionesBitacora.InicioSesionFallido, "Contraseña incorrecta", cancellationToken);

                return NotFound(new UsuarioResponse { Mensaje = "No se validó la información correctamente" });
            }

            // Migración transparente: el usuario escribió bien su contraseña vieja (texto plano),
            // así que se aprovecha para guardarla ya hasheada.
            if (resultado == ResultadoVerificacion.CorrectaRequiereHash)
            {
                await context.ExecuteAsync(new CommandDefinition(
                    "sp_ActualizarContrasenna",
                    new { usuario.IdUsuario, Contrasenna = _hash.Hashear(model.Contrasenna) },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
            }

            await _bitacora.RegistrarAsync(usuario.IdUsuario, AccionesBitacora.InicioSesion, cancellationToken: cancellationToken);

            return Ok(new LoginResponse
            {
                Token = _token.Generar(usuario.IdUsuario, usuario.NombreCompleto),
                IdUsuario = usuario.IdUsuario,
                NombreCompleto = usuario.NombreCompleto,
                FotoPerfil = usuario.FotoPerfil
            });
        }

        /// <summary>
        /// POST /api/Home/Register. Crea un usuario nuevo con el procedimiento
        /// sp_RegistrarUsuario. Ese procedimiento SOLO inserta si no existe ya otro usuario
        /// con la misma Identificacion o el mismo CorreoElectronico; el usuario nuevo queda
        /// con Estado = 1 (activo). La contraseña se guarda como hash de BCrypt, nunca en texto plano.
        /// </summary>
        /// <param name="model">JSON con Identificacion, NombreCompleto, CorreoElectronico y Contrasenna.</param>
        /// <returns>
        /// - 200 OK: se insertó el usuario. Mensaje = "Se registró la información correctamente".
        /// - 409 Conflict: no se insertó porque la identificación o el correo ya estaban registrados.
        /// - 400 Bad Request: algún campo llegó vacío (lo responde [ApiController] solo).
        /// - 429 Too Many Requests: demasiados intentos desde la misma IP.
        /// </returns>
        [HttpPost]
        [Route("Register")]
        [EnableRateLimiting("credenciales")]
        public async Task<IActionResult> Register(RegisterRequestModel model, CancellationToken cancellationToken)
        {
            await using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            var parameters = new DynamicParameters();
            parameters.Add("@Identificacion", model.Identificacion);
            parameters.Add("@NombreCompleto", model.NombreCompleto);
            parameters.Add("@CorreoElectronico", model.CorreoElectronico);
            parameters.Add("@Contrasenna", _hash.Hashear(model.Contrasenna));

            // ExecuteAsync devuelve cuántas filas se afectaron: 1 si se insertó el usuario;
            // -1 si el procedimiento no insertó nada porque el usuario ya existía.
            var filas = await context.ExecuteAsync(new CommandDefinition(
                "sp_RegistrarUsuario",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

            if (filas <= 0)
                return Conflict(new UsuarioResponse { Mensaje = "La identificación o el correo electrónico ya están registrados" });

            return Ok(new UsuarioResponse { Mensaje = "Se registró la información correctamente" });
        }

        /// <summary>
        /// POST /api/Home/Forgot. Primer paso de "Olvidé mi contraseña". Hace lo siguiente:
        ///  1. Busca con sp_ConsultarCorreo un usuario ACTIVO que tenga ese correo.
        ///  2. Si no existe, responde 404 y termina.
        ///  3. Si existe, cifra su IdUsuario con PasswordService. El token resultante deja de
        ///     ser válido 1 hora después de generarse.
        ///  4. Arma el link de recuperación juntando Variables:LinkForgot + token, por ejemplo:
        ///     https://localhost:7280/Home/ChangePassword/Xy7_aB-...  (el token va al final).
        ///  5. Responde 200 con los datos del usuario.
        ///
        /// IMPORTANTE: el link se guarda en la variable linkCorreo pero TODAVÍA NO SE ENVÍA
        /// a ningún lado (el envío por correo electrónico está pendiente) y tampoco se incluye
        /// en la respuesta. Por ahora el link solo existe dentro de este método.
        /// </summary>
        /// <param name="model">JSON con el CorreoElectronico del usuario.</param>
        /// <returns>
        /// - 200 OK: el correo pertenece a un usuario activo. El cuerpo trae IdUsuario y NombreCompleto.
        /// - 404 Not Found: el correo no existe o el usuario está inactivo.
        ///   Mensaje = "No se validó la información correctamente".
        /// - 400 Bad Request: el correo llegó vacío (lo responde [ApiController] solo).
        /// </returns>
        [HttpPost]
        [Route("Forgot")]
        public IActionResult Forgot(ForgotRequestModel model)
        {
            using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            //Validar que el usuario exista
            var parameters = new DynamicParameters();
            parameters.Add("@CorreoElectronico", model.CorreoElectronico);

            // Devuelve IdUsuario, Identificacion, NombreCompleto, CorreoElectronico y Estado,
            // o null si no hay un usuario activo con ese correo
            var response = context.QueryFirstOrDefault<UsuarioResponse>("sp_ConsultarCorreo", parameters);

            if (response == null)
                return NotFound(new UsuarioResponse { Mensaje = "No se validó la información correctamente" });

            //Mecanismo de recuperación
            // Se cifra el IdUsuario (por ejemplo 5) para que no aparezca a simple vista en el
            // link: así nadie puede cambiar el número en la URL para entrar a otra cuenta.
            // TimeSpan.FromHours(1) = el token vence 1 hora después de este momento.
            var usuarioId = _password.Cifrar(response.IdUsuario.ToString(), TimeSpan.FromHours(1));
            // Link completo = valor de "Variables:LinkForgot" en appsettings.json + token
            var linkCorreo = _configuration.GetValue<string>("Variables:LinkForgot") + usuarioId;

            //Enviar correo electrónico
            // (Pendiente: aquí se enviará linkCorreo al CorreoElectronico del usuario.)

            return Ok(response);
        }
    }
}
