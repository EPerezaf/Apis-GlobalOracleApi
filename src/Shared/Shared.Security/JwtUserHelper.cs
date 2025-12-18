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
    }
}

