namespace LegalManager.Infrastructure.ExternalServices
{
    /// <summary>
    /// Resilience knobs for the external LLM client.
    /// Worst case per request, no breaker: (RetryCount + 1) attempts x TimeoutSeconds
    /// plus retry backoff = 3 x 15s + (2s + 4s) = 51s before the 502 surfaces.
    /// </summary>
    public class LlmResilienceOptions
    {
        public const string SectionName = "Llm:Resilience";

        public int RetryCount { get; set; } = 2;

        public int InitialDelaySeconds { get; set; } = 2;

        public double BackoffFactor { get; set; } = 2.0;

        public int BreakerFailuresBeforeOpen { get; set; } = 5;

        public int BreakerOpenDurationSeconds { get; set; } = 30;

        public int TimeoutSeconds { get; set; } = 15;
    }
}
