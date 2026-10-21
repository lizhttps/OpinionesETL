using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpinionesETL.Data.Daos;
using OpinionesETL.Data.Daos.Interfaces;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;
using OpinionesETL.Data.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Logging por consola
        services.AddLogging(builder =>
        {
            builder.AddConsole();
        });

        // DbContext con la cadena de conexión
        services.AddDbContext<CustomerReviewsDBContext>(options =>
            options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));

        // Logger propio desacoplado
        services.AddTransient(typeof(IAppLogger<>), typeof(AppLogger<>));

        // DAOs
        services.AddTransient<IClienteDao, ClienteDao>();
        services.AddTransient<IProductoDao, ProductoDao>();
        services.AddTransient<IFuenteDatoDao, FuenteDatoDao>();
        services.AddTransient<IWebReviewDao, WebReviewDao>();
        services.AddTransient<ISurveyDao, SurveyDao>();
        services.AddTransient<ISocialCommentDao, SocialCommentDao>();

        // Registro de los 6 Services
        services.AddTransient<ClienteService>();
        services.AddTransient<ProductoService>();
        services.AddTransient<FuenteDatoService>();
        services.AddTransient<WebReviewService>();
        services.AddTransient<SurveyService>();
        services.AddTransient<SocialCommentService>();
    })
    .Build();

Console.WriteLine("==================================================");
Console.WriteLine("       INICIANDO PIPELINE DE CARGA ETL           ");
Console.WriteLine("==================================================");

using var scope = host.Services.CreateScope();
var provider = scope.ServiceProvider;

// Lista para almacenar el resultado de cada carga
var resultados = new List<EtlResult>();

try
{
    // ------------------------------------------------------------------
    // FASE 1: LIMPIEZA GLOBAL 
    // ------------------------------------------------------------------
    Console.WriteLine("\n[FASE 1] Limpiando tablas (orden inverso de dependencias)...");

    await provider.GetRequiredService<ISocialCommentDao>().ClearSocialCommentAsync();
    await provider.GetRequiredService<ISurveyDao>().ClearSurveysAsync();
    await provider.GetRequiredService<IWebReviewDao>().ClearWebReviewsAsync();
    await provider.GetRequiredService<IProductoDao>().ClearProductoAsync();
    await provider.GetRequiredService<IClienteDao>().ClearClientesAsync();
    await provider.GetRequiredService<IFuenteDatoDao>().ClearFuenteDatosAsync();

    // ------------------------------------------------------------------
    // FASE 2: CARGA
    // ------------------------------------------------------------------
    Console.WriteLine("\n[FASE 2] Cargando datos...");

    // 1. Cargar tablas maestras (Padres sin dependencias FK)
    var clienteService = provider.GetRequiredService<ClienteService>();
    resultados.Add(await clienteService.LoadClientesAsync());

    var productoService = provider.GetRequiredService<ProductoService>();
    resultados.Add(await productoService.LoadProductosAsync());

    var fuenteDatoService = provider.GetRequiredService<FuenteDatoService>();
    resultados.Add(await fuenteDatoService.LoadFuenteDatosAsync());

    // 2. Cargar tablas dependientes (Hijas con FK hacia Clientes y Productos)
    var webReviewService = provider.GetRequiredService<WebReviewService>();
    resultados.Add(await webReviewService.LoadWebReviewsAsync());

    var surveyService = provider.GetRequiredService<SurveyService>();
    resultados.Add(await surveyService.LoadSurveysAsync());

    var socialCommentService = provider.GetRequiredService<SocialCommentService>();
    resultados.Add(await socialCommentService.LoadSocialCommentsAsync());
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[ERROR CRÍTICO EN PIPELINE]: {ex.Message}");
    Console.ResetColor();
}

// ----------------------------------------------------------------------
// RESUMEN FINAL DEL PROCESO ETL 
// ----------------------------------------------------------------------
Console.WriteLine("\n================================================================================");
Console.WriteLine("                         RESUMEN DEL PROCESO ETL                                ");
Console.WriteLine("================================================================================");
Console.WriteLine(string.Format("{0,-18} | {1,-10} | {2,-10} | {3,-10} | {4,-10}",
    "Tabla", "Procesados", "Insertados", "Rechazados", "Estado"));
Console.WriteLine(new string('-', 80));

int totalProcesados = 0;
int totalInsertados = 0;
int totalRechazados = 0;

foreach (var r in resultados)
{
    totalProcesados += r.RegistrosProcesados;
    totalInsertados += r.RegistrosInsertados;
    totalRechazados += r.RegistrosRechazados;

    string estado = r.Exitoso ? "OK" : "CON ERRORES";
    Console.WriteLine(string.Format("{0,-18} | {1,10} | {2,10} | {3,10} | {4,-10}",
        r.NombreTabla, r.RegistrosProcesados, r.RegistrosInsertados, r.RegistrosRechazados, estado));
}

Console.WriteLine(new string('-', 80));
Console.WriteLine(string.Format("{0,-18} | {1,10} | {2,10} | {3,10} |",
    "TOTALES GENERALES", totalProcesados, totalInsertados, totalRechazados));
Console.WriteLine("================================================================================\n");

// Mostrar detalle de rechazos si los hubo
var tablasConErrores = resultados.Where(r => r.RegistrosRechazados > 0).ToList();
if (tablasConErrores.Any())
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("DETALLE DE MOTIVOS DE RECHAZO (Primeros reportes por tabla):");
    Console.ResetColor();

    foreach (var r in tablasConErrores)
    {
        Console.WriteLine($"\n--> Tabla: {r.NombreTabla} ({r.RegistrosRechazados} filas rechazadas):");
        // Mostramos las primeras 5 razones para no saturar la pantalla
        foreach (var motivo in r.MotivosRechazo.Take(5))
        {
            Console.WriteLine($"    * {motivo}");
        }
        if (r.MotivosRechazo.Count > 5)
        {
            Console.WriteLine($"    * ... y {r.MotivosRechazo.Count - 5} rechazos adicionales.");
        }
    }
}

Console.WriteLine("\nPresione cualquier tecla para finalizar...");
Console.ReadKey();