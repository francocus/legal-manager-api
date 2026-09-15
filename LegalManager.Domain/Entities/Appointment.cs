namespace LegalManager.Domain.Entities
{
    public class Appointment
    {
        public static readonly IReadOnlyList<TimeOnly> ValidSlots =
            [new(9, 0), new(10, 30), new(12, 0), new(14, 0), new(15, 30), new(17, 0)];

        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public DateOnly Date { get; private set; }
        public TimeOnly Time { get; private set; }
        public TimeOnly EndTime { get; private set; }
        public string? Reason { get; private set; }
        public AppointmentStatus Status { get; private set; }
        public AppointmentStatus EffectiveStatus =>
            Status == AppointmentStatus.Confirmado && Date.ToDateTime(Time) < DateTime.Now
                ? AppointmentStatus.Finalizado
                : Status;
        public string? Area { get; private set; }
        public string? Location { get; private set; }
        public string? Notes { get; private set; }
        public Guid ClientId { get; private set; }
        public Guid LawyerId { get; private set; }
        public Guid? CaseId { get; private set; }
        public bool Active { get; private set; }

        private Appointment()
        {
            Title = string.Empty;
            Reason = string.Empty;
        }

        public Appointment(string title, DateOnly date, TimeOnly time, TimeOnly endTime, string? reason, string? area, string? location, string? notes, Guid clientId, Guid lawyerId, Guid? caseId)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("El título es obligatorio.", nameof(title));

            if (clientId == Guid.Empty)
                throw new ArgumentException("El turno necesita un cliente asignado.", nameof(clientId));

            if (lawyerId == Guid.Empty)
                throw new ArgumentException("El turno necesita un abogado asignado.", nameof(lawyerId));

            if (!ValidSlots.Contains(time))
                throw new ArgumentException($"Horario inválido. Valores permitidos: {string.Join(", ", ValidSlots)}.", nameof(time));

            Id = Guid.NewGuid();
            Title = title;
            Date = date;
            Time = time;
            EndTime = endTime;
            Reason = reason;
            Status = AppointmentStatus.Pendiente;
            Area = area;
            Location = location;
            Notes = notes;
            ClientId = clientId;
            LawyerId = lawyerId;
            CaseId = caseId;
            Active = true;
        }

        public void Confirm()
        {
            if (Status == AppointmentStatus.Cancelado)
                throw new InvalidOperationException("No se puede confirmar un turno cancelado.");

            if (Status == AppointmentStatus.Confirmado)
                throw new InvalidOperationException("El turno ya está confirmado.");

            Status = AppointmentStatus.Confirmado;
        }

        public void Cancel()
        {
            if (Status == AppointmentStatus.Cancelado)
                throw new InvalidOperationException("El turno ya está cancelado.");

            Status = AppointmentStatus.Cancelado;
        }

        public void Reschedule(DateOnly newDate, TimeOnly newTime, TimeOnly newEndTime)
        {
            if (Status == AppointmentStatus.Cancelado)
                throw new InvalidOperationException("No se puede reprogramar un turno cancelado.");

            if (!ValidSlots.Contains(newTime))
                throw new ArgumentException($"Horario inválido. Valores permitidos: {string.Join(", ", ValidSlots)}.", nameof(newTime));

            Date = newDate;
            Time = newTime;
            EndTime = newEndTime;
            Status = AppointmentStatus.Pendiente;
        }

        public void UpdateNotes(string? location, string? notes)
        {
            Location = location;
            Notes = notes;
        }

        public void Deactivate()
        {
            if (!Active)
                throw new InvalidOperationException("El turno ya está inactivo.");

            Active = false;
        }

        public bool OverlapsWith(DateOnly date, TimeOnly time)
            => Active && Status != AppointmentStatus.Cancelado && Date == date && Time == time;
    }
}