// Controllers/LoginController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace GM.CatalogSync.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _oracleConnectionString;

        public LoginController(
            ILogger<LoginController> logger, 
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Obtener la cadena específica para CatalogSync
            _oracleConnectionString = configuration.GetConnectionString("CatalogSync") ??
                                     configuration.GetConnectionString("Oracle");
        }

        [HttpPost("authenticate")]
        public async Task<ActionResult<LoginResponse>> Authenticate([FromBody] LoginRequest loginRequest)
        {
            _logger.LogInformation("[LOGIN] Iniciando autenticación para usuario: {Usuario}", loginRequest.usuario);
            
            try
            {
                // Validar datos de entrada
                if (string.IsNullOrEmpty(loginRequest.usuario) ||
                    string.IsNullOrEmpty(loginRequest.password) ||
                    string.IsNullOrEmpty(loginRequest.empresaId) ||
                    string.IsNullOrEmpty(loginRequest.agenciaId))
                {
                    return BadRequest(new LoginResponse
                    {
                        response = "ERROR",
                        message = "Todos los campos son requeridos"
                    });
                }

                // Validar credenciales contra Oracle
                var usuarioInfo = await ValidarCredencialesOracle(loginRequest);
                
                if (usuarioInfo == null)
                {
                    _logger.LogWarning("[LOGIN] Credenciales incorrectas para usuario: {Usuario}", loginRequest.usuario);
                    return Unauthorized(new LoginResponse
                    {
                        response = "ERROR",
                        message = "Credenciales incorrectas"
                    });
                }

                _logger.LogInformation("[LOGIN] Autenticación exitosa para usuario: {Usuario}", loginRequest.usuario);

                // Generar token JWT usando la configuración existente
                var token = await GenerarTokenJWTAsync(usuarioInfo);

                // Preparar respuesta
                usuarioInfo.token = token;

                return Ok(new LoginResponse
                {
                    response = "OK",
                    message = "Autenticación exitosa",
                    results = usuarioInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LOGIN] Error en autenticación para usuario: {Usuario}", loginRequest.usuario);
                return StatusCode(500, new LoginResponse
                {
                    response = "ERROR",
                    message = "Error interno del servidor"
                });
            }
        }

        private async Task<UsuarioInfo> ValidarCredencialesOracle(LoginRequest loginRequest)
        {
            OracleConnection connection = null;
            try
            {
                if (string.IsNullOrEmpty(_oracleConnectionString))
                {
                    _logger.LogError("[LOGIN] Cadena de conexión Oracle no configurada");
                    return null;
                }

                connection = new OracleConnection(_oracleConnectionString);
                await connection.OpenAsync();
                
                var query = @"SELECT US_IDUSUARIO, US_NOMBRE, AGEN_NOMAGENCIA, US_ASESORSERV, US_URLIMAGEN 
                              FROM LABGDMS.MS_VUSUARIOS 
                              WHERE EMPR_EMPRESAID = :empresaId 
                              AND USA_AGEN_IDAGENCIA = :agenciaId 
                              AND UPPER(US_IDUSUARIO) = UPPER(:usuario) 
                              AND US_PASSWORD = :password";

                using (var command = new OracleCommand(query, connection))
                {
                    command.Parameters.Add(":empresaId", OracleDbType.Varchar2).Value = loginRequest.empresaId;
                    command.Parameters.Add(":agenciaId", OracleDbType.Varchar2).Value = loginRequest.agenciaId;
                    command.Parameters.Add(":usuario", OracleDbType.Varchar2).Value = loginRequest.usuario;
                    command.Parameters.Add(":password", OracleDbType.Varchar2).Value = loginRequest.password;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new UsuarioInfo
                            {
                                usuario = loginRequest.usuario,
                                empresaId = loginRequest.empresaId,
                                agenciaId = loginRequest.agenciaId,
                                usuarioID = reader["US_ASESORSERV"]?.ToString() ?? "",
                                nombreUsuario = reader["US_NOMBRE"]?.ToString() ?? "",
                                nombreAgencia = reader["AGEN_NOMAGENCIA"]?.ToString() ?? "",
                                urlImagen = reader["US_URLIMAGEN"]?.ToString() ?? ""
                            };
                        }
                    }
                }
                
                return null;
            }
            catch (OracleException orex)
            {
                _logger.LogError(orex, "[LOGIN] Error de Oracle: {ErrorCode}, {Message}", orex.ErrorCode, orex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LOGIN] Error al validar credenciales");
                return null;
            }
            finally
            {
                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                    await connection.DisposeAsync();
                }
            }
        }

        private async Task<string> GenerarTokenJWTAsync(UsuarioInfo usuario)
        {
            var jwtConfig = _configuration.GetSection("Jwt").Get<JwtConfig>();
            
            if (jwtConfig == null || string.IsNullOrEmpty(jwtConfig.Key))
            {
                throw new InvalidOperationException("Configuración JWT no encontrada");
            }

            var key = Encoding.UTF8.GetBytes(jwtConfig.Key);
            
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.usuario),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, 
                    ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString()),
                new Claim("USUARIO", usuario.usuario.ToUpper()),
                new Claim("EMPRESAID", usuario.empresaId),
                new Claim("AGENCIAID", usuario.agenciaId),
                new Claim("USUARIOID", usuario.usuarioID),
                new Claim("NOMBRE", usuario.nombreUsuario),
                new Claim(ClaimTypes.Name, usuario.usuario),
                // Claim adicional para identificar el tipo de token
                new Claim("TokenType", "CatalogSyncLogin")
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256);

            var expirationHours = _configuration.GetValue<int>("Jwt:Login:ExpirationHours", 8);
            
            var token = new JwtSecurityToken(
                issuer: jwtConfig.Issuer,
                audience: jwtConfig.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("verify")]
        [Authorize]
        public IActionResult VerifyToken()
        {
            var claims = User.Claims;
            var userInfo = new
            {
                Usuario = claims.FirstOrDefault(c => c.Type == "USUARIO")?.Value,
                EmpresaId = claims.FirstOrDefault(c => c.Type == "EMPRESAID")?.Value,
                AgenciaId = claims.FirstOrDefault(c => c.Type == "AGENCIAID")?.Value,
                UsuarioId = claims.FirstOrDefault(c => c.Type == "USUARIOID")?.Value,
                Nombre = claims.FirstOrDefault(c => c.Type == "NOMBRE")?.Value,
                TokenType = claims.FirstOrDefault(c => c.Type == "TokenType")?.Value
            };

            return Ok(new
            {
                response = "OK",
                message = "Token válido",
                userInfo
            });
        }
    }
}