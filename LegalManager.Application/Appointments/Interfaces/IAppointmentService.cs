using LegalManager.Application.Auth.DTOs;
using LegalManager.Application.Users.DTOs;
using LegalManager.Application.Cases.DTOs;
using LegalManager.Application.Appointments.DTOs;
using LegalManager.Application.Documents.DTOs;
using LegalManager.Application.Common;

namespace LegalManager.Application.Appointments.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AppointmentResponse>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<PagedResponse<AppointmentResponse>> GetPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TimeOnly>> GetAvailabilityAsync(Guid lawyerId, DateOnly date, CancellationToken cancellationToken = default);

        Task<AppointmentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<AppointmentResponse?> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);

        Task<AppointmentResponse?> CancelAsync(Guid id, CancellationToken cancellationToken = default);

        Task<AppointmentResponse?> RescheduleAsync(Guid id, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}