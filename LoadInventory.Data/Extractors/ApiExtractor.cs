using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using OpinionesETL.Data.Extractors.Interfaces;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Extractors
{
    public class ApiExtractor : IExtractor<SocialComment>
    {
        private const string DefaultApiUrl = "https://dummyjson.com/comments?limit=0";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiUrl;

        public ApiExtractor(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiUrl = configuration["ApiUrl"] ?? DefaultApiUrl;
        }

        public async Task<List<SocialComment>> ExtractAsync(CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient();

            using var httpResponse = await client.GetAsync(_apiUrl, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content
                .ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken);

            if (response?.Comments == null)
                return new List<SocialComment>();

            // La API no trae fecha se usa la fecha de extracción.
            var fechaExtraccion = DateOnly.FromDateTime(DateTime.Today);

            return response.Comments.Select(c => new SocialComment
            {
                IdComment  = c.Id,
                Comentario = c.Body,
                Fuente     = "API-DummyJSON",
                Fecha      = fechaExtraccion,
                IdCliente  = null,
                IdProducto = c.PostId
            }).ToList();
        }

        private class ApiResponse
        {
            [JsonPropertyName("comments")]
            public List<ApiComment> Comments { get; set; } = new();
        }

        private class ApiComment
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("body")]
            public string Body { get; set; } = string.Empty;

            [JsonPropertyName("postId")]
            public int PostId { get; set; }
        }
    }
}