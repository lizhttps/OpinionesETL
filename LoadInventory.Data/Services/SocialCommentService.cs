using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;
using System.Text.RegularExpressions;

namespace OpinionesETL.Data.Services
{
    public class SocialCommentService
    {
        // Acepta un prefijo opcional de letras seguido de dígitos: "T0001", "C12", "45"
        private static readonly Regex IdConPrefijo = new(@"^[A-Za-z]*(\d+)$", RegexOptions.Compiled);

        private readonly Daos.Interfaces.ISocialCommentDao _socialCommentDao;
        private readonly IAppLogger<SocialCommentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly CustomerReviewsDBContext _context;

        public SocialCommentService(
            Daos.Interfaces.ISocialCommentDao socialCommentDao,
            IAppLogger<SocialCommentService> logger,
            IConfiguration configuration,
            CustomerReviewsDBContext context)
        {
            _socialCommentDao = socialCommentDao;
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        /// <summary>
        /// Convierte un ID de texto (con o sin prefijo de letras) a entero positivo.
        /// Ejemplos: "T0001" -> 1, "0045" -> 45, "12" -> 12.
        /// </summary>
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

        public async Task<EtlResult> LoadSocialCommentsAsync()
        {
            var result = new EtlResult { NombreTabla = "SocialComments" };

            try
            {
                string? filePath = _configuration["SocialCommentsFilePath"];

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    string errorMsg = $"El archivo de social comments no existe en la ruta: '{filePath}'";
                    _logger.LogWarning(errorMsg);
                    result.MotivosRechazo.Add(errorMsg);
                    return result;
                }

                var socialCommentsCsv = new List<CSV.SocialCommentCsv>();

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);

                await foreach (var record in csv.GetRecordsAsync<CSV.SocialCommentCsv>())
                {
                    socialCommentsCsv.Add(record);
                }

                _logger.LogInfo($"Se leyeron {socialCommentsCsv.Count} registros del archivo CSV.");

                var socialCommentsValidos = new List<Models.SocialComment>();
                var idsVistos = new HashSet<int>();

                // -------------------- IDS EXISTENTES (integridad referencial) --------------------
                var productosExistentes = (await _context.Productos
                    .Select(p => p.IdProducto)
                    .ToListAsync()).ToHashSet();

                var clientesExistentes = (await _context.Clientes
                    .Select(c => c.IdCliente)
                    .ToListAsync()).ToHashSet();

                // -------------------- VALIDACIÓN DE DATOS --------------------
                foreach (var s in socialCommentsCsv)
                {
                    result.RegistrosProcesados++;

                    // ------------ LIMPIEZA DE DATOS ------------
                    string comentarioLimpio = s.Comentario?.Trim() ?? string.Empty;
                    string fuenteLimpia = s.Fuente?.Trim() ?? string.Empty;

                    // ------------ CONVERSIÓN DE IDS (texto con prefijo -> número) ------------
                    if (!TryParseId(s.IdComment, out int idComment))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdComment '{s.IdComment}' no es válido.");
                        continue;
                    }

                    if (!TryParseId(s.IdProducto, out int idProducto))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdProducto '{s.IdProducto}' del comentario {idComment} no es válido.");
                        continue;
                    }

                    // IdCliente: vacío es válido (queda null), pero texto que no es un ID se rechaza
                    int? idCliente = null;
                    if (!string.IsNullOrWhiteSpace(s.IdCliente))
                    {
                        if (!TryParseId(s.IdCliente, out int idClienteParseado))
                        {
                            result.RegistrosRechazados++;
                            result.MotivosRechazo.Add($"Fila rechazada: El IdCliente '{s.IdCliente}' del comentario {idComment} no es válido.");
                            continue;
                        }
                        idCliente = idClienteParseado;
                    }

                    // ------------ VALIDACIÓN: TIPO DE FUENTE OBLIGATORIO ------------
                    if (string.IsNullOrWhiteSpace(fuenteLimpia))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El comentario con ID {idComment} no tiene tipo de fuente.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: FECHA VÁLIDA ------------
                    if (s.Fecha == default)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El comentario con ID {idComment} no tiene fecha válida.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: COMENTARIO NO VACÍO ------------
                    if (string.IsNullOrWhiteSpace(comentarioLimpio))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El comentario con ID {idComment} está vacío.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: PRODUCTO EXISTE ------------
                    if (!productosExistentes.Contains(idProducto))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdProducto '{idProducto}' del comentario {idComment} no existe en la base de datos.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: CLIENTE EXISTE (solo si viene) ------------
                    if (idCliente != null && !clientesExistentes.Contains(idCliente.Value))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El IdCliente '{idCliente}' del comentario {idComment} no existe en la base de datos.");
                        continue;
                    }

                    // ------------ VALIDACIÓN: DUPLICADOS POR ID ------------
                    if (!idsVistos.Add(idComment))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El ID {idComment} está duplicado en el archivo.");
                        continue;
                    }

                    // ------------ AGREGAR REGISTRO VALIDADO ------------
                    socialCommentsValidos.Add(new Models.SocialComment
                    {
                        IdComment = idComment,
                        IdCliente = idCliente,
                        IdProducto = idProducto,
                        Comentario = comentarioLimpio,
                        Fuente = fuenteLimpia,
                        Fecha = DateOnly.FromDateTime(s.Fecha)
                    });
                }

                // -------------------- INSERCIÓN EN BASE DE DATOS --------------------
                if (socialCommentsValidos.Count > 0)
                {
                    await _socialCommentDao.ClearSocialCommentAsync();
                    await _socialCommentDao.LoadSocialCommentsAsync(socialCommentsValidos);
                    result.RegistrosInsertados = socialCommentsValidos.Count;
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
                _logger.LogError(ex, "Error crítico durante el proceso ETL de SocialComments.");
                throw;
            }
        }
    }
}