using Microsoft.EntityFrameworkCore;
using OpinionesETL.Data.Daos.Interfaces;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Daos
{
    public class SurveyDao : ISurveyDao
    {
        private readonly CustomerReviewsDBContext _context;
        private readonly IAppLogger<SurveyDao> _logger;

        public SurveyDao(CustomerReviewsDBContext context, IAppLogger<SurveyDao> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ClearSurveysAsync()
        {
            try
            {
                await _context.Surveys.ExecuteDeleteAsync();
                _context.ChangeTracker.Clear();
                _logger.LogInfo("Tabla Surveys limpiada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar la tabla Surveys.");
                throw;
            }
        }

        public async Task LoadSurveysAsync(List<Models.Survey> surveys)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Surveys] ON;");

                await _context.Surveys.AddRangeAsync(surveys);
                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Surveys] OFF;");

                await transaction.CommitAsync();
                _logger.LogInfo($"Se han cargado {surveys.Count} registros en la tabla Surveys.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al cargar los datos en la tabla Surveys.");
                throw;
            }
        }
    }
}
