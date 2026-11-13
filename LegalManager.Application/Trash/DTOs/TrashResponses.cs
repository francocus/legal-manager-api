using LegalManager.Application.Appointments.DTOs;
using LegalManager.Application.Documents.DTOs;
using LegalManager.Application.Users.DTOs;
using LegalManager.Domain.Entities;

namespace LegalManager.Application.Trash.DTOs
{
    public record TrashCaseResponse(
        Guid Id, string CaseNumber, string Title, DateOnly? DeactivatedAt,
        int TotalAppointments, int TotalDocuments)
    {
        public static TrashCaseResponse Desde(Case c, int totalAppointments, int totalDocuments) => new(
            c.Id, c.CaseNumber, c.Title, c.DeactivatedAt, totalAppointments, totalDocuments);
    }

    public record TrashAppointmentResponse(AppointmentResponse Appointment, DateOnly? DeactivatedAt)
    {
        public static TrashAppointmentResponse Desde(Appointment a) => new(AppointmentResponse.Desde(a), a.DeactivatedAt);
    }

    public record TrashDocumentResponse(DocumentResponse Document, DateOnly? DeactivatedAt, string? CaseNumber, string? CaseTitle)
    {
        public static TrashDocumentResponse Desde(Document d, string? caseNumber, string? caseTitle) => new(
            DocumentResponse.Desde(d), d.DeactivatedAt, caseNumber, caseTitle);
    }

    public record TrashUserResponse(UserResponse User, DateOnly? DeactivatedAt, bool IsAdmin)
    {
        public static TrashUserResponse Desde(User user) => new(
            UserResponse.Desde(user, includePrivateData: true), user.DeactivatedAt, user is Admin);
    }

    public record TrashResponse(
        IReadOnlyList<TrashCaseResponse> Cases,
        IReadOnlyList<TrashAppointmentResponse> Appointments,
        IReadOnlyList<TrashDocumentResponse> Documents,
        IReadOnlyList<TrashUserResponse> Users);
}
