using CsvHelper;
using Microsoft.Extensions.Configuration;
using OpinionesETL.Data.Extractors.Interfaces;

namespace OpinionesETL.Data.Extractors
{
    public class CsvExtractor : IExtractor<CSV.Surveys>
    {
        private readonly string _filePath;

        public CsvExtractor(IConfiguration configuration)
        {
            _filePath = configuration["SurveysFilePath"] ?? string.Empty;
        }

        public async Task<List<CSV.Surveys>> ExtractAsync()
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                throw new FileNotFoundException($"Archivo CSV no encontrado en la ruta: '{_filePath}'");

            var records = new List<CSV.Surveys>();
            using var reader = new StreamReader(_filePath);
            using var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);

            await foreach (var record in csv.GetRecordsAsync<CSV.Surveys>())
            {
                records.Add(record);
            }

            return records;
        }
    }
}