using System.Text.Json;

namespace LegalManager.Presentation.Security
{
    // El framework emite 401/403 sin cuerpo (auth middleware y policies): el
    // cliente solo ve el codigo. Este middleware completa esas respuestas
    // vacias con el mismo formato ProblemDetails que usan los errores de
    // validacion de la API; los cuerpos ya escritos (por ejemplo el texto
    // plano de ForbiddenExceptionMiddleware) no se pisan.
    public class ProblemDetailsMiddleware(RequestDelegate next)
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task InvokeAsync(HttpContext context)
        {
            await next(context);

            var status = context.Response.StatusCode;
            if (status is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
                || context.Response.HasStarted
                || context.Response.ContentLength.HasValue
                || context.Response.ContentType != null)
                return;

            var problem = new
            {
                type = status == StatusCodes.Status401Unauthorized
                    ? "https://tools.ietf.org/html/rfc9110#section-15.5.2"
                    : "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                title = status == StatusCodes.Status401Unauthorized
                    ? "No autenticado. Iniciá sesión para continuar."
                    : "No tenés permisos para realizar esta operación.",
                status
            };

            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
    }
}
