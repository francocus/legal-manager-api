using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LegalManager.Domain;
using LegalManager.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace LegalManager.Infrastructure.ExternalServices
{
    public class OpenCodeAiSummaryService(HttpClient httpClient, IOptions<LlmOptions> options) : IAiSummaryService
    {
        // Product copy: the prompt is behavior, not code, so it stays in Spanish and in
        // sync with the summaries the firm delivers. It forbids inventing anything that
        // is not already registered in the case.
        private const string SystemPrompt =
            "Redactás resúmenes de expedientes para un estudio jurídico. Te paso los datos registrados del expediente. Escribí un resumen profesional, claro y objetivo en español, usando EXCLUSIVAMENTE esos datos: no inventes hechos, fechas, montos, nombres ni documentos que no figuren. Devolvé solo el texto del resumen, en párrafos breves, sin títulos ni formato markdown.";

        private readonly LlmOptions llmSettings = options.Value;

        public async Task<string> GenerateSummaryAsync(string caseContext, CancellationToken cancellationToken = default)
        {
            // Configuration problem, not an upstream outage: caught here so the whole API
            // starts fine without user-secrets and only the summary endpoint degrades to 502.
            if (string.IsNullOrWhiteSpace(llmSettings.ApiKey))
                throw new AiSummaryUnavailableException("Falta la API key del servicio de IA ('Llm:ApiKey' en user-secrets). Configúrala y volvé a intentar.");

            var request = new ChatCompletionRequest
            {
                Model = llmSettings.Model,
                Messages = [new ChatMessage("system", SystemPrompt), new ChatMessage("user", caseContext)],
                Temperature = llmSettings.Temperature,
                MaxTokens = llmSettings.MaxOutputTokens,
                ReasoningEffort = string.IsNullOrWhiteSpace(llmSettings.ReasoningEffort) ? null : llmSettings.ReasoningEffort
            };

            try
            {
                using var response = await httpClient.PostAsJsonAsync("v1/chat/completions", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    throw new AiSummaryUnavailableException($"El servicio de IA no está disponible ahora mismo (código {(int)response.StatusCode}).");

                var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);
                var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrWhiteSpace(content))
                    throw new AiSummaryUnavailableException("El servicio de IA no devolvió un resumen.");

                return content.Trim();
            }
            catch (AiSummaryUnavailableException)
            {
                throw;
            }
            catch (BrokenCircuitException)
            {
                throw new AiSummaryUnavailableException("El servicio de IA está temporalmente fuera de servicio (circuit breaker abierto). Volvé a intentar en unos minutos.");
            }
            catch (TimeoutRejectedException)
            {
                throw new AiSummaryUnavailableException("El servicio de IA no respondió dentro del tiempo configurado.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw new AiSummaryUnavailableException("El servicio de IA no respondió dentro del tiempo configurado.");
            }
            catch (HttpRequestException)
            {
                throw new AiSummaryUnavailableException("No se pudo conectar con el servicio de IA.");
            }
            catch (JsonException)
            {
                throw new AiSummaryUnavailableException("El servicio de IA devolvió una respuesta con formato inesperado.");
            }
        }

        private sealed class ChatCompletionRequest
        {
            [JsonPropertyName("model")] public string Model { get; init; } = string.Empty;

            [JsonPropertyName("messages")] public IReadOnlyList<ChatMessage> Messages { get; init; } = [];

            [JsonPropertyName("temperature")] public double Temperature { get; init; }

            [JsonPropertyName("max_tokens")] public int MaxTokens { get; init; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("reasoning_effort")] public string? ReasoningEffort { get; init; }
        }

        private sealed class ChatMessage(string role, string content)
        {
            [JsonPropertyName("role")] public string Role { get; init; } = role;

            [JsonPropertyName("content")] public string Content { get; init; } = content;
        }

        private sealed class ChatCompletionResponse
        {
            [JsonPropertyName("choices")] public IReadOnlyList<ChatChoice> Choices { get; init; } = [];
        }

        private sealed class ChatChoice
        {
            [JsonPropertyName("message")] public ChatMessage? Message { get; init; }
        }
    }
}