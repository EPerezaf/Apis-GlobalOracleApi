using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shared.Exceptions;
using Shared.Security;
using ValidationError = Shared.Exceptions.ValidationError;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Servicio de autenticación para dealers.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IDistribuidorRepository _distribuidorRepository;

    // Dummy data para validación (en producción, esto vendría de la BD)
    private static readonly Dictionary<string, string> DummyDealers = new()
    {
        { "319333", "319333#2025;" }, // CHEVROLET TORO PARRAL
        { "316456", "316456#2025;" }, // CHEVROLET SARACHO CASAS GRANDES
        { "121211", "121211#2025;" }, // CHEVROLET INTELISIS
        { "122095", "122095#2025;" }, // CHEVROLET MOTORES DE MORELIA
        { "122102", "122102#2025;" }, // CHEVROLET CELAYA CENTRO
        { "122134", "122134#2025;" }, // CHEVROLET PIEDRAS NEGRAS
        { "122153", "122153#2025;" }, // CHEVROLET FELIX GUASAVE
        { "122158", "122158#2025;" }, // CHEVROLET FELIX LOS MOCHIS
        { "122164", "122164#2025;" }, // CHEVROLET CIUDAD JUAREZ
        { "166851", "166851#2025;" }, // CHEVROLET CULIACAN MOTORS
        { "167083", "167083#2025;" }, // CHEVROLET MILENIO MOTORS
        { "185005", "185005#2025;" }, // CHEVROLET SALAMANCA
        { "197573", "197573#2025;" }, // CHEVROLET CAR ONE UNIVERSIDAD
        { "202280", "202280#2025;" }, // CHEVROLET SILAO
        { "203322", "203322#2025;" }, // CADILLAC CULIACAN
        { "216847", "216847#2025;" }, // BUICK-GMC HERMOSILLO
        { "227460", "227460#2025;" }, // BUICK-GMC CIUDAD JUAREZ
        { "233204", "233204#2025;" }, // BUICK-GMC-CADILLAC SALTILLO
        { "235397", "235397#2025;" }, // BUICK-GMC-CADILLAC CHIHUAHUA
        { "262900", "262900#2025;" }, // CHEVROLET TORO
        { "287076", "287076#2025;" }, // CHEVROLET CAR ONE TLALPAN
        { "289357", "289357#2025;" }, // CHEVROLET TORO CD CUAUHTEMOC
        { "289363", "289363#2025;" }, // CHEVROLET TORO UNIVERSIDAD
        { "289364", "289364#2025;" }, // CHEVROLET TORO CD CAMARGO
        { "289365", "289365#2025;" }, // CHEVROLET TORO MANITOBA
        { "290457", "290457#2025;" }, // CHEVROLET MILENIO GALERÍAS
        { "290464", "290464#2025;" }, // CHEVROLET ACAMBARO
        { "290487", "290487#2025;" }, // CHEVROLET CAR ONE RUIZ CORTINES
        { "290488", "290488#2025;" }, // CHEVROLET CAR ONE LAS TORRES
        { "290489", "290489#2025;" }, // CHEVROLET CAR ONE NOGALAR
        { "290490", "290490#2025;" }, // CHEVROLET CAR ONE ALLENDE
        { "290582", "290582#2025;" }, // CADILLAC LOS MOCHIS
        { "290593", "290593#2025;" }, // CHEVROLET AGUA PRIETA
        { "290594", "290594#2025;" }, // CHEVROLET CANANEA
        { "290933", "290933#2025;" }, // CHEVROLET MADERO MOTORES DE MORELIA
        { "293251", "293251#2025;" }, // CHEVROLET CAR ONE LAS BOMBAS
        { "294896", "294896#2025;" }, // CHEVROLET URIANGATO
        { "295681", "295681#2025;" }, // CHEVROLET TORO ORTIZ MENA
        { "295682", "295682#2025;" }, // CHEVROLET TORO CIUDAD DELICIAS
        { "299145", "299145#2025;" }, // BUICK-GMC CIUDAD OBREGON
        { "301711", "301711#2025;" }, // CHEVROLET AEROPUERTO CULIACAN
        { "313868", "313868#2025;" }, // CHEVROLET PASION AUTOMOTRIZ
        { "317810", "317810#2025;" }, // CHEVROLET CUERNAVACA
        { "317822", "317822#2025;" }, // CHEVROLET CUERNAVACA PALMAS
        { "317835", "317835#2025;" }, // CHEVROLET CUERNAVACA ZAPATA
        { "319334", "319334#2025;" }, // CHEVROLET TORO JIMENEZ
        { "326599", "326599#2025;" }  // CHEVROLET CUAUTLA
    };

    public AuthService(
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IDistribuidorRepository distribuidorRepository)
    {
        _configuration = configuration;
        _logger = logger;
        _distribuidorRepository = distribuidorRepository;
    }

    /// <inheritdoc />
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        _logger.LogInformation("🔐 [AUTH] Intento de login para dealer: {DealerBac}", request.DealerBac);

        // Validar credenciales (dummy validation)
        if (!DummyDealers.TryGetValue(request.DealerBac, out var expectedPassword))
        {
            _logger.LogWarning("⚠️ [AUTH] Dealer no encontrado: {DealerBac}", request.DealerBac);
            throw new BusinessValidationException("Credenciales inválidas", new List<ValidationError>());
        }

        if (request.Password != expectedPassword)
        {
            _logger.LogWarning("⚠️ [AUTH] Contraseña incorrecta para dealer: {DealerBac}", request.DealerBac);
            throw new BusinessValidationException("Credenciales inválidas", new List<ValidationError>());
        }

        // Generar token JWT
        var token = await GenerateJwtTokenAsync(request.DealerBac);

        // Consultar información del distribuidor desde CO_DISTRIBUIDORES
        var distribuidor = await _distribuidorRepository.ObtenerPorDealerBacAsync(request.DealerBac);
        
        // Si no se encuentra el distribuidor, usar valores por defecto
        var nombre = distribuidor?.Nombre ?? string.Empty;
        var razonSocial = distribuidor?.RazonSocial ?? string.Empty;
        var dms = string.IsNullOrWhiteSpace(distribuidor?.Dms) ? "GDMS" : distribuidor.Dms;

        _logger.LogInformation("✅ [AUTH] Login exitoso para dealer: {DealerBac}, Nombre: {Nombre}, DMS: {Dms}",
            request.DealerBac, nombre, dms);

        return new LoginResponseDto
        {
            Token = token.Token,
            DealerBac = request.DealerBac,
            Nombre = nombre,
            RazonSocial = razonSocial,
            Dms = dms,
            ExpiresAt = token.ExpiresAt
        };
    }

    private async Task<(string Token, DateTime ExpiresAt)> GenerateJwtTokenAsync(string dealerBac)
    {
        var jwtConfig = _configuration.GetSection("Jwt").Get<JwtConfig>();
        if (jwtConfig == null || string.IsNullOrWhiteSpace(jwtConfig.Key))
        {
            throw new InvalidOperationException("La configuración JWT no está completa en appsettings.json");
        }

        var key = Encoding.UTF8.GetBytes(jwtConfig.Key);
        var expiresAt = DateTimeHelper.GetMexicoDateTime().AddHours(24); // Token válido por 24 horas

        var claims = new List<Claim>
        {
            new Claim("USUARIO", dealerBac),
            new Claim(ClaimTypes.NameIdentifier, dealerBac),
            new Claim("DEALERBAC", dealerBac), // Claim específico para dealerBac
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrWhiteSpace(jwtConfig.Subject))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, jwtConfig.Subject));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return await Task.FromResult((tokenString, expiresAt));
    }

    private class JwtConfig
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
    }
}

