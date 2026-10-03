using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;

namespace LegalManager.Application.Services
{
    public class UserService(
        IUserRepository usersRepository,
        ICaseRepository casesRepository,
        IAppointmentRepository appointmentsRepository,
        IPasswordHasher passwordHasher,
        ICurrentUser currentUser) : IUserService
    {
        private string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña es obligatoria.", nameof(password));

            return passwordHasher.Hash(password);
        }

        private void EnsureUnique(string email, string dni, Guid? excludeId = null)
        {
            var users = usersRepository.GetAll();
            var normalizedEmail = email?.Trim();

            if (users.Any(u => u.Id != excludeId && string.Equals(u.Email.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Ya existe un usuario con ese email.");

            if (users.Any(u => u.Id != excludeId && u.Dni == dni))
                throw new InvalidOperationException("Ya existe un usuario con ese DNI.");
        }

        public UserResponse CreateClient(CreateClientRequest request)
        {
            EnsureUnique(request.Email, request.Dni);

            var client = new Client(request.FirstName, request.LastName, request.Dni, request.Email, HashPassword(request.Password), request.Phone, request.Address);
            usersRepository.Add(client);
            usersRepository.Save();
            return UserResponse.Desde(client, CanSeeDni(client));
        }

        public UserResponse CreateLawyer(CreateLawyerRequest request)
        {
            EnsureUnique(request.Email, request.Dni);

            var lawyer = new Lawyer(request.FirstName, request.LastName, request.Dni, request.Email, HashPassword(request.Password), request.BarNumber, request.Phone, request.Specialties);
            usersRepository.Add(lawyer);
            usersRepository.Save();
            return UserResponse.Desde(lawyer, CanSeeDni(lawyer));
        }

        public UserResponse CreateAdmin(CreateAdminRequest request)
        {
            EnsureUnique(request.Email, request.Dni);

            var admin = new Admin(request.FirstName, request.LastName, request.Dni, request.Email, HashPassword(request.Password));
            usersRepository.Add(admin);
            usersRepository.Save();
            return UserResponse.Desde(admin, CanSeeDni(admin));
        }

        public IReadOnlyList<UserResponse> GetAll() => usersRepository.GetAll().Select(u => UserResponse.Desde(u, CanSeeDni(u))).ToList();

        public IReadOnlyList<UserResponse> GetClients() => usersRepository.GetAll().OfType<Client>().Select(u => UserResponse.Desde(u, CanSeeDni(u))).ToList().AsReadOnly();

        public IReadOnlyList<UserResponse> GetLawyers() => usersRepository.GetAll().OfType<Lawyer>().Select(u => UserResponse.Desde(u, CanSeeDni(u))).ToList().AsReadOnly();

        public IReadOnlyList<UserResponse> GetAdmins() => usersRepository.GetAll().OfType<Admin>().Select(u => UserResponse.Desde(u, CanSeeDni(u))).ToList().AsReadOnly();

        public UserResponse? GetById(Guid id)
        {
            var user = usersRepository.GetById(id);
            if (user == null) return null;

            EnsureCanRead(user);

            return UserResponse.Desde(user, CanSeeDni(user));
        }

        public UserResponse? Update(Guid id, UpdateUserRequest request)
        {
            var user = usersRepository.GetById(id);
            if (user == null) return null;

            EnsureUnique(request.Email, request.Dni, id);

            user.UpdateDetails(request.FirstName, request.LastName, request.Dni, request.Email);
            usersRepository.Save();
            return UserResponse.Desde(user, CanSeeDni(user));
        }

        public UserResponse? UpdateClientPhone(Guid id, string? phone)
        {
            var user = usersRepository.GetById(id);
            if (user == null) return null;

            EnsureCanEditContact(user);

            if (user is not Client client)
                throw new ArgumentException("El usuario indicado no es un cliente.");

            client.UpdatePhone(phone);
            usersRepository.Save();
            return UserResponse.Desde(client, CanSeeDni(client));
        }

        public UserResponse? UpdateLawyerPhone(Guid id, string? phone)
        {
            var user = usersRepository.GetById(id);
            if (user == null) return null;

            EnsureCanEditContact(user);

            if (user is not Lawyer lawyer)
                throw new ArgumentException("El usuario indicado no es un abogado.");

            lawyer.UpdatePhone(phone);
            usersRepository.Save();
            return UserResponse.Desde(lawyer, CanSeeDni(lawyer));
        }

        public UserResponse? UpdateClientAddress(Guid id, string? address)
        {
            var user = usersRepository.GetById(id);
            if (user == null) return null;

            EnsureCanEditContact(user);

            if (user is not Client client)
                throw new ArgumentException("El usuario indicado no es un cliente.");

            client.UpdateAddress(address);
            usersRepository.Save();
            return UserResponse.Desde(client, CanSeeDni(client));
        }

        public UserResponse? UpdateBarNumber(Guid id, string barNumber)
        {
            var user = usersRepository.GetById(id);
            if (user == null) return null;

            if (user is not Lawyer lawyer)
                throw new ArgumentException("El usuario indicado no es un abogado.");

            lawyer.UpdateBarNumber(barNumber);
            usersRepository.Save();
            return UserResponse.Desde(lawyer, CanSeeDni(lawyer));
        }

        public UserResponse? UpdateSpecialties(Guid id, IEnumerable<string> specialties)
        {
            var user = usersRepository.GetById(id);
            if (user == null) return null;

            if (user is not Lawyer lawyer)
                throw new ArgumentException("El usuario indicado no es un abogado.");

            lawyer.UpdateSpecialties(specialties);
            usersRepository.Save();
            return UserResponse.Desde(lawyer, CanSeeDni(lawyer));
        }

        public bool Delete(Guid id)
        {
            var user = usersRepository.GetById(id);
            if (user == null) return false;

            if (user.Id == currentUser.Id)
                throw new ForbiddenException("Un usuario no puede darse de baja a sí mismo.");

            var hasActiveCases = casesRepository.GetAll()
                .Any(c => (c.ClientId == id || c.LawyerIds.Contains(id)) && c.Status != CaseStatus.Cerrado);

            var hasActiveAppointments = appointmentsRepository.GetAll()
                .Any(a => (a.ClientId == id || a.LawyerId == id)
                    && a.EffectiveStatus != AppointmentStatus.Cancelado && a.EffectiveStatus != AppointmentStatus.Finalizado);

            if (hasActiveCases || hasActiveAppointments)
                throw new InvalidOperationException($"El usuario con id {id} tiene expedientes o turnos activos y no puede ser desactivado.");

            user.Deactivate();
            usersRepository.Save();
            return true;
        }

        private bool CanSeeDni(User user) => currentUser.IsAdmin || user.Id == currentUser.Id;

        private void EnsureCanRead(User user)
        {
            if (currentUser.IsAdmin) return;

            if (user.Id == currentUser.Id) return;

            if (currentUser.IsLawyer && user is Client && IsClientLinkedToLawyer(user.Id)) return;

            if (currentUser.IsClient && user is Lawyer) return;

            throw new ForbiddenException("No tiene acceso a este usuario.");
        }

        private void EnsureCanEditContact(User user)
        {
            if (currentUser.IsAdmin) return;

            if (user.Id == currentUser.Id) return;

            throw new ForbiddenException("Solo puede modificar sus propios datos de contacto.");
        }

        private bool IsClientLinkedToLawyer(Guid clientId)
        {
            if (casesRepository.GetAll().Any(c => c.ClientId == clientId && c.LawyerIds.Contains(currentUser.Id)))
                return true;

            return appointmentsRepository.GetAll().Any(a => a.ClientId == clientId && a.LawyerId == currentUser.Id);
        }
    }
}