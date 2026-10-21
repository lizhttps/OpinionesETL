

namespace OpinionesETL.Data.Logging
{
    public interface IAppLogger<T>
    {
        void LogInfo(string mensaje);
        void LogWarning(string mensaje);
        void LogError(Exception ex, string mensaje);
    }
}