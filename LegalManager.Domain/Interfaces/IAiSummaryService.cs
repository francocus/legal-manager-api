namespace LegalManager.Domain.Interfaces
{
    public interface IAiSummaryService
    {
        Task<string> GenerateSummaryAsync(string caseContext, CancellationToken cancellationToken = default);
    }
}