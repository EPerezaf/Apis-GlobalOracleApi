using Dapper;
using GM.DealerSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Infrastructure;

namespace GM.DealerSync.Infrastructure.Repositories;

/// <summary>
/// Repository para obtener productos desde CO_GM_LISTAPRODUCTOS para generar payload
/// </summary>
public class ProductoPayloadRepository : IProductoPayloadRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<ProductoPayloadRepository> _logger;

    public ProductoPayloadRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<ProductoPayloadRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<List<ProductoPayload>> GetAllProductosAsync()
    {
        const string sql = @"
            SELECT 
                COGM_NOMBREPRODUCTO as NombreProducto,
                COGM_PAIS as Pais,
                COGM_NOMBREMODELO as NombreModelo,
                COGM_ANIOMODELO as AnioModelo,
                COGM_MODELOINTERES as ModeloInteres,
                COGM_MARCANEGOCIO as MarcaNegocio,
                COGM_NOMBRELOCAL as NombreLocal,
                COGM_DEFINICIONVEHICULO as DefinicionVehiculo
            FROM CO_GM_LISTAPRODUCTOS
            ORDER BY COGM_PRODUCTOID";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo todos los productos para payload...");

            using var connection = await _connectionFactory.CreateConnectionAsync();
            var productos = await connection.QueryAsync<ProductoPayload>(sql);
            var lista = productos.ToList();

            _logger.LogInformation("✅ [REPOSITORY] {Cantidad} productos obtenidos para payload", lista.Count);
            return lista;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener productos. ErrorCode: {ErrorCode}", ex.Number);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error inesperado al obtener productos");
            throw;
        }
    }
}

