using System.Text.Json.Serialization;

namespace LegalManager.Application.Common
{
    public record PagedResponse<T>([property: JsonPropertyName("items")] IReadOnlyList<T> Items, [property: JsonPropertyName("page")] int Page, [property: JsonPropertyName("pageSize")] int PageSize, [property: JsonPropertyName("total")] int Total);

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