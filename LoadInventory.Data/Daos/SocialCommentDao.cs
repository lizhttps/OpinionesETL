using Microsoft.EntityFrameworkCore;
using OpinionesETL.Data.Daos.Interfaces;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Daos
{
    public class SocialCommentDao : ISocialCommentDao
    {
        private readonly CustomerReviewsDBContext _context;
        private readonly IAppLogger<SocialCommentDao> _logger;

        public SocialCommentDao(CustomerReviewsDBContext context, IAppLogger<SocialCommentDao> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ClearSocialCommentAsync()
        {
            try
            {
                await _context.SocialComments.ExecuteDeleteAsync();
                _context.ChangeTracker.Clear();
                _logger.LogInfo("Tabla SocialComments limpiada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar la tabla SocialComments.");
                throw;
            }
        }

        public async Task LoadSocialCommentsAsync(List<Models.SocialComment> socialComments)
        {
            try
            {
                var entityType = _context.Model.FindEntityType(typeof(Models.SocialComment))
                    ?? throw new InvalidOperationException("No se encontró la entidad SocialComment en el modelo.");
                var tabla = $"[{entityType.GetSchema() ?? "dbo"}].[{entityType.GetTableName()}]";

                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync();

                    // IDENTITY_INSERT solo vale para la conexión actual, por eso va dentro
                    // de la misma transacción que el SaveChanges. Solo se activa si la tabla
                    // realmente tiene una columna IDENTITY.
                    await _context.Database.ExecuteSqlRawAsync(
                        $"IF OBJECTPROPERTY(OBJECT_ID('{tabla}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT {tabla} ON");

                    await _context.SocialComments.AddRangeAsync(socialComments);
                    await _context.SaveChangesAsync();

                    await _context.Database.ExecuteSqlRawAsync(
                        $"IF OBJECTPROPERTY(OBJECT_ID('{tabla}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT {tabla} OFF");

                    await tx.CommitAsync();
                });

                _context.ChangeTracker.Clear();
                _logger.LogInfo($"Se han cargado {socialComments.Count} registros en la tabla SocialComments.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los datos en la tabla SocialComments.");
                throw;
            }
        }
    }
}