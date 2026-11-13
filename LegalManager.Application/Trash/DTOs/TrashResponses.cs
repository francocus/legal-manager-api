using LegalManager.Application.Appointments.DTOs;
using LegalManager.Application.Documents.DTOs;
using LegalManager.Application.Users.DTOs;
using LegalManager.Domain.Entities;

namespace LegalManager.Application.Trash.DTOs
{
    public record TrashCaseResponse(
        Guid Id, string CaseNumber, string Title, DateTime? DeactivatedAt,
        int TotalAppointments, int TotalDocuments)
    {
        public static TrashCaseResponse Desde(Case c, int totalAppointments, int totalDocuments) => new(
            c.Id, c.CaseNumber, c.Title,
            c.DeactivatedAt.HasValue ? DateTime.SpecifyKind(c.DeactivatedAt.Value, DateTimeKind.Utc) : null,
            totalAppointments, totalDocuments);
    }

    public record TrashAppointmentResponse(AppointmentResponse Appointment, DateTime? DeactivatedAt)
    {
        public static TrashAppointmentResponse Desde(Appointment a) => new(
            AppointmentResponse.Desde(a),
            a.DeactivatedAt.HasValue ? DateTime.SpecifyKind(a.DeactivatedAt.Value, DateTimeKind.Utc) : null);
    }

    public record TrashDocumentResponse(DocumentResponse Document, DateTime? DeactivatedAt, string? CaseNumber, string? CaseTitle)
    {
        public static TrashDocumentResponse Desde(Document d, string? caseNumber, string? caseTitle) => new(
            DocumentResponse.Desde(d),
            d.DeactivatedAt.HasValue ? DateTime.SpecifyKind(d.DeactivatedAt.Value, DateTimeKind.Utc) : null,
            caseNumber, caseTitle);
    }

    public record TrashUserResponse(UserResponse User, DateTime? DeactivatedAt, bool IsAdmin)
    {
        public static TrashUserResponse Desde(User user) => new(
            UserResponse.Desde(user, includePrivateData: true),
            user.DeactivatedAt.HasValue ? DateTime.SpecifyKind(user.DeactivatedAt.Value, DateTimeKind.Utc) : null,
            user is Admin);
    }

    public record TrashResponse(
        IReadOnlyList<TrashCaseResponse> Cases,
        IReadOnlyList<TrashAppointmentResponse> Appointments,
        IReadOnlyList<TrashDocumentResponse> Documents,
        IReadOnlyList<TrashUserResponse> Users);
}
