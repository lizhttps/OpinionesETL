using Microsoft.EntityFrameworkCore;
using OpinionesETL.Data.Daos.Interfaces;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Daos
{
    public class ProductoDao : IProductoDao
    {
        private readonly CustomerReviewsDBContext _context;
        private readonly IAppLogger<ProductoDao> _logger;

        public ProductoDao(CustomerReviewsDBContext context, IAppLogger<ProductoDao> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ClearProductoAsync()
        {
            try
            {
                await _context.Productos.ExecuteDeleteAsync();
                _context.ChangeTracker.Clear();
                _logger.LogInfo("Tabla Productos limpiada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar la tabla Productos.");
                throw;
            }
        }

        public async Task LoadProductoAsync(List<Models.Producto> productos)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Productos] ON;");

                await _context.Productos.AddRangeAsync(productos);
                await _context.SaveChangesAsync();

     
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Productos] OFF;");

                await transaction.CommitAsync();
                _logger.LogInfo($"Se han cargado {productos.Count} registros en la tabla Productos.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al cargar los datos en la tabla Productos.");
                throw;
            }
        }
    }
}
