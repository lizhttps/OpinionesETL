using System.Diagnostics;
using System.Text.Json;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Extractors
{
    public class ExtractionService
    {
        private readonly CsvExtractor _csvExtractor;
        private readonly DatabaseExtractor _dbExtractor;
        private readonly ApiExtractor _apiExtractor;
        private readonly IAppLogger<ExtractionService> _logger;

        private const string StagingFolder = "staging";

        public ExtractionService(
            CsvExtractor csvExtractor,
            DatabaseExtractor dbExtractor,
            ApiExtractor apiExtractor,
            IAppLogger<ExtractionService> logger)
        {
            _csvExtractor = csvExtractor;
            _dbExtractor = dbExtractor;
            _apiExtractor = apiExtractor;
            _logger = logger;
        }

        public async Task RunExtractionAsync()
        {
            _logger.LogInfo("=== INICIANDO FASE DE EXTRACCIÓN (E de ETL) ===");
            Directory.CreateDirectory(StagingFolder);

            var stopwatchTotal = Stopwatch.StartNew();

            // 1. Extracción CSV (Encuestas)
            try
            {
                var sw = Stopwatch.StartNew();
                var surveys = await _csvExtractor.ExtractAsync();
                sw.Stop();

                await GuardarStagingAsync("staging_surveys.json", surveys);
                _logger.LogInfo($"[CSV] Encuestas extraídas: {surveys.Count} en {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CSV] Error extrayendo encuestas.");
            }

            // 2. Extracción Base de Datos (WebReviews)
            try
            {
                var sw = Stopwatch.StartNew();
                var reviews = await _dbExtractor.ExtractAsync();
                sw.Stop();

                await GuardarStagingAsync("staging_webreviews.json", reviews);
                _logger.LogInfo($"[BD] Reseñas extraídas: {reviews.Count} en {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BD] Error extrayendo reseñas desde la BD.");
            }

            // 3. Extracción API REST (SocialComments)
            try
            {
                var sw = Stopwatch.StartNew();
                var comments = await _apiExtractor.ExtractAsync();
                sw.Stop();

                await GuardarStagingAsync("staging_socialcomments.json", comments);
                _logger.LogInfo($"[API] Comentarios extraídos: {comments.Count} en {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] Error extrayendo comentarios desde la API.");
            }

            stopwatchTotal.Stop();
            _logger.LogInfo($"=== EXTRACCIÓN COMPLETADA en {stopwatchTotal.ElapsedMilliseconds} ms ===\n");
        }

        private static async Task GuardarStagingAsync<T>(string fileName, T data)
        {
            string path = Path.Combine(StagingFolder, fileName);
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
        }
    }
}