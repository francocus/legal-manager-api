namespace LegalManager.Infrastructure.ExternalServices
{
    public class LlmOptions
    {
        public const string SectionName = "Llm";

        public string BaseUrl { get; set; } = string.Empty;

        // Structural only: the real ApiKey lives in user-secrets ("Llm:ApiKey"),
        // it must never be hardcoded or committed.
        public string ApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string ReasoningEffort { get; set; } = string.Empty;

        public string SessionId { get; set; } = string.Empty;

        public int MaxOutputTokens { get; set; } = 2000;

        public double Temperature { get; set; } = 0.4;

        public LlmResilienceOptions Resilience { get; set; } = new();
    }
}