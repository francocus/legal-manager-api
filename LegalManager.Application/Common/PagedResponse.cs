using System.Text.Json.Serialization;

namespace LegalManager.Application.Common
{
    // Optional paging envelope for list endpoints. Without page/pageSize query params
    // the endpoints keep returning the full plain arrays (backward compatible with the SPA).
    public record PagedResponse<T>([property: JsonPropertyName("items")] IReadOnlyList<T> Items, [property: JsonPropertyName("page")] int Page, [property: JsonPropertyName("pageSize")] int PageSize, [property: JsonPropertyName("total")] int Total);

    // Non-generic home for the shared paging defaults and validation. The Spanish
    // messages are user-facing copy: controllers map ArgumentException to 400.
    public static class PagedResponse
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
        {
            var currentPage = page ?? DefaultPage;
            var currentPageSize = pageSize ?? DefaultPageSize;

            if (currentPage < 1)
                throw new ArgumentException("La página debe ser mayor a 0.");

            if (currentPageSize < 1 || currentPageSize > MaxPageSize)
                throw new ArgumentException("El tamaño de página debe estar entre 1 y 100.");

            return (currentPage, currentPageSize);
        }
    }
}
