namespace OpinionesETL.Data.Extractors.Interfaces
{
    public interface IExtractor<T>
    {
        Task<List<T>> ExtractAsync(CancellationToken cancellationToken = default);
    }
}