using Microsoft.EntityFrameworkCore;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;
using OpinionesETL.Data.Models.DW;

namespace OpinionesETL.Data.Services;

public class DwDimensionService
{
    private readonly CustomerReviewsDBContext _oltpContext;
    private readonly OpinionesDwContext _dwContext;
    private readonly IAppLogger<DwDimensionService> _logger;

    public DwDimensionService(
        CustomerReviewsDBContext oltpContext,
        OpinionesDwContext dwContext,
        IAppLogger<DwDimensionService> logger)
    {
        _oltpContext = oltpContext;
        _dwContext = dwContext;
        _logger = logger;
    }

    public Task<EtlResult> LoadDimClientesAsync() =>
        EjecutarCargaAsync("Dim_Clientes", async result =>
        {
            var origen = await _oltpContext.Clientes.AsNoTracking().ToListAsync();
            var existentes = await _dwContext.DimClientes.ToListAsync();

            var porId = existentes.ToDictionary(c => c.IdCliente);
            var emailsUsados = existentes
                .ToDictionary(c => c.Email.Trim().ToLowerInvariant(), c => c.IdCliente);

            var nuevos = new List<DimCliente>();
            int actualizados = 0;

            foreach (var c in origen)
            {
                result.RegistrosProcesados++;

                string nombre = c.Nombre?.Trim() ?? string.Empty;
                string email = c.Email?.Trim().ToLowerInvariant() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(email))
                {
                    result.RegistrosRechazados++;
                    result.MotivosRechazo.Add($"Cliente ID {c.IdCliente}: nombre o correo vacío.");
                    continue;
                }

                if (porId.TryGetValue(c.IdCliente, out var existente))
                {
                    if (emailsUsados.TryGetValue(email, out var otroId) && otroId != c.IdCliente)
                    {
                        result.RegistrosRechazados++;
                        result.MotivosRechazo.Add($"Cliente ID {c.IdCliente}: correo '{email}' ya pertenece al ID {otroId}.");
                        continue;
                    }

                    if (existente.Nombre != nombre || !string.Equals(existente.Email, email, StringComparison.OrdinalIgnoreCase))
                    {
                        emailsUsados.Remove(existente.Email.Trim().ToLowerInvariant());
                        existente.Nombre = nombre;
                        existente.Email = email;
                        emailsUsados[email] = existente.IdCliente;
                        actualizados++;
                    }
                    continue;
                }

                if (emailsUsados.ContainsKey(email))
                {
                    result.RegistrosRechazados++;
                    result.MotivosRechazo.Add($"Cliente ID {c.IdCliente}: correo duplicado '{email}'.");
                    continue;
                }

                var nuevo = new DimCliente
                {
                    IdCliente = c.IdCliente,
                    Nombre = nombre,
                    Email = email
                };

                nuevos.Add(nuevo);
                porId[nuevo.IdCliente] = nuevo;
                emailsUsados[email] = nuevo.IdCliente;
            }

            if (nuevos.Count > 0)
                await _dwContext.DimClientes.AddRangeAsync(nuevos);

            if (nuevos.Count > 0 || actualizados > 0)
                await _dwContext.SaveChangesAsync();

            result.RegistrosInsertados = nuevos.Count;
            _logger.LogInfo($"Dim_Clientes -> Nuevos: {nuevos.Count}, Actualizados: {actualizados}, Rechazados: {result.RegistrosRechazados}");
        });

    public Task<EtlResult> LoadDimProductosAsync() =>
        EjecutarCargaAsync("Dim_Productos", async result =>
        {
            var origen = await _oltpContext.Productos.AsNoTracking().ToListAsync();
            var porId = await _dwContext.DimProducto.ToDictionaryAsync(p => p.IdProducto);

            var nuevos = new List<DimProducto>();
            int actualizados = 0;

            foreach (var p in origen)
            {
                result.RegistrosProcesados++;

                string nombre = p.Nombre?.Trim() ?? string.Empty;
                string categoria = p.Categoria?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(categoria))
                {
                    result.RegistrosRechazados++;
                    result.MotivosRechazo.Add($"Producto ID {p.IdProducto}: nombre o categoría vacío.");
                    continue;
                }

                if (porId.TryGetValue(p.IdProducto, out var existente))
                {
                    if (existente.Nombre != nombre || existente.Categoria != categoria)
                    {
                        existente.Nombre = nombre;
                        existente.Categoria = categoria;
                        actualizados++;
                    }
                    continue;
                }

                var nuevo = new DimProducto
                {
                    IdProducto = p.IdProducto,
                    Nombre = nombre,
                    Categoria = categoria
                };

                nuevos.Add(nuevo);
                porId[nuevo.IdProducto] = nuevo;
            }

            if (nuevos.Count > 0)
                await _dwContext.DimProducto.AddRangeAsync(nuevos);

            if (nuevos.Count > 0 || actualizados > 0)
                await _dwContext.SaveChangesAsync();

            result.RegistrosInsertados = nuevos.Count;
            _logger.LogInfo($"Dim_Productos -> Nuevos: {nuevos.Count}, Actualizados: {actualizados}, Rechazados: {result.RegistrosRechazados}");
        });

    public Task<EtlResult> LoadDimFuenteAsync() =>
        EjecutarCargaAsync("Dim_Fuente", async result =>
        {
            var tiposOrigen = await _oltpContext.FuenteDatos
                .AsNoTracking()
                .Select(f => f.TipoFuente)
                .Distinct()
                .ToListAsync();

            var existentes = (await _dwContext.DimFuente
                .AsNoTracking()
                .Select(f => f.TipoFuente)
                .ToListAsync())
                .Select(f => f.Trim().ToLowerInvariant())
                .ToHashSet();

            var nuevas = new List<DimFuente>();

            foreach (var raw in tiposOrigen)
            {
                string tipo = raw?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(tipo))
                    continue;

                result.RegistrosProcesados++;

                if (existentes.Add(tipo.ToLowerInvariant()))
                    nuevas.Add(new DimFuente { TipoFuente = tipo });
            }

            if (nuevas.Count > 0)
            {
                await _dwContext.DimFuente.AddRangeAsync(nuevas);
                await _dwContext.SaveChangesAsync();
            }

            result.RegistrosInsertados = nuevas.Count;
            _logger.LogInfo($"Dim_Fuente -> Nuevos: {nuevas.Count}, Ya existentes: {result.RegistrosProcesados - nuevas.Count}");
        });

    public Task<EtlResult> LoadDimFechaAsync() =>
        EjecutarCargaAsync("Dim_Fecha", async result =>
        {
            var fechasWeb = await _oltpContext.WebReviews.AsNoTracking().Select(r => r.Fecha).Distinct().ToListAsync();
            var fechasSurveys = await _oltpContext.Surveys.AsNoTracking().Select(s => s.Fecha).Distinct().ToListAsync();
            var fechasSocial = await _oltpContext.SocialComments.AsNoTracking().Select(c => c.Fecha).Distinct().ToListAsync();

            var fechasOrigen = fechasWeb
                .Concat(fechasSurveys)
                .Concat(fechasSocial)
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            var existentes = (await _dwContext.DimFecha
                .AsNoTracking()
                .Select(d => d.Fecha)
                .ToListAsync())
                .ToHashSet();

            var nuevas = new List<DimFecha>();

            foreach (var fecha in fechasOrigen)
            {
                result.RegistrosProcesados++;

                if (existentes.Add(fecha))
                {
                    nuevas.Add(new DimFecha
                    {
                        Fecha = fecha,
                        Mes = fecha.Month,
                        Trimestre = ((fecha.Month - 1) / 3) + 1,
                        Anio = fecha.Year
                    });
                }
            }

            if (nuevas.Count > 0)
            {
                await _dwContext.DimFecha.AddRangeAsync(nuevas);
                await _dwContext.SaveChangesAsync();
            }

            result.RegistrosInsertados = nuevas.Count;
            _logger.LogInfo($"Dim_Fecha -> Nuevas: {nuevas.Count}, Ya existentes: {result.RegistrosProcesados - nuevas.Count}");
        });

    public Task<EtlResult> LoadDimClasificacionAsync() =>
        EjecutarCargaAsync("Dim_Clasificacion", async result =>
        {
            var clasificacionesSurveys = await _oltpContext.Surveys
                .AsNoTracking()
                .Select(s => s.Clasificacion)
                .Distinct()
                .ToListAsync();

            var catalogoBase = new[] { "Positiva", "Neutra", "Negativa" };
            var todas = catalogoBase
                .Concat(clasificacionesSurveys.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existentes = (await _dwContext.DimClasificacion
                .AsNoTracking()
                .Select(c => c.Nombre)
                .ToListAsync())
                .Select(c => c.Trim().ToLowerInvariant())
                .ToHashSet();

            var nuevas = new List<DimClasificacion>();

            foreach (var nombre in todas)
            {
                result.RegistrosProcesados++;

                if (existentes.Add(nombre.ToLowerInvariant()))
                    nuevas.Add(new DimClasificacion { Nombre = nombre });
            }

            if (nuevas.Count > 0)
            {
                await _dwContext.DimClasificacion.AddRangeAsync(nuevas);
                await _dwContext.SaveChangesAsync();
            }

            result.RegistrosInsertados = nuevas.Count;
            _logger.LogInfo($"Dim_Clasificacion -> Nuevas: {nuevas.Count}, Ya existentes: {result.RegistrosProcesados - nuevas.Count}");
        });

    private async Task<EtlResult> EjecutarCargaAsync(string nombreTabla, Func<EtlResult, Task> accion)
    {
        var result = new EtlResult { NombreTabla = nombreTabla };

        try
        {
            _dwContext.ChangeTracker.Clear();
            await accion(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cargando la dimensión {nombreTabla} en el DataWarehouse.");
            throw;
        }
    }
}