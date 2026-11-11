using System.Net;
using Polly;
using Polly.Timeout;

namespace LegalManager.Infrastructure.AI
{
    public static class LlmPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(LlmResilienceOptions options)
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(options.TimeoutSeconds));
        }

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(LlmResilienceOptions options)
        {
            return Policy
                .Handle<HttpRequestException>()
                .Or<TimeoutRejectedException>()
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
                .Or<TimeoutRejectedException>()
                .OrResult<HttpResponseMessage>(IsTransientFailure)
                .CircuitBreakerAsync(
                    options.BreakerFailuresBeforeOpen,
                    TimeSpan.FromSeconds(options.BreakerOpenDurationSeconds));
        }

        private static bool IsTransientFailure(HttpResponseMessage response)
        {
            return (int)response.StatusCode >= 500
                || response.StatusCode == HttpStatusCode.RequestTimeout
                || response.StatusCode == HttpStatusCode.TooManyRequests;
        }
    }
}