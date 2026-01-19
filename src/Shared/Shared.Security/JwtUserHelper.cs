using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Shared.Security
{
    /// <summary>
    /// Helper para extraer información del usuario autenticado desde el JWT
    /// </summary>
    public static class JwtUserHelper
    {
        /// <summary>
        /// Obtiene el usuario actual desde el JWT token
        /// </summary>
        /// <param name="user">ClaimsPrincipal del contexto HTTP</param>
        /// <param name="logger">Logger opcional para registrar información</param>
        /// <returns>Identificador del usuario o valor por defecto</returns>
        public static string GetCurrentUser(ClaimsPrincipal? user, ILogger? logger = null)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                logger?.LogWarning("⚠️ Usuario no autenticado - usando 'SYSTEM'");
                return "SYSTEM";
            }

            // Intentar obtener el usuario de diferentes claims (en orden de prioridad)
            var currentUser = 
                // 1. Claim personalizado "USUARIO" (prioridad para este proyecto)
                user.FindFirst("USUARIO")?.Value
                // 2. NameIdentifier (ID único del usuario)
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                // 3. Name (nombre del usuario)
                ?? user.FindFirst(ClaimTypes.Name)?.Value
                // 4. Email
                ?? user.FindFirst(ClaimTypes.Email)?.Value
                // 5. Subject (claim personalizado)
                ?? user.FindFirst("sub")?.Value
                // 6. Identity.Name
                ?? user.Identity?.Name
                // 7. Fallback
                ?? "SYSTEM";

            logger?.LogDebug("👤 Usuario autenticado: {User} (Claims: {ClaimCount})", 
                currentUser, user.Claims.Count());

            // Truncar si es muy largo (máximo 50 caracteres para la BD)
            if (currentUser.Length > 50)
            {
                currentUser = currentUser.Substring(0, 50);
                logger?.LogWarning("⚠️ Usuario truncado a 50 caracteres: {User}", currentUser);
            }

            return currentUser;
        }

        /// <summary>
        /// Obtiene el DealerBac del usuario autenticado desde el JWT token
        /// </summary>
        /// <param name="user">ClaimsPrincipal del contexto HTTP</param>
        /// <param name="logger">Logger opcional para registrar información</param>
        /// <returns>DealerBac del usuario</returns>
        /// <exception cref="UnauthorizedAccessException">Si el usuario no está autenticado o no tiene DealerBac</exception>
        public static string GetDealerBac(ClaimsPrincipal? user, ILogger? logger = null)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                logger?.LogWarning("⚠️ Usuario no autenticado - no se puede obtener DealerBac");
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            // Intentar obtener dealerBac del claim "DEALERBAC" (prioridad)
            var dealerBac = user.FindFirst("DEALERBAC")?.Value
                ?? user.FindFirst("USUARIO")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(dealerBac))
            {
                logger?.LogWarning("⚠️ DealerBac no encontrado en el token");
                throw new UnauthorizedAccessException("DealerBac no encontrado en el token");
            }

            logger?.LogDebug("🏪 DealerBac obtenido: {DealerBac}", dealerBac);
            return dealerBac;
        }

        /// <summary>
        /// Obtiene el EmpresaId del usuario autenticado desde el JWT token
        /// </summary>
        /// <param name="user">ClaimsPrincipal del contexto HTTP</param>
        /// <param name="logger">Logger opcional para registrar información</param>
        /// <returns>EmpresaId del usuario, o null si no está disponible</returns>
        public static int? GetEmpresaId(ClaimsPrincipal? user, ILogger? logger = null)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                logger?.LogWarning("⚠️ Usuario no autenticado - no se puede obtener EmpresaId");
                return null;
            }

            // Intentar obtener empresaId del claim "EMPRESAID" o "EMPR_EMPRESAID"
            var empresaIdStr = user.FindFirst("EMPRESAID")?.Value
                ?? user.FindFirst("EMPR_EMPRESAID")?.Value;

            if (string.IsNullOrWhiteSpace(empresaIdStr))
            {
                logger?.LogDebug("⚠️ EmpresaId no encontrado en el token");
                return null;
            }

            if (int.TryParse(empresaIdStr, out var empresaId))
            {
                logger?.LogDebug("🏢 EmpresaId obtenido: {EmpresaId}", empresaId);
                return empresaId;
            }

            logger?.LogWarning("⚠️ EmpresaId no es un número válido: {EmpresaIdStr}", empresaIdStr);
            return null;
        }
        
        /// <summary>
        /// Obtiene información completa del usuario desde el JWT token
        /// </summary>
        /// <param name="user">ClaimsPrincipal del contexto HTTP</param>
        /// <param name="logger">Logger opcional para registrar información</param>
        /// <returns>Objeto con toda la información del usuario</returns>
        public static UserInfo GetCurrentUserInfo(ClaimsPrincipal? user, ILogger? logger = null)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                logger?.LogWarning("⚠️ Usuario no autenticado - usando información por defecto");
                return new UserInfo
                {
                    Usuario = "SYSTEM",
                    EmpresaId = null,
                    AgenciaId = null,
                    UsuarioId = null,
                    Nombre = "Sistema"
                };
            }

            var usuario = GetCurrentUser(user, logger);
            var empresaId = GetEmpresaId(user, logger);
            
            // Nueva información específica para tu sistema
            var agenciaId = user.FindFirst("AGENCIAID")?.Value
                          ?? user.FindFirst("AGEN_IDAGENCIA")?.Value;
            
            var usuarioId = user.FindFirst("USUARIOID")?.Value
                          ?? user.FindFirst("US_ASESORSERV")?.Value;
            
            var nombre = user.FindFirst("NOMBRE")?.Value
                       ?? user.FindFirst(ClaimTypes.Name)?.Value
                       ?? user.FindFirst(ClaimTypes.GivenName)?.Value;

            logger?.LogDebug("👤 Información de usuario obtenida - Usuario: {Usuario}, Empresa: {EmpresaId}, Agencia: {AgenciaId}", 
                usuario, empresaId, agenciaId);

            return new UserInfo
            {
                Usuario = usuario,
                EmpresaId = empresaId,
                AgenciaId = agenciaId,
                UsuarioId = usuarioId,
                Nombre = nombre
            };
        }

        /// <summary>
        /// Obtiene el AgenciaId del usuario autenticado desde el JWT token
        /// </summary>
        public static string? GetAgenciaId(ClaimsPrincipal? user, ILogger? logger = null)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                logger?.LogWarning("⚠️ Usuario no autenticado - no se puede obtener AgenciaId");
                return null;
            }

            var agenciaId = user.FindFirst("AGENCIAID")?.Value
                          ?? user.FindFirst("AGEN_IDAGENCIA")?.Value;

            logger?.LogDebug("🏪 AgenciaId obtenido: {AgenciaId}", agenciaId ?? "N/A");
            return agenciaId;
        }

        /// <summary>
        /// Obtiene el UsuarioId (ID interno) del usuario autenticado desde el JWT token
        /// </summary>
        public static string? GetUsuarioId(ClaimsPrincipal? user, ILogger? logger = null)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                logger?.LogWarning("⚠️ Usuario no autenticado - no se puede obtener UsuarioId");
                return null;
            }

            var usuarioId = user.FindFirst("USUARIOID")?.Value
                          ?? user.FindFirst("US_ASESORSERV")?.Value;

            logger?.LogDebug("🆔 UsuarioId obtenido: {UsuarioId}", usuarioId ?? "N/A");
            return usuarioId;
        }
    }

    /// <summary>
    /// Información completa del usuario autenticado
    /// </summary>
    public class UserInfo
    {
        public string Usuario { get; set; } = "SYSTEM";
        public int? EmpresaId { get; set; }
        public string? AgenciaId { get; set; }
        public string? UsuarioId { get; set; }
        public string? Nombre { get; set; }

        public bool HasEmpresaInfo => EmpresaId.HasValue && !string.IsNullOrEmpty(AgenciaId);
        
        public override string ToString()
        {
            return HasEmpresaInfo 
                ? $"{Usuario} (Empresa: {EmpresaId}, Agencia: {AgenciaId})"
                : Usuario;
        }
    }
    
}

