namespace LegalManager.Infrastructure.ExternalServices
{
    public class LlmResilienceOptions
    {
        public const string SectionName = "Llm:Resilience";

        public int RetryCount { get; set; } = 3;

        public int InitialDelaySeconds { get; set; } = 2;

        public double BackoffFactor { get; set; } = 2.0;

        public int BreakerFailuresBeforeOpen { get; set; } = 5;

        public int BreakerOpenDurationSeconds { get; set; } = 30;

        public int TimeoutSeconds { get; set; } = 20;
    }
}
