namespace LegalManager.Application.Documents.DTOs
{
    public record SaveGeneratedDocumentRequest(Guid CaseId, string PdfBase64, string SummaryText, bool GeneratedByAi);
}