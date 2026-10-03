using System.ComponentModel.DataAnnotations;
using LegalManager.Domain;

namespace LegalManager.Application.DTOs
{
    public record CreateAppointmentRequest(
        [param: StringLength(FieldLengths.Title)] string Title,
        DateOnly Date,
        TimeOnly Time,
        TimeOnly EndTime,
        [param: StringLength(FieldLengths.Reason)] string? Reason,
        [param: StringLength(FieldLengths.Area)] string? Area,
        [param: StringLength(FieldLengths.Location)] string? Location,
        [param: StringLength(FieldLengths.LongText)] string? Notes,
        Guid ClientId,
        Guid LawyerId,
        Guid? CaseId);
}