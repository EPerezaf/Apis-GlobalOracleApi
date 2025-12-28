using System.Security.Cryptography;
using System.Text;

namespace Shared.Security;

/// <summary>
/// Helper para generar hashes criptográficos.
/// </summary>
public static class HashHelper
{
    /// <summary>
    /// Genera un hash SHA256 de los valores concatenados.
    /// </summary>
    /// <param name="values">Valores a concatenar y hashear</param>
    /// <returns>Hash SHA256 en formato hexadecimal (minúsculas)</returns>
    public static string GenerateSha256Hash(params object[] values)
    {
        if (values == null || values.Length == 0)
        {
            throw new ArgumentException("Se requiere al menos un valor para generar el hash", nameof(values));
        }

        // Concatenar todos los valores como string
        var concatenated = string.Join("", values.Select(v => v?.ToString() ?? string.Empty));

        if (string.IsNullOrWhiteSpace(concatenated))
        {
            throw new ArgumentException("Los valores concatenados no pueden estar vacíos", nameof(values));
        }

        // Generar hash SHA256
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(concatenated);
        var hashBytes = sha256.ComputeHash(bytes);

        // Convertir a string hexadecimal (minúsculas)
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Genera un token de confirmación para sincronización de archivos.
    /// Hash SHA256 de: idCarga + dealerBac + proceso + fechaSincronizacion (ISO format) + registrosSincronizados
    /// </summary>
    /// <param name="idCarga">ID de la carga (COCA_IDCARGA)</param>
    /// <param name="dealerBac">Código BAC del dealer (COSA_DEALERBAC)</param>
    /// <param name="proceso">Nombre del proceso de sincronización (COCA_PROCESO)</param>
    /// <param name="fechaSincronizacion">Fecha de sincronización</param>
    /// <param name="registrosSincronizados">Número de registros sincronizados</param>
    /// <returns>Token de confirmación (hash SHA256)</returns>
    public static string GenerateTokenConfirmacion(
        string idCarga,
        string dealerBac,
        string proceso,
        DateTime fechaSincronizacion,
        int registrosSincronizados)
    {
        // Formatear fecha en formato ISO 8601 (sin milisegundos)
        var fechaIso = fechaSincronizacion.ToString("yyyy-MM-ddTHH:mm:ss");

        // Generar hash con los valores especificados (incluyendo proceso)
        return GenerateSha256Hash(idCarga, dealerBac, proceso, fechaIso, registrosSincronizados);
    }
}

