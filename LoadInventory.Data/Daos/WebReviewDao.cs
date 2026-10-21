using Microsoft.EntityFrameworkCore;
using OpinionesETL.Data.Daos.Interfaces;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Daos
{
    public class WebReviewDao : IWebReviewDao
    {
        private readonly CustomerReviewsDBContext _context;
        private readonly IAppLogger<WebReviewDao> _logger;

        public WebReviewDao(CustomerReviewsDBContext context, IAppLogger<WebReviewDao> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ClearWebReviewsAsync()
        {
            try
            {
                await _context.WebReviews.ExecuteDeleteAsync();
                _context.ChangeTracker.Clear();
                _logger.LogInfo("Tabla WebReviews limpiada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar la tabla WebReviews.");
                throw;
            }
        }

        public async Task LoadWebReviewsAsync(List<Models.WebReview> webReviews)
        {
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync();

                    // IDENTITY_INSERT solo vale para la sesión/conexión actual,
                    // por eso va dentro de la misma transacción que el SaveChanges.
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Web_Reviews ON");

                    await _context.WebReviews.AddRangeAsync(webReviews);
                    await _context.SaveChangesAsync();

                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Web_Reviews OFF");

                    await tx.CommitAsync();
                });

                _context.ChangeTracker.Clear();
                _logger.LogInfo($"Se han cargado {webReviews.Count} registros en la tabla WebReviews.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los datos en la tabla WebReviews.");
                throw;
            }
        }
    }
}