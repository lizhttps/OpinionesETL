using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;
using System.Text.RegularExpressions;

namespace OpinionesETL.Data.Services
{
    public class WebReviewService
    {
        // Acepta un prefijo opcional de letras seguido de dígitos: "W0001", "C12", "45"
        private static readonly Regex IdConPrefijo = new(@"^[A-Za-z]*(\d+)$", RegexOptions.Compiled);

        private readonly Daos.Interfaces.IWebReviewDao _webReviewDao;
        private readonly IAppLogger<WebReviewService> _logger;
        private readonly IConfiguration _configuration;
        private readonly CustomerReviewsDBContext _context;

        public WebReviewService(
            Daos.Interfaces.IWebReviewDao webReviewDao,
            IAppLogger<WebReviewService> logger,
            IConfiguration configuration,
            CustomerReviewsDBContext context)
        {
            _webReviewDao = webReviewDao;
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }
        private static bool TryParseId(string? raw, out int id)
        {
            id = 0;

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var match = IdConPrefijo.Match(raw.Trim());
            if (!match.Success)
                return false;

            return int.TryParse(match.Groups[1].Value, out id) && id > 0;
        }

        public async Task<EtlResult> LoadWebReviewsAsync()
        {
            var result = new EtlResult { NombreTabla = "WebReviews" };

            try
            {
                string? filePath = _configuration["WebReviewsFilePath"];

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    string errorMsg = $"El archivo de web reviews no existe en la ruta: '{filePath}'";
                    _logger.LogWarning(errorMsg);
                    result.MotivosRechazo.Add(errorMsg);
                    return result;
                }

                var webReviewsCsv = new List<CSV.WebReviewCsv>();

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);

                await foreach (var record in csv.GetRecordsAsync<CSV.WebReviewCsv>())
                {
                    webReviewsCsv.Add(record);
                }

                _logger.LogInfo($"Se leyeron {webReviewsCsv.Count} registros del archivo CSV.");

                var webReviewsValidos = new List<Models.WebReview>();
                var idsVistos = new HashSet<int>();

                // Consultar IDs existentes en la BD para verificar integridad referencial (FK)
                var productosExistentes = (await _context.Productos
                    .Select(p => p.IdProducto)
                    .ToListAsync()).ToHashSet();

                var clientesExistentes = (await _context.Clientes
                    .Select(c => c.IdCliente)
                    .ToListAsync()).ToHashSet();

                // -------------------- VALIDACIÓN DE DATOS --------------------
                foreach (var w in webReviewsCsv)
                {
                    result.RegistrosProcesados++;

                    // Limpieza de datos
                    string comentarioLimpio = w.Comentario?.Trim() ?? string.Empty;

                    // Conversión de IDs (texto con prefijo -> número)
                    if (!TryParseId(w.IdReview, out int idReview))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdReview '{w.IdReview}' no es válido.");
                        continue;
                    }

                    if (!TryParseId(w.IdCliente, out int idCliente))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdCliente '{w.IdCliente}' de la review {idReview} no es válido.");
                        continue;
                    }

                    if (!TryParseId(w.IdProducto, out int idProducto))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdProducto '{w.IdProducto}' de la review {idReview} no es válido.");
                        continue;
                    }

                    // Validación: Comentario obligatorio
                    if (string.IsNullOrWhiteSpace(comentarioLimpio))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: La review {idReview} no tiene comentario.");
                        continue;
                    }

                    // Validación: Rating entre 1 y 5
                    if (w.Rating < 1 || w.Rating > 5)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El Rating '{w.Rating}' de la review {idReview} está fuera del rango permitido (1 a 5).");
                        continue;
                    }

                    // Validación: Fecha válida
                    if (w.Fecha == default)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: La review {idReview} no tiene fecha válida.");
                        continue;
                    }

                    // Validación: Integridad referencial (FK)
                    if (!clientesExistentes.Contains(idCliente))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdCliente '{idCliente}' de la review {idReview} no existe en la base de datos.");
                        continue;
                    }

                    if (!productosExistentes.Contains(idProducto))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdProducto '{idProducto}' de la review {idReview} no existe en la base de datos.");
                        continue;
                    }

                    // Validación: Duplicados por ID
                    if (!idsVistos.Add(idReview))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El ID {idReview} está duplicado en el archivo.");
                        continue;
                    }

                    // Agregar registro validado
                    webReviewsValidos.Add(new Models.WebReview
                    {
                        IdReview = idReview,
                        IdCliente = idCliente,
                        IdProducto = idProducto,
                        Fecha = DateOnly.FromDateTime(w.Fecha),
                        Comentario = comentarioLimpio,
                        Rating = w.Rating
                    });
                }

                // -------------------- INSERCIÓN EN BASE DE DATOS --------------------
                if (webReviewsValidos.Count > 0)
                {
                    await _webReviewDao.ClearWebReviewsAsync();
                    await _webReviewDao.LoadWebReviewsAsync(webReviewsValidos);
                    result.RegistrosInsertados = webReviewsValidos.Count;
                }
                else
                {
                    _logger.LogWarning("No se encontraron registros válidos para insertar. La tabla no fue modificada.");
                }

                _logger.LogInfo($"Carga completada: {result.RegistrosInsertados} insertados, {result.RegistrosRechazados} rechazados.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico durante el proceso ETL de WebReviews.");
                throw;
            }
        }
    }
}