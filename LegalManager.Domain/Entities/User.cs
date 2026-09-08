namespace LegalManager.Domain.Entities
{
    public abstract class User
    {
        public Guid Id { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Dni { get; private set; }
        public string Email { get; private set; }
        public string Password { get; private set; }
        public DateOnly RegistrationDate { get; private set; }
        public bool Active { get; private set; }

        protected User()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Dni = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
        }

        protected User(string firstName, string lastName, string dni, string email, string password)
            : this()
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("El nombre es obligatorio.", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("El apellido es obligatorio.", nameof(lastName));

            if (string.IsNullOrWhiteSpace(dni))
                throw new ArgumentException("El DNI es obligatorio.", nameof(dni));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email es obligatorio.", nameof(email));

            Id = Guid.NewGuid();
            FirstName = firstName;
            LastName = lastName;
            Dni = dni;
            Email = email;
            Password = password;
            RegistrationDate = DateOnly.FromDateTime(DateTime.Now);
            Active = true;
        }

        public void UpdateDetails(string firstName, string lastName, string dni, string email)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("El nombre es obligatorio.", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("El apellido es obligatorio.", nameof(lastName));

            if (string.IsNullOrWhiteSpace(dni))
                throw new ArgumentException("El DNI es obligatorio.", nameof(dni));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email es obligatorio.", nameof(email));

            FirstName = firstName;
            LastName = lastName;
            Dni = dni;
            Email = email;
        }

        public void Deactivate()
        {
            if (!Active)
                throw new InvalidOperationException("El usuario ya está inactivo.");

            Active = false;
        }
    }
}