using System.ComponentModel.DataAnnotations;
using LegalManager.Domain.ValueObjects;
using LegalManager.Application.Common.Validation;

namespace LegalManager.Application.Appointments.DTOs
{
    public record CreateAppointmentRequest(
        [param: StringLength(FieldLengths.Title)] string Title,
        DateOnly Date,
        TimeOnly Time,
        TimeOnly EndTime,
        [param: StringLength(FieldLengths.Reason)] string? Reason,
        [param: StringLength(FieldLengths.Area), LegalArea(AllowEmpty = true)] string? Area,
        [param: StringLength(FieldLengths.Location)] string? Location,
        [param: StringLength(FieldLengths.LongText)] string? Notes,
        Guid ClientId,
        Guid LawyerId,
        Guid? CaseId);
}