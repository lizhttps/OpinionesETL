using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpinionesETL.Data.Extractors.Interfaces;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Extractors
{
    public class ExtractionService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly IExtractor<CSV.Surveys> _csvExtractor;
        private readonly IExtractor<WebReview> _dbExtractor;
        private readonly IExtractor<SocialComment> _apiExtractor;
        private readonly IAppLogger<ExtractionService> _logger;
        private readonly string _stagingFolder;

        public ExtractionService(
            IExtractor<CSV.Surveys> csvExtractor,
            IExtractor<WebReview> dbExtractor,
            IExtractor<SocialComment> apiExtractor,
            IConfiguration configuration,
            IAppLogger<ExtractionService> logger)
        {
            _csvExtractor = csvExtractor;
            _dbExtractor = dbExtractor;
            _apiExtractor = apiExtractor;
            _logger = logger;
            _stagingFolder = configuration["StagingFolder"] ?? "staging";
        }

        public async Task RunExtractionAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInfo("=== INICIANDO FASE DE EXTRACCIÓN (E de ETL) ===");
            Directory.CreateDirectory(_stagingFolder);

            var stopwatchTotal = Stopwatch.StartNew();

            // Las tres fuentes se extraen en paralelo. Cada una maneja sus propios
            // errores, así que el fallo de una no bloquea a las demás.
            await Task.WhenAll(
                ExtractToStagingAsync("CSV", "staging_surveys.json", _csvExtractor, cancellationToken),
                ExtractToStagingAsync("BD", "staging_webreviews.json", _dbExtractor, cancellationToken),
                ExtractToStagingAsync("API", "staging_socialcomments.json", _apiExtractor, cancellationToken));

            stopwatchTotal.Stop();
            _logger.LogInfo($"=== EXTRACCIÓN COMPLETADA en {stopwatchTotal.ElapsedMilliseconds} ms ===\n");
        }

        private async Task ExtractToStagingAsync<T>(
            string source, string fileName, IExtractor<T> extractor, CancellationToken cancellationToken)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var data = await extractor.ExtractAsync(cancellationToken);
                sw.Stop();

                await SaveStagingAsync(fileName, data, cancellationToken);
                _logger.LogInfo($"[{source}] Registros extraídos: {data.Count} en {sw.ElapsedMilliseconds} ms.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInfo($"[{source}] Extracción cancelada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{source}] Error durante la extracción.");
            }
        }

        private async Task SaveStagingAsync<T>(string fileName, T data, CancellationToken cancellationToken)
        {
            var path = Path.Combine(_stagingFolder, fileName);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, data, JsonOptions, cancellationToken);
        }
    }
}