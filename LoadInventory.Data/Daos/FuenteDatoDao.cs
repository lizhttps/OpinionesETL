using Microsoft.EntityFrameworkCore;
using OpinionesETL.Data.Daos.Interfaces;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Daos
{
    public class FuenteDatoDao : IFuenteDatoDao
    {
        private readonly CustomerReviewsDBContext _context;
        private readonly IAppLogger<FuenteDatoDao> _logger;

        public FuenteDatoDao(CustomerReviewsDBContext context, IAppLogger<FuenteDatoDao> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ClearFuenteDatosAsync()
        {
            try
            {
                await _context.FuenteDatos.ExecuteDeleteAsync();
                _context.ChangeTracker.Clear();
                _logger.LogInfo("Tabla FuenteDatos limpiada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar la tabla FuenteDatos.");
                throw;
            }
        }

        public async Task LoadFuenteDatosAsync(List<Models.FuenteDato> fuenteDatos)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Abrir compuerta para insertar los IDs explícitos
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Fuente_datos] ON;");

                await _context.FuenteDatos.AddRangeAsync(fuenteDatos);
                await _context.SaveChangesAsync();

                // Cerrar compuerta de identidad
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Fuente_datos] OFF;");

                await transaction.CommitAsync();
                _logger.LogInfo($"Se han cargado {fuenteDatos.Count} registros en la tabla FuenteDatos.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al cargar los datos en la tabla FuenteDatos.");
                throw;
            }
        }
    }
}
