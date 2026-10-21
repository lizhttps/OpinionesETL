using CsvHelper;
using Microsoft.Extensions.Configuration;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Services
{
    public class FuenteDatoService
    {
        private readonly Daos.Interfaces.IFuenteDatoDao _fuenteDatoDao;
        private readonly IAppLogger<FuenteDatoService> _logger;
        private readonly IConfiguration _configuration;

        public FuenteDatoService(
            Daos.Interfaces.IFuenteDatoDao fuenteDatoDao,
            IAppLogger<FuenteDatoService> logger,
            IConfiguration configuration)
        {
            _fuenteDatoDao = fuenteDatoDao;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<EtlResult> LoadFuenteDatosAsync()
        {
            var result = new EtlResult { NombreTabla = "FuenteDatos" };

            try
            {
                // -------------------- OBTENER RUTA DEL ARCHIVO --------------------
                string filePath = _configuration["FuenteDatosFilePath"];

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    string errorMsg = $"El archivo de fuente de datos no existe en la ruta: '{filePath}'";
                    _logger.LogWarning(errorMsg);
                    result.MotivosRechazo.Add(errorMsg);
                    return result;
                }

                // -------------------- LECTURA DEL CSV --------------------
                var fuenteDatosCsv = new List<CSV.FuenteDatos>();
                var idsVistos = new HashSet<int>();

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);

                await foreach (var record in csv.GetRecordsAsync<CSV.FuenteDatos>())
                {
                    fuenteDatosCsv.Add(record);
                }

                _logger.LogInfo($"Se leyeron {fuenteDatosCsv.Count} registros del archivo CSV.");

                var fuenteDatosValidos = new List<Models.FuenteDato>();

                // -------------------- VALIDACIÓN DE DATOS --------------------
                foreach (var f in fuenteDatosCsv)
                {
                    result.RegistrosProcesados++;

                    // ------------ LIMPIEZA DE DATOS ------------
                    string TipoFuenteLimpia = f.TipoFuente?.Trim() ?? string.Empty;

                    // ------------ VALIDACIÓN: ID VÁLIDO ------------
                    if (!int.TryParse(f.IdFuente?.Replace("F", "").Replace("f", ""), out int idNumerico) || idNumerico <= 0)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: IdFuente '{f.IdFuente}' no es válido.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: TIPO DE FUENTE OBLIGATORIO ------------
                    if (string.IsNullOrWhiteSpace(TipoFuenteLimpia))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: La fuente con ID {idNumerico} no tiene tipo de fuente.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: FECHA DE CARGA VÁLIDA ------------
                    if (f.FechaCarga == default)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: La fuente con ID {idNumerico} no tiene fecha de carga válida.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: DUPLICADOS POR ID ------------
                    if (!idsVistos.Add(idNumerico))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El ID {idNumerico} está duplicado en el archivo.");
                        continue;
                    }

                    // ------------ AGREGAR REGISTRO VALIDADO ------------
                    fuenteDatosValidos.Add(new Models.FuenteDato
                    {
                        IdFuente = idNumerico,
                        TipoFuente = TipoFuenteLimpia,
                        FechaCarga = DateOnly.FromDateTime(f.FechaCarga)
                    });
                }

                // -------------------- INSERCIÓN EN BASE DE DATOS --------------------
                if (fuenteDatosValidos.Count > 0)
                {
                    await _fuenteDatoDao.ClearFuenteDatosAsync();
                    await _fuenteDatoDao.LoadFuenteDatosAsync(fuenteDatosValidos);
                    result.RegistrosInsertados = fuenteDatosValidos.Count;
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
                _logger.LogError(ex, "Error crítico durante el proceso ETL de FuenteDatos.");
                throw;
            }
        }
    }
}
