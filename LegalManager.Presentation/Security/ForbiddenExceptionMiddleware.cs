using LegalManager.Application;

namespace LegalManager.Presentation.Security
{
    public class ForbiddenExceptionMiddleware(RequestDelegate next, ILogger<ForbiddenExceptionMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (ForbiddenException ex)
            {
                logger.LogWarning(ex, "Acceso denegado: {Message}", ex.Message);

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync(ex.Message);
            }
        }
    }
}