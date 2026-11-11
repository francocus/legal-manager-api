using LegalManager.Application.DTOs;

namespace LegalManager.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AppointmentResponse>> GetAllAsync(CancellationToken cancellationToken = default);

        // Paged listing. With both params null this is never called: controllers keep the
        // plain-array behavior. With either param present, page defaults to 1 and pageSize
        // to 20; page/pageSize bounds are validated here (ArgumentException -> 400).
        Task<PagedResponse<AppointmentResponse>> GetPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TimeOnly>> GetAvailabilityAsync(Guid lawyerId, DateOnly date, CancellationToken cancellationToken = default);

        Task<AppointmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<AppointmentResponse?> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);

        Task<AppointmentResponse?> CancelAsync(Guid id, CancellationToken cancellationToken = default);

        Task<AppointmentResponse?> RescheduleAsync(Guid id, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}