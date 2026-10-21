using System.Net;
using Polly;

namespace LegalManager.Infrastructure.ExternalServices
{
    public static class LlmPolicies
    {
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

        // Solo se reintentan/cuentan fallos transitorios: errores de red, 5xx del servidor,
        // 408 (timeout del server) y 429 (rate limit). Un 400/401/403 no cambia si se reintenta.
        private static bool IsTransientFailure(HttpResponseMessage response)
        {
            return (int)response.StatusCode >= 500
                || response.StatusCode == HttpStatusCode.RequestTimeout
                || response.StatusCode == HttpStatusCode.TooManyRequests;
        }
    }
}
