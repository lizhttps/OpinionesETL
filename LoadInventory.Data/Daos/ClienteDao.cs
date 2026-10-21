using Microsoft.EntityFrameworkCore;
using OpinionesETL.Data.Daos.Interfaces;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Daos
{
    public class ClienteDao : IClienteDao
    {
        private readonly CustomerReviewsDBContext _context;
        private readonly IAppLogger<ClienteDao> _logger;

        public ClienteDao(CustomerReviewsDBContext context, IAppLogger<ClienteDao> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ClearClientesAsync()
        {
            try
            {
                await _context.Clientes.ExecuteDeleteAsync();
                _context.ChangeTracker.Clear();
                _logger.LogInfo("Tabla Clientes limpiada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar la tabla Clientes.");
                throw;
            }
        }

        public async Task LoadClientesAsync(List<Models.Cliente> clientes)
        {
            try
            {
                // La estrategia de ejecución evita errores si EnableRetryOnFailure está activo.
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    // Limpia entidades pendientes por si la estrategia reintenta.
                    _context.ChangeTracker.Clear();

                    // La transacción fuerza a usar la MISMA conexión en los 3 pasos.
                    await using var tx = await _context.Database.BeginTransactionAsync();

                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Clientes] ON");

                    await _context.Clientes.AddRangeAsync(clientes);
                    await _context.SaveChangesAsync();

                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Clientes] OFF");

                    await tx.CommitAsync();
                });

                _logger.LogInfo($"Se han cargado {clientes.Count} registros en la tabla Clientes.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los datos en la tabla Clientes.");
                throw;
            }
        }
    }
}