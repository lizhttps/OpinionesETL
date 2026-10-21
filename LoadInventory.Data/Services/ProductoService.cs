using CsvHelper;
using Microsoft.Extensions.Configuration;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;


namespace OpinionesETL.Data.Services
{
    public class ProductoService
    {
        private readonly Daos.Interfaces.IProductoDao _productoDao;
        private readonly IAppLogger<ProductoService> _logger;
        private readonly IConfiguration _configuration;

        public ProductoService(
            Daos.Interfaces.IProductoDao productoDao,
            IAppLogger<ProductoService> logger,
            IConfiguration configuration)
        {
            _productoDao = productoDao;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<EtlResult> LoadProductosAsync()
        {
            var result = new EtlResult { NombreTabla = "Productos" };

            try
            {
                // Obtener la ruta del archivo desde appsettings.json
                string filePath = _configuration["ProductosFilePath"];

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    string errorMsg = $"El archivo de productos no existe en la ruta: '{filePath}'";
                    _logger.LogWarning(errorMsg);
                    result.MotivosRechazo.Add(errorMsg);
                    return result;
                }

                // Leer las filas del CSV en memoria
                List<CSV.Products> productosCsv = new();

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);

                await foreach (var record in csv.GetRecordsAsync<CSV.Products>())
                {
                    productosCsv.Add(record);
                }

                _logger.LogInfo($"Se leyeron {productosCsv.Count} registros del archivo CSV.");

                // Colecciones para validación y control de duplicados
                List<Models.Producto> productosValidos = new();
                HashSet<int> idsVistos = new();

                // Recorrer, limpiar y validar cada producto
                foreach (var p in productosCsv)
                {
                    result.RegistrosProcesados++;

                    // Limpieza de datos (quitar espacios en blanco)
                    string nombreLimpio = p.Nombre?.Trim() ?? string.Empty;
                    string categoriaLimpia = p.Categoria?.Trim() ?? string.Empty;

                    // Validación: ID mayor a cero
                    if (p.IdProducto <= 0)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: ID '{p.IdProducto}' no es válido (debe ser mayor a 0).");
                        continue;
                    }

                    // Validación: Nombre obligatorio
                    if (string.IsNullOrWhiteSpace(nombreLimpio))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El producto con ID {p.IdProducto} no tiene nombre.");
                        continue;
                    }

                 

                    if (string.IsNullOrWhiteSpace(categoriaLimpia))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El producto con ID {p.IdProducto} no tiene categoría.");
                        continue;
                    }


                    // Validación: Duplicados por ID dentro del archivo
                    if (!idsVistos.Add(p.IdProducto))
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Fila rechazada: El ID {p.IdProducto} está duplicado en el archivo.");
                        continue;
                    }

                    // Agregar el registro validado a la lista de entidades
                    productosValidos.Add(new Models.Producto
                    {
                        IdProducto = p.IdProducto,
                        Nombre = nombreLimpio,
                        Categoria = categoriaLimpia
                    });
                }

                // Inserción en la base de datos solo si existen registros válidos
                if (productosValidos.Count > 0)
                {
                    await _productoDao.ClearProductoAsync();
                    await _productoDao.LoadProductoAsync(productosValidos);
                    result.RegistrosInsertados = productosValidos.Count;
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
                _logger.LogError(ex, "Error crítico durante el proceso ETL de Productos.");
                throw;
            }
        }
    }
}

