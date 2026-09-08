namespace LegalManager.Domain.Entities
{
    public class Case
    {
        public Guid Id { get; private set; }
        public string CaseNumber { get; private set; }
        public string Title { get; private set; }
        public string Area { get; private set; }
        public CaseStatus Status { get; private set; }
        public DateOnly StartDate { get; private set; }
        public DateOnly LastUpdate { get; private set; }
        public DateOnly? ClosingDate { get; private set; }
        public string Description { get; private set; }
        public string? Notes { get; private set; }
        public Guid ClientId { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        private readonly List<Lawyer> lawyers = new List<Lawyer>();
        public IReadOnlyList<Guid> LawyerIds => lawyers.Select(l => l.Id).ToList().AsReadOnly();
        public bool Active { get; private set; }

        private Case()
        {
            CaseNumber = string.Empty;
            Title = string.Empty;
            Area = string.Empty;
            Description = string.Empty;
        }

        public Case(string caseNumber, string title, string area, DateOnly startDate, string description, string? notes, Guid clientId, Lawyer initialLawyer, Guid createdByUserId)
        {
            if (string.IsNullOrWhiteSpace(caseNumber))
                throw new ArgumentException("El número de expediente es obligatorio.", nameof(caseNumber));

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("El título es obligatorio.", nameof(title));

            if (string.IsNullOrWhiteSpace(area))
                throw new ArgumentException("El área es obligatoria.", nameof(area));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("La descripción es obligatoria.", nameof(description));

            if (clientId == Guid.Empty)
                throw new ArgumentException("El expediente necesita un cliente asignado.", nameof(clientId));

            if (initialLawyer == null)
                throw new ArgumentException("El expediente necesita un abogado asignado.", nameof(initialLawyer));

            if (createdByUserId == Guid.Empty)
                throw new ArgumentException("El expediente necesita un usuario que lo cree.", nameof(createdByUserId));

            Id = Guid.NewGuid();
            CaseNumber = caseNumber;
            Title = title;
            Area = area;
            Status = CaseStatus.Activo;
            StartDate = startDate;
            LastUpdate = startDate;
            Description = description;
            Notes = notes;
            ClientId = clientId;
            CreatedByUserId = createdByUserId;
            lawyers.Add(initialLawyer);
            Active = true;
        }

        public void UpdateDetails(string title, string area, string description, string? notes)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("El título es obligatorio.", nameof(title));

            if (string.IsNullOrWhiteSpace(area))
                throw new ArgumentException("El área es obligatoria.", nameof(area));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("La descripción es obligatoria.", nameof(description));

            if (Status == CaseStatus.Cerrado)
                throw new InvalidOperationException("No se puede modificar un expediente cerrado.");

            Title = title;
            Area = area;
            Description = description;
            Notes = notes;
            LastUpdate = DateOnly.FromDateTime(DateTime.Now);
        }

        public void ChangeStatus(CaseStatus newStatus)
        {
            if (Status == CaseStatus.Cerrado)
                throw new InvalidOperationException("No se puede reabrir un expediente cerrado.");

            Status = newStatus;
            LastUpdate = DateOnly.FromDateTime(DateTime.Now);

            if (newStatus == CaseStatus.Cerrado)
                ClosingDate = LastUpdate;
        }

        public void AddLawyer(Lawyer lawyer)
        {
            if (Status == CaseStatus.Cerrado)
                throw new InvalidOperationException("No se pueden modificar los abogados de un expediente cerrado.");

            if (lawyers.Any(l => l.Id == lawyer.Id))
                throw new InvalidOperationException("Este abogado ya está asignado al expediente.");

            lawyers.Add(lawyer);
        }

        public void RemoveLawyer(Guid lawyerId)
        {
            if (Status == CaseStatus.Cerrado)
                throw new InvalidOperationException("No se pueden modificar los abogados de un expediente cerrado.");

            if (lawyers.Count == 1)
                throw new InvalidOperationException("El expediente debe tener al menos un abogado asignado.");

            var lawyer = lawyers.FirstOrDefault(l => l.Id == lawyerId);
            if (lawyer == null)
                throw new InvalidOperationException("Este abogado no está asignado al expediente.");

            lawyers.Remove(lawyer);
        }

        public void Deactivate()
        {
            if (!Active)
                throw new InvalidOperationException("El expediente ya está inactivo.");

            Active = false;
        }
    }
}