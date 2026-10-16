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
    /// Controlador de la API para la autenticación de usuarios: inicio de sesión,
    /// registro y recuperación de acceso. Todas las rutas empiezan con /api/Home.
    ///
    /// Se comunica con SQL Server mediante Dapper, llamando procedimientos almacenados.
    /// Usa un constructor primario que recibe por inyección de dependencias:
    ///  - _configuration: para leer appsettings.json (cadena de conexión, link de recuperación)
    ///  - _password: el servicio que cifra el token del link de recuperación
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController(IConfiguration _configuration, IPasswordService _password) : ControllerBase
    {
        /// <summary>
        /// POST api/Home/Login. Valida las credenciales del usuario contra la BD.
        /// </summary>
        /// <param name="model">Correo y contraseña enviados por la WEB.</param>
        /// <returns>
        /// 200 OK con los datos del usuario si las credenciales son correctas;
        /// 404 NotFound con un mensaje si no coinciden.
        /// </returns>
        [HttpPost]
        [Route("Login")]
        public IActionResult Login(LoginRequestModel model)
        {
            // "using" cierra la conexión automáticamente al terminar el método
            using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            // Parámetros que recibe el procedimiento almacenado
            var parameters = new DynamicParameters();
            parameters.Add("@CorreoElectronico", model.CorreoElectronico);
            parameters.Add("@Contrasenna", model.Contrasenna);

            // Devuelve la primera fila mapeada a UsuarioResponse, o null si no hay resultados
            var response = context.QueryFirstOrDefault<UsuarioResponse>("sp_IniciarSesion", parameters);

            if(response == null)
                return NotFound(new UsuarioResponse { Mensaje = "No se validó la información correctamente" });

            return Ok(response);
        }

        /// <summary>
        /// POST api/Home/Register. Registra un usuario nuevo en la BD.
        /// </summary>
        /// <param name="model">Identificación, nombre, correo y contraseña del usuario.</param>
        /// <returns>
        /// 200 OK si se insertó el registro;
        /// 422 UnprocessableEntity si no se insertó (por ejemplo, porque el usuario ya existe).
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

            // Execute devuelve la cantidad de filas afectadas por el procedimiento
            var response = context.Execute("sp_RegistrarUsuario", parameters);

            if(response <= 0)
                return UnprocessableEntity(new UsuarioResponse { Mensaje = "No se registró la información correctamente" });

            return Ok(new UsuarioResponse { Mensaje = "Se registró la información correctamente" });
        }

        /// <summary>
        /// POST api/Home/Forgot. Inicia la recuperación de acceso:
        ///  1. Busca el usuario por correo con sp_ConsultarCorreo.
        ///  2. Si existe, genera un token cifrado con su id, válido por 1 hora.
        ///  3. Arma el link (Variables:LinkForgot + token) que se enviará por correo.
        /// El envío del correo todavía no está implementado (queda para una próxima clase).
        /// </summary>
        /// <param name="model">Correo electrónico del usuario.</param>
        /// <returns>200 OK con los datos del usuario, o 404 NotFound si el correo no existe.</returns>
        [HttpPost]
        [Route("Forgot")]
        public IActionResult Forgot(ForgotRequestModel model)
        {
            using var context = new SqlConnection(_configuration.GetValue<string>("Variables:ConnectionString"));

            //Validar que el usuario exista
            var parameters = new DynamicParameters();
            parameters.Add("@CorreoElectronico", model.CorreoElectronico);

            var response = context.QueryFirstOrDefault<UsuarioResponse>("sp_ConsultarCorreo", parameters);

            if (response == null)
                return NotFound(new UsuarioResponse { Mensaje = "No se validó la información correctamente" });

            //Mecanismo de recuperación
            // Se cifra el IdUsuario para que no viaje visible en el link y expire en 1 hora
            var usuarioId = _password.Cifrar(response.IdUsuario.ToString(), TimeSpan.FromHours(1));
            // Ej: https://localhost:7280/Home/ChangePassword/<token>
            var linkCorreo = _configuration.GetValue<string>("Variables:LinkForgot") + usuarioId;

            //Enviar correo electrónico

            return Ok(response);
        }
    }
}
