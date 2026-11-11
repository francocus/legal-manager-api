namespace LegalManager.Application.Documents.Interfaces
{
    public interface IPdfDocumentRenderer
    {
        byte[] RenderSummaryPdf(SummaryPdfData data);

        byte[] RenderConstanciaPdf(ConstanciaPdfData data);
    }

    public record SummaryPdfData(string CaseNumber, string Title, string Area, string Status, string StartDate, string LastUpdate, string? ClosingDate, string Description, string Notes, string SummaryText);

    public record ConstanciaPdfData(string CaseNumber, string Title, string ClientName, string LawyerNames, string Area, string Status, string StartDate, string LastUpdate, string Description, string GeneratedAt);
}