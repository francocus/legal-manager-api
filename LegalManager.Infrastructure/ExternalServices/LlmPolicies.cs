using System.Net;
using Polly;

namespace LegalManager.Infrastructure.ExternalServices
{
    public static class LlmPolicies
    {
        // Per-attempt deadline: lives between the circuit breaker and the HTTP send, unlike
        // HttpClient.Timeout, which would wrap the whole retry chain.
        public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(LlmResilienceOptions options)
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(options.TimeoutSeconds));
        }

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(LlmResilienceOptions options)
        {
            return Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(IsTransientFailure)
                .WaitAndRetryAsync(
                    options.RetryCount,
                    attempt => TimeSpan.FromSeconds(
                        options.InitialDelaySeconds * Math.Pow(options.BackoffFactor, attempt - 1)));
        }

        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(LlmResilienceOptions options)
        {
            return Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(IsTransientFailure)
                .CircuitBreakerAsync(
                    options.BreakerFailuresBeforeOpen,
                    TimeSpan.FromSeconds(options.BreakerOpenDurationSeconds));
        }

        // Only transient failures are retried/counted: network errors, server 5xx,
        // 408 (server timeout) and 429 (rate limit). A 400/401/403 will not succeed on retry.
        private static bool IsTransientFailure(HttpResponseMessage response)
        {
            return (int)response.StatusCode >= 500
                || response.StatusCode == HttpStatusCode.RequestTimeout
                || response.StatusCode == HttpStatusCode.TooManyRequests;
        }
    }
}
