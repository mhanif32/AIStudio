using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TechUnited_AiStudio.Models;

namespace TechUnited_AiStudio.Services;

public class OllamaService
{
    private readonly HttpClient _chatClient;      // Port 11434
    private readonly HttpClient _embeddingClient; // Port 11444
    private readonly IHttpClientFactory _httpClientFactory;

    private const string ImageBridgeUrl = "http://10.0.0.103:11435/api/generate";

    public OllamaService(HttpClient chatClient, IHttpClientFactory httpClientFactory)
    {
        _chatClient = chatClient;
        _httpClientFactory = httpClientFactory;
        _embeddingClient = _httpClientFactory.CreateClient("EmbeddingClient");
    }

    // --- NEW: Chat Response with History and System Context ---
    public async Task<string> GetChatResponse(List<ChatMessage> history, string systemMessage)
    {
        try
        {
            // 1. Prepare messages array starting with the System Context (the RAG data)
            var messages = new List<object>
            {
                new { role = "system", content = systemMessage }
            };

            // 2. Add the last 10 messages of history to keep the AI on track
            foreach (var msg in history.TakeLast(10))
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            var requestBody = new
            {
                model = "llama3",
                messages = messages,
                stream = false
            };

            // 3. Use /api/chat instead of /api/generate for multi-turn conversations
            var response = await _chatClient.PostAsJsonAsync("api/chat", requestBody);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                // In /api/chat, the response is nested in message.content
                if (json.TryGetProperty("message", out var messageProp))
                {
                    return messageProp.GetProperty("content").GetString() ?? "No content returned.";
                }
            }
            return $"Error: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"Connection Error: {ex.Message}";
        }
    }

    // --- Generate Embeddings for RAG ---
    public async Task<float[]> GetEmbeddingAsync(string text, bool isQuery = false)
    {
        try
        {
            string prefix = isQuery ? "search_query: " : "search_document: ";
            var requestBody = new { model = "nomic-embed-text", prompt = prefix + text };

            var response = await _embeddingClient.PostAsJsonAsync("api/embeddings", requestBody);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
                return result?.Embedding ?? Array.Empty<float>();
            }
            return Array.Empty<float>();
        }
        catch { return Array.Empty<float>(); }
    }

    // Keep your existing methods for Completion, Streaming, Models, and Images...
    public async Task<string> GetCompletion(string prompt)
    {
        try
        {
            var requestBody = new { model = "llama3", prompt = prompt, stream = false };
            var response = await _chatClient.PostAsJsonAsync("api/generate", requestBody);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                return json.GetProperty("response").GetString() ?? "No response.";
            }
            return $"Error: {response.StatusCode}";
        }
        catch (Exception ex) { return $"Error: {ex.Message}"; }
    }

    public async Task<string?> GenerateImageAsync(string prompt)
    {
        try
        {
            var response = await _chatClient.PostAsJsonAsync(ImageBridgeUrl, new { prompt = prompt });
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                return json.TryGetProperty("response", out var base64Data) ? base64Data.GetString() : null;
            }
        }
        catch { }
        return null;
    }

    private class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}