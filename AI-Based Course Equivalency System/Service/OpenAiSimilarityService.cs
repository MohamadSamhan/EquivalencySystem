using Domine.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Service
{
    public class OpenAiSimilarityService : ISimilarityService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly ILogger<OpenAiSimilarityService>? _logger;
        private const string Model = "text-embedding-3-small";

        public OpenAiSimilarityService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<OpenAiSimilarityService>? logger = null)
        {
            _httpFactory = httpFactory;
            _apiKey = config["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:ApiKey not configured");
            _baseUrl = config["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
            _logger = logger;
        }

        // synchronous API required by existing codebase - wrapper around async implementation
        public double CalculateSimilarity(string a, string b)
        {
            return CalculateSimilarityAsync(a ?? string.Empty, b ?? string.Empty).GetAwaiter().GetResult();
        }

        private async Task<double> CalculateSimilarityAsync(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b))
                return 1.0;

            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return 0.0;

            try
            {
                _logger?.LogInformation("Starting similarity calculation: textA_len={LenA}, textB_len={LenB}", a.Length, b.Length);
                
                var vecA = await GetDocumentEmbeddingAsync(a);
                var vecB = await GetDocumentEmbeddingAsync(b);

                _logger?.LogInformation("Embeddings retrieved: vecA={VecA}, vecB={VecB}", vecA?.Length ?? 0, vecB?.Length ?? 0);

                if (vecA == null || vecB == null || vecA.Length != vecB.Length)
                {
                    _logger?.LogError("Embeddings missing or length mismatch: vecA={VecA} vecB={VecB}. Check OpenAI API key and connectivity.", vecA?.Length, vecB?.Length);
                    return 0.0;
                }

                var dot = 0.0;
                var normA = 0.0;
                var normB = 0.0;
                for (int i = 0; i < vecA.Length; i++)
                {
                    dot += vecA[i] * vecB[i];
                    normA += vecA[i] * vecA[i];
                    normB += vecB[i] * vecB[i];
                }

                if (normA == 0 || normB == 0) return 0.0;

                var cosine = dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
                var normalized = (cosine + 1.0) / 2.0;
                var result = Math.Clamp(normalized, 0.0, 1.0);
                
                _logger?.LogInformation("Similarity calculation complete: result={Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "OpenAiSimilarityService failed - check OpenAI API key, credentials, and account balance");
                return 0.0;
            }
        }

        // Create a single embedding for a long document by splitting into chunks and averaging chunk embeddings
        private async Task<double[]?> GetDocumentEmbeddingAsync(string text)
        {
            var chunks = ChunkText(text, 2000);
            var embeddings = new List<double[]>();

            foreach (var chunk in chunks)
            {
                var emb = await GetEmbeddingForChunkAsync(chunk);
                if (emb != null) embeddings.Add(emb);
                else _logger?.LogWarning("Chunk embedding returned null (chunk length {Len})", chunk?.Length ?? 0);
            }

            if (embeddings.Count == 0) return null;

            var length = embeddings[0].Length;
            var avg = new double[length];
            foreach (var e in embeddings)
            {
                for (int i = 0; i < length; i++) avg[i] += e[i];
            }
            for (int i = 0; i < length; i++) avg[i] /= embeddings.Count;

            return avg;
        }

        private async Task<double[]?> GetEmbeddingForChunkAsync(string chunk)
        {
            using var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(_baseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var reqObj = new
            {
                model = Model,
                input = chunk
            };

            var content = new StringContent(JsonSerializer.Serialize(reqObj), Encoding.UTF8, "application/json");
            
            _logger?.LogDebug("Sending embedding request to {BaseUrl}embeddings with model={Model}, chunk_length={ChunkLen}", _baseUrl, Model, chunk.Length);
            
            using var resp = await client.PostAsync("embeddings", content);

            var respText = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                _logger?.LogError("OpenAI embeddings API failed: Status={Status} Response={Response}. Verify: 1) API key is valid, 2) Account has credits, 3) Rate limits not exceeded", resp.StatusCode, Truncate(respText, 500));
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(respText);
                if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
                {
                    _logger?.LogError("OpenAI embeddings missing data field in response: {Response}", Truncate(respText, 500));
                    return null;
                }

                var embeddingEl = data[0].GetProperty("embedding");
                var list = new double[embeddingEl.GetArrayLength()];
                for (int i = 0; i < list.Length; i++) list[i] = embeddingEl[i].GetDouble();
                
                _logger?.LogDebug("Successfully got embedding of length {EmbeddingLen}", list.Length);
                return list;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to parse OpenAI embedding response: {Response}", Truncate(respText, 500));
                return null;
            }
        }

        private static IEnumerable<string> ChunkText(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            int pos = 0;
            while (pos < text.Length)
            {
                var len = Math.Min(maxChars, text.Length - pos);
                yield return text.Substring(pos, len);
                pos += len;
            }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        // Public wrapper so other parts of the app (debug endpoints) can request raw embeddings.
        public Task<double[]?> GetEmbeddingAsync(string text) => GetDocumentEmbeddingAsync(text);
    }
}
