namespace LegalManager.Application.DTOs
{
    // No FileName on purpose: the server decides it from GeneratedByAi
    // ("ResumenIA.pdf" / "Constancia.pdf") so client input cannot spoof it.
    public record SaveGeneratedDocumentRequest(Guid CaseId, string PdfBase64, string SummaryText, bool GeneratedByAi);
}
