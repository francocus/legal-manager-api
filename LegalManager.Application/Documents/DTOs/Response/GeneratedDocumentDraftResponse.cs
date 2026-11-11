namespace LegalManager.Application.Documents.DTOs
{
    // Confirm-first contract: generation never persists anything. The draft
    // (including its PDF as base64) goes back to the client and only an
    // explicit POST /api/document/generated call stores it.
    public record GeneratedDocumentDraftResponse(
        string FileName,
        string ContentType,
        long SizeBytes,
        string SummaryText,
        string PdfBase64);
}
