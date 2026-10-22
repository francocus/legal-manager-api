namespace LegalManager.Domain
{
    // DNI argentino: 7 u 8 digitos, opcionalmente con puntos o espacios como
    // separadores visuales. La normalizacion (solo digitos) es la unica fuente
    // para el almacenamiento y para las comparaciones de unicidad, de modo que
    // "27.894.561" y "27894561" sean siempre equivalentes.
    public static class DniFormat
    {
        public static string Normalize(string dni)
            => (dni ?? string.Empty).Replace(".", "").Replace(" ", "");
    }
}
