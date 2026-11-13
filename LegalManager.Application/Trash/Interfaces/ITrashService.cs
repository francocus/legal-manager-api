using LegalManager.Application.Trash.DTOs;

namespace LegalManager.Application.Trash.Interfaces
{
    public interface ITrashService
    {
        Task<TrashResponse> GetTrashAsync(CancellationToken cancellationToken = default);

        Task<bool> RestoreCaseAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> RestoreAppointmentAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> RestoreDocumentAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> RestoreUserAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
