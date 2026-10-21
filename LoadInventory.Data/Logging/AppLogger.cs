using Microsoft.Extensions.Logging;

namespace OpinionesETL.Data.Logging
{
    public class AppLogger<T> : IAppLogger<T>
    {
        private readonly ILogger<T> _logger;

        public AppLogger(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void LogInfo(string mensaje)
        {
            _logger.LogInformation(mensaje);
        }

        public void LogWarning(string mensaje)
        {
            _logger.LogWarning(mensaje);
        }

        public void LogError(Exception ex, string mensaje)
        {
            _logger.LogError(ex, mensaje);
        }
    }
}
