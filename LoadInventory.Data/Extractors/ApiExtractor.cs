using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OpinionesETL.Data.Extractors.Interfaces;
using OpinionesETL.Data.Models;

namespace OpinionesETL.Data.Extractors
{
    public class ApiExtractor : IExtractor<SocialComment>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiUrl = "https://dummyjson.com/comments?limit=0";

        public ApiExtractor(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<SocialComment>> ExtractAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetFromJsonAsync<ApiResponse>(ApiUrl);

            if (response?.Comments == null)
                return new List<SocialComment>();

            return response.Comments.Select(c => new SocialComment
            {
                IdComment  = c.Id,
                Comentario = c.Body,
                Fuente     = "API-DummyJSON",
                Fecha      = DateOnly.FromDateTime(DateTime.Today),
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