using CsvHelper;
using Microsoft.Extensions.Configuration;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;
using System.Net.Mail;

namespace OpinionesETL.Data.Services;

public class ClienteService
{
    private readonly Daos.Interfaces.IClienteDao _clienteDao;
    private readonly IAppLogger<ClienteService> _logger;
    private readonly IConfiguration _configuration;

    public ClienteService(
        Daos.Interfaces.IClienteDao clienteDao,
        IAppLogger<ClienteService> logger,
        IConfiguration configuration)
    {
        _clienteDao = clienteDao;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<EtlResult> LoadClientesAsync()
    {
        var result = new EtlResult { NombreTabla = "Clientes" };

        try
        {
            // Obtener la ruta del archivo desde appsettings.json
            string filePath = _configuration["ClientesFilePath"];

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                string errorMsg = $"El archivo de clientes no existe en la ruta: '{filePath}'";
                _logger.LogWarning(errorMsg);
                result.MotivosRechazo.Add(errorMsg);
                return result;
            }

            // Leer las filas del CSV en memoria
            List<CSV.Cliente> clientesCsv = new();

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);

            await foreach (var record in csv.GetRecordsAsync<CSV.Cliente>())
            {
                clientesCsv.Add(record);
            }

            _logger.LogInfo($"Se leyeron {clientesCsv.Count} registros del archivo CSV.");

            // Colecciones para validación y control de duplicados
            List<Models.Cliente> clientesValidos = new();
            HashSet<int> idsVistos = new();
            HashSet<string> emailsVistos = new();

            // Recorrer, limpiar y validar cada cliente
            foreach (var c in clientesCsv)
            {
                result.RegistrosProcesados++;

                // Limpieza de datos (quitar espacios en blanco y normalizar a minúsculas seguras)
                string nombreLimpio = c.Nombre?.Trim() ?? string.Empty;
                string emailLimpio = c.Email?.Trim().ToLowerInvariant() ?? string.Empty;

                // Validación: ID mayor a cero
                if (c.IdCliente <= 0)
                {
                    result.RegistrosRechazados++;
                    result.MotivosRechazo.Add($"Fila rechazada: ID '{c.IdCliente}' no es válido (debe ser mayor a 0).");
                    continue;
                }

                // Validación: Nombre obligatorio
                if (string.IsNullOrWhiteSpace(nombreLimpio))
                {
                    result.RegistrosRechazados++;
                    result.MotivosRechazo.Add($"Fila rechazada: El cliente con ID {c.IdCliente} no tiene nombre.");
                    continue;
                }

                // Validación estricta de correo: verifica formato y descarta nombres adicionales
                bool emailValido = MailAddress.TryCreate(emailLimpio, out var addr) && addr.Address == emailLimpio;
                if (!emailValido)
                {
                    result.RegistrosRechazados++;
                    result.MotivosRechazo.Add($"Fila rechazada: El correo '{emailLimpio}' del ID {c.IdCliente} no tiene un formato válido.");
                    continue;
                }

                // Validación: Duplicados por ID dentro del archivo
                if (!idsVistos.Add(c.IdCliente))
                {
                    result.RegistrosRechazados++;
                    result.MotivosRechazo.Add($"Fila rechazada: El ID {c.IdCliente} está duplicado en el archivo.");
                    continue;
                }

                // Validación: Duplicados por Email dentro del archivo (por restricción UNIQUE)
                if (!emailsVistos.Add(emailLimpio))
                {
                    result.RegistrosRechazados++;
                    result.MotivosRechazo.Add($"Fila rechazada: El correo '{emailLimpio}' ya existe en otra fila.");
                    continue;
                }

                // Agregar el registro validado a la lista de entidades
                clientesValidos.Add(new Models.Cliente
                {
                    IdCliente = c.IdCliente,
                    Nombre = nombreLimpio,
                    Email = emailLimpio
                });
            }

            // Inserción en la base de datos solo si existen registros válidos
            if (clientesValidos.Count > 0)
            {
                await _clienteDao.ClearClientesAsync();
                await _clienteDao.LoadClientesAsync(clientesValidos);
                result.RegistrosInsertados = clientesValidos.Count;
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
            _logger.LogError(ex, "Error crítico durante el proceso ETL de Clientes.");
            throw;
        }
    }
}
