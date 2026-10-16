using Dapper;
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
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController(IConfiguration _configuration, IPasswordService _password) : ControllerBase
    {
        /// <summary>
        /// POST /api/Home/Login. Verifica si existe un usuario ACTIVO (Estado = 1) con ese
        /// correo y esa contraseña, usando el procedimiento sp_IniciarSesion.
        /// </summary>
        /// <param name="model">JSON con CorreoElectronico y Contrasenna enviado por la WEB.</param>
        /// <returns>
        /// - 200 OK: las credenciales coinciden. El cuerpo trae IdUsuario y NombreCompleto.
        /// - 404 Not Found: no hay coincidencia (correo o contraseña incorrectos, o usuario
        ///   inactivo). El cuerpo trae Mensaje = "No se validó la información correctamente".
        /// - 400 Bad Request: algún campo llegó vacío (lo responde [ApiController] solo).
        /// </returns>
        [HttpPost]
        [Route("Login")]
        public IActionResult Login(LoginRequestModel model)
        {
            // Abre la conexión a SQL Server. "using var" la cierra y libera automáticamente
            // cuando el método termina, aunque ocurra una excepción.
            using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            // Parámetros del procedimiento. El nombre ("@CorreoElectronico") debe ser idéntico
            // al que declara el procedimiento en SQL Server.
            var parameters = new DynamicParameters();
            parameters.Add("@CorreoElectronico", model.CorreoElectronico);
            parameters.Add("@Contrasenna", model.Contrasenna);

            // Ejecuta sp_IniciarSesion y toma SOLO la primera fila del resultado convertida
            // a UsuarioResponse. Si el procedimiento no devuelve filas, response queda en null.
            var response = context.QueryFirstOrDefault<UsuarioResponse>("sp_IniciarSesion", parameters);

            // null = no se encontró un usuario con esas credenciales
            if(response == null)
                return NotFound(new UsuarioResponse { Mensaje = "No se validó la información correctamente" });

            return Ok(response);
        }

        /// <summary>
        /// POST /api/Home/Register. Crea un usuario nuevo con el procedimiento
        /// sp_RegistrarUsuario. Ese procedimiento SOLO inserta si no existe ya otro usuario
        /// con la misma Identificacion o el mismo CorreoElectronico; el usuario nuevo queda
        /// con Estado = 1 (activo).
        /// </summary>
        /// <param name="model">JSON con Identificacion, NombreCompleto, CorreoElectronico y Contrasenna.</param>
        /// <returns>
        /// - 200 OK: se insertó 1 fila. Mensaje = "Se registró la información correctamente".
        /// - 422 Unprocessable Entity: no se insertó nada porque la identificación o el correo
        ///   ya estaban registrados. Mensaje = "No se registró la información correctamente".
        /// - 400 Bad Request: algún campo llegó vacío (lo responde [ApiController] solo).
        /// </returns>
        [HttpPost]
        [Route("Register")]
        public IActionResult Register(RegisterRequestModel model)
        {
            using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            var parameters = new DynamicParameters();
            parameters.Add("@Identificacion", model.Identificacion);
            parameters.Add("@NombreCompleto", model.NombreCompleto);
            parameters.Add("@CorreoElectronico", model.CorreoElectronico);
            parameters.Add("@Contrasenna", model.Contrasenna);

            // Execute se usa para instrucciones que no devuelven filas (INSERT, UPDATE, DELETE).
            // Retorna cuántas filas se afectaron: 1 si se insertó el usuario, 0 si el
            // procedimiento no insertó porque el usuario ya existía.
            var response = context.Execute("sp_RegistrarUsuario", parameters);

            if(response <= 0)
                return UnprocessableEntity(new UsuarioResponse { Mensaje = "No se registró la información correctamente" });

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
