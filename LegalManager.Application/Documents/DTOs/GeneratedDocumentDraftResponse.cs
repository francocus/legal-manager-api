namespace LegalManager.Application.Documents.DTOs
{
    public record GeneratedDocumentDraftResponse(
        string FileName,
        string ContentType,
        long SizeBytes,
        string SummaryText,
        string PdfBase64);
}