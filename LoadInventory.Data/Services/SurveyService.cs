using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Services
{
    public class SurveyService
    {
        private readonly Daos.Interfaces.ISurveyDao _surveyDao;
        private readonly IAppLogger<SurveyService> _logger;
        private readonly IConfiguration _configuration;
        private readonly CustomerReviewsDBContext _context;

        public SurveyService(
            Daos.Interfaces.ISurveyDao surveyDao,
            IAppLogger<SurveyService> logger,
            IConfiguration configuration,
            CustomerReviewsDBContext context)
        {
            _surveyDao = surveyDao;
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        public async Task<EtlResult> LoadSurveysAsync()
        {
            var result = new EtlResult { NombreTabla = "Surveys" };

            try
            {
                string filePath = _configuration["SurveysFilePath"];

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    string errorMsg = $"El archivo de surveys no existe en la ruta: '{filePath}'";
                    _logger.LogWarning(errorMsg);
                    result.MotivosRechazo.Add(errorMsg);
                    return result;
                }

                var surveysCsv = new List<CSV.Surveys>();

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);

                await foreach (var record in csv.GetRecordsAsync<CSV.Surveys>())
                {
                    surveysCsv.Add(record);
                }

                _logger.LogInfo($"Se leyeron {surveysCsv.Count} registros del archivo CSV.");

                var surveysValidos = new List<Models.Survey>();
                var idsVistos = new HashSet<int>();

                // -------------------- IDS EXISTENTES (integridad referencial) --------------------
                var productosExistentes = (await _context.Productos
                    .Select(p => p.IdProducto)
                    .ToListAsync()).ToHashSet();

                var clientesExistentes = (await _context.Clientes
                    .Select(c => c.IdCliente)
                    .ToListAsync()).ToHashSet();

                // -------------------- CLASIFICACIONES PERMITIDAS (CHECK de la BD) --------------------
                var clasificacionesValidas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "positiva", "Positiva" },
                    { "negativa", "Negativa" },
                    { "neutra", "Neutra" }
                };

                // -------------------- VALIDACIÓN DE DATOS --------------------
                foreach (var s in surveysCsv)
                {
                    result.RegistrosProcesados++;

                    // ------------ LIMPIEZA DE DATOS ------------
                    string comentarioLimpio = s.Comentario?.Trim() ?? string.Empty;
                    string fuenteLimpia = s.Fuente?.Trim() ?? string.Empty;
                    string clasificacionLimpia = s.Clasificacion?.Trim() ?? string.Empty;

                    // Asignación directa de números (ya son int en tu clase CSV)
                    int idOpinion = s.IdOpinion;
                    int idCliente = s.IdCliente;
                    int idProducto = s.IdProducto;
                    int puntaje = s.PuntajeSatisfaccion;

                    // ------------ VALIDACIÓN: IDS MAYORES A CERO ------------
                    if (idOpinion <= 0)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdOpinion '{idOpinion}' no es válido (debe ser mayor a 0).");
                        continue;
                    }

                    if (idCliente <= 0)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdCliente '{idCliente}' de la opinión {idOpinion} no es válido.");
                        continue;
                    }

                    if (idProducto <= 0)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdProducto '{idProducto}' de la opinión {idOpinion} no es válido.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: PUNTAJE ENTRE 1 Y 5 ------------
                    if (puntaje < 1 || puntaje > 5)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El puntaje {puntaje} de la opinión {idOpinion} está fuera del rango 1 a 5.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: CLASIFICACIÓN PERMITIDA ------------
                    if (!clasificacionesValidas.TryGetValue(clasificacionLimpia, out string? clasificacion))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: La clasificación '{clasificacionLimpia}' de la opinión {idOpinion} no es válida.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: CAMPOS OBLIGATORIOS ------------
                    if (string.IsNullOrWhiteSpace(fuenteLimpia))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: La opinión {idOpinion} no tiene fuente.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(comentarioLimpio))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: La opinión {idOpinion} no tiene comentario.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: FECHA VÁLIDA ------------
                    if (s.Fecha == default)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: La opinión {idOpinion} no tiene fecha válida.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: INTEGRIDAD REFERENCIAL (FK) ------------
                    if (!clientesExistentes.Contains(idCliente))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdCliente '{idCliente}' de la opinión {idOpinion} no existe en la base de datos.");
                        continue;
                    }

                    if (!productosExistentes.Contains(idProducto))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdProducto '{idProducto}' de la opinión {idOpinion} no existe en la base de datos.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: DUPLICADOS POR ID ------------
                    if (!idsVistos.Add(idOpinion))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El ID {idOpinion} está duplicado en el archivo.");
                        continue;
                    }

                    // ------------ AGREGAR REGISTRO VALIDADO ------------
                    surveysValidos.Add(new Models.Survey
                    {
                        IdOpinion = idOpinion,
                        IdCliente = idCliente,
                        IdProducto = idProducto,
                        Fuente = fuenteLimpia,
                        Fecha = DateOnly.FromDateTime(s.Fecha),
                        Comentario = comentarioLimpio,
                        Clasificacion = clasificacion,
                        PuntajeSatisfaccion = puntaje
                    });
                }

                // -------------------- INSERCIÓN EN BASE DE DATOS --------------------
                if (surveysValidos.Count > 0)
                {
                    await _surveyDao.ClearSurveysAsync();
                    await _surveyDao.LoadSurveysAsync(surveysValidos);
                    result.RegistrosInsertados = surveysValidos.Count;
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
                _logger.LogError(ex, "Error crítico durante el proceso ETL de Surveys.");
                throw;
            }
        }
    }
}
