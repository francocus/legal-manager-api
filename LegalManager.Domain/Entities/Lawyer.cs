namespace LegalManager.Domain.Entities
{
    public class Lawyer : User
    {
        public string BarNumber { get; private set; }
        public string? Phone { get; private set; }
        public IReadOnlyList<string> Specialties { get; private set; }

        private Lawyer()
        {
            BarNumber = string.Empty;
            Specialties = Array.Empty<string>();
        }

        public Lawyer(string firstName, string lastName, string dni, string email, string password, string barNumber, string? phone, IEnumerable<string>? specialties = null)
            : base(firstName, lastName, dni, email, password)
        {
            if (string.IsNullOrWhiteSpace(barNumber))
                throw new ArgumentException("La matrícula es obligatoria.", nameof(barNumber));

            BarNumber = barNumber;
            Phone = phone;
            Specialties = (specialties ?? []).ToList().AsReadOnly();
        }

        public void UpdateBarNumber(string barNumber)
        {
            if (string.IsNullOrWhiteSpace(barNumber))
                throw new ArgumentException("La matrícula es obligatoria.", nameof(barNumber));

            BarNumber = barNumber;
        }

        public void UpdatePhone(string? phone)
        {
            Phone = phone;
        }

        public void UpdateSpecialties(IEnumerable<string> specialties)
        {
            var cleaned = specialties?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? [];

            if (cleaned.Count == 0)
                throw new ArgumentException("Se requiere al menos una especialidad.", nameof(specialties));

            Specialties = cleaned.AsReadOnly();
        }
    }
}