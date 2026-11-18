using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpinionesETL.Data.Daos;
using OpinionesETL.Data.Daos.Interfaces;
using OpinionesETL.Data.Extractors;
using OpinionesETL.Data.Extractors.Interfaces;
using OpinionesETL.Data.Logging;
using OpinionesETL.Data.Models;
using OpinionesETL.Data.Services;
using OpinionesETL.Load;

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

        // Inversión de Dependencias (DIP): Extractores registrados por interfaz
        services.AddHttpClient();
        services.AddTransient<IExtractor<OpinionesETL.Data.CSV.Surveys>, CsvExtractor>();
        services.AddTransient<IExtractor<WebReview>, DatabaseExtractor>();
        services.AddTransient<IExtractor<SocialComment>, ApiExtractor>();

        // Registramos también las clases concretas por si ExtractionService las inyecta directamente
        services.AddTransient<CsvExtractor>();
        services.AddTransient<DatabaseExtractor>();
        services.AddTransient<ApiExtractor>();
        services.AddTransient<ExtractionService>();

        // DAOs
        services.AddTransient<IClienteDao, ClienteDao>();
        services.AddTransient<IProductoDao, ProductoDao>();
        services.AddTransient<IFuenteDatoDao, FuenteDatoDao>();
        services.AddTransient<IWebReviewDao, WebReviewDao>();
        services.AddTransient<ISurveyDao, SurveyDao>();
        services.AddTransient<ISocialCommentDao, SocialCommentDao>();

        // Services (Transformación + Carga)
        services.AddTransient<ClienteService>();
        services.AddTransient<ProductoService>();
        services.AddTransient<FuenteDatoService>();
        services.AddTransient<WebReviewService>();
        services.AddTransient<SurveyService>();
        services.AddTransient<SocialCommentService>();

        // Worker Service que orquesta todo el pipeline
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();