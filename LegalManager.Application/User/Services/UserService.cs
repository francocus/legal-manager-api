using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain;
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

        private async Task EnsureUniqueAsync(string email, string dni, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var users = await usersRepository.GetAllAsync(cancellationToken);
            var normalizedEmail = email?.Trim();

            if (users.Any(u => u.Id != excludeId && string.Equals(u.Email.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Ya existe un usuario con ese email.");

            if (users.Any(u => u.Id != excludeId && u.Dni == DniFormat.Normalize(dni)))
                throw new InvalidOperationException("Ya existe un usuario con ese DNI.");
        }

        public async Task<UserResponse> CreateClientAsync(CreateClientRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureUniqueAsync(request.Email, request.Dni, null, cancellationToken);

            var client = new Client(request.FirstName, request.LastName, request.Dni, request.Email, HashPassword(request.Password), request.Phone, request.Address);
            usersRepository.Add(client);
            await usersRepository.SaveAsync(cancellationToken);
            return UserResponse.Desde(client, CanSeePrivateData(client));
        }

        public async Task<UserResponse> CreateLawyerAsync(CreateLawyerRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureUniqueAsync(request.Email, request.Dni, null, cancellationToken);

            var lawyer = new Lawyer(request.FirstName, request.LastName, request.Dni, request.Email, HashPassword(request.Password), request.BarNumber, request.Phone, request.Specialties);
            usersRepository.Add(lawyer);
            await usersRepository.SaveAsync(cancellationToken);
            return UserResponse.Desde(lawyer, CanSeePrivateData(lawyer));
        }

        public async Task<UserResponse> CreateAdminAsync(CreateAdminRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureUniqueAsync(request.Email, request.Dni, null, cancellationToken);

            var admin = new Admin(request.FirstName, request.LastName, request.Dni, request.Email, HashPassword(request.Password));
            usersRepository.Add(admin);
            await usersRepository.SaveAsync(cancellationToken);
            return UserResponse.Desde(admin, CanSeePrivateData(admin));
        }

        public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var users = await usersRepository.GetAllAsync(cancellationToken);
            return users.Select(u => UserResponse.Desde(u, CanSeePrivateData(u))).ToList();
        }

        public async Task<IReadOnlyList<UserResponse>> GetClientsAsync(CancellationToken cancellationToken = default)
        {
            var users = await usersRepository.GetAllAsync(cancellationToken);
            return users.OfType<Client>().Select(u => UserResponse.Desde(u, CanSeePrivateData(u))).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<UserResponse>> GetLawyersAsync(CancellationToken cancellationToken = default)
        {
            var users = await usersRepository.GetAllAsync(cancellationToken);
            return users.OfType<Lawyer>().Select(u => UserResponse.Desde(u, CanSeePrivateData(u))).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<UserResponse>> GetAdminsAsync(CancellationToken cancellationToken = default)
        {
            var users = await usersRepository.GetAllAsync(cancellationToken);
            return users.OfType<Admin>().Select(u => UserResponse.Desde(u, CanSeePrivateData(u))).ToList().AsReadOnly();
        }

        public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByIdAsync(id, cancellationToken);
            if (user == null) return null;

            await EnsureCanReadAsync(user, cancellationToken);

            return UserResponse.Desde(user, CanSeePrivateData(user));
        }

        public async Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByIdAsync(id, cancellationToken);
            if (user == null) return null;

            await EnsureUniqueAsync(request.Email, request.Dni, id, cancellationToken);

            user.UpdateDetails(request.FirstName, request.LastName, request.Dni, request.Email);
            await usersRepository.SaveAsync(cancellationToken);
            return UserResponse.Desde(user, CanSeePrivateData(user));
        }

        public async Task<UserResponse?> UpdateClientPhoneAsync(Guid id, string? phone, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByIdAsync(id, cancellationToken);
            if (user == null) return null;

            EnsureCanEditContact(user);

            if (user is not Client client)
                throw new ArgumentException("El usuario indicado no es un cliente.");

            client.UpdatePhone(phone);
            await usersRepository.SaveAsync(cancellationToken);
            return UserResponse.Desde(client, CanSeePrivateData(client));
        }

        public async Task<UserResponse?> UpdateLawyerPhoneAsync(Guid id, string? phone, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByIdAsync(id, cancellationToken);
            if (user == null) return null;

            EnsureCanEditContact(user);

            if (user is not Lawyer lawyer)
                throw new ArgumentException("El usuario indicado no es un abogado.");

            lawyer.UpdatePhone(phone);
            await usersRepository.SaveAsync(cancellationToken);
            return UserResponse.Desde(lawyer, CanSeePrivateData(lawyer));
        }

        public async Task<UserResponse?> UpdateClientAddressAsync(Guid id, string? address, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByIdAsync(id, cancellationToken);
            if (user == null) return null;

            EnsureCanEditContact(user);

            if (user is not Client client)
                throw new ArgumentException("El usuario indicado no es un cliente.");

            client.UpdateAddress(address);
            await usersRepository.SaveAsync(cancellationToken);
            return UserResponse.Desde(client, CanSeePrivateData(client));
        }

        public async Task<UserResponse?> UpdateBarNumberAsync(Guid id, string barNumber, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByIdAsync(id, cancellationToken);
            if (user == null) return null;

            if (user is not Lawyer lawyer)
                throw new ArgumentException("El usuario indicado no es un abogado.");

            lawyer.UpdateBarNumber(barNumber);
            await usersRepository.SaveAsync(cancellationToken);
            return UserResponse.Desde(lawyer, CanSeePrivateData(lawyer));
        }

        public async Task<UserResponse?> UpdateSpecialtiesAsync(Guid id, IEnumerable<string> specialties, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByIdAsync(id, cancellationToken);
            if (user == null) return null;

            if (user is not Lawyer lawyer)
                throw new ArgumentException("El usuario indicado no es un abogado.");

            lawyer.UpdateSpecialties(specialties);
            await usersRepository.SaveAsync(cancellationToken);
            return UserResponse.Desde(lawyer, CanSeePrivateData(lawyer));
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByIdAsync(id, cancellationToken);
            if (user == null) return false;

            if (user.Id == currentUser.Id)
                throw new ForbiddenException("Un usuario no puede darse de baja a sí mismo.");

            var allCases = await casesRepository.GetAllAsync(cancellationToken);
            var hasActiveCases = allCases
                .Any(c => (c.ClientId == id || c.LawyerIds.Contains(id)) && c.Status != CaseStatus.Cerrado);

            var allAppointments = await appointmentsRepository.GetAllAsync(cancellationToken);
            var hasActiveAppointments = allAppointments
                .Any(a => (a.ClientId == id || a.LawyerId == id)
                    && a.EffectiveStatus != AppointmentStatus.Cancelado && a.EffectiveStatus != AppointmentStatus.Finalizado);

            if (hasActiveCases || hasActiveAppointments)
                throw new InvalidOperationException($"El usuario con id {id} tiene expedientes o turnos activos y no puede ser desactivado.");

            user.Deactivate();
            await usersRepository.SaveAsync(cancellationToken);
            return true;
        }

        // One predicate for both private user fields (Dni and Email): either the admin
        // sees them or the user themselves does. No one else sees them on any endpoint.
        private bool CanSeePrivateData(User user) => currentUser.IsAdmin || user.Id == currentUser.Id;

        private async Task EnsureCanReadAsync(User user, CancellationToken cancellationToken = default)
        {
            if (currentUser.IsAdmin) return;

            if (user.Id == currentUser.Id) return;

            if (currentUser.IsLawyer && user is Client && await IsClientLinkedToLawyerAsync(user.Id, cancellationToken)) return;

            if (currentUser.IsClient && user is Lawyer) return;

            throw new ForbiddenException("No tiene acceso a este usuario.");
        }

        private void EnsureCanEditContact(User user)
        {
            if (currentUser.IsAdmin) return;

            if (user.Id == currentUser.Id) return;

            throw new ForbiddenException("Solo puede modificar sus propios datos de contacto.");
        }

        private async Task<bool> IsClientLinkedToLawyerAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            var allCases = await casesRepository.GetAllAsync(cancellationToken);
            if (allCases.Any(c => c.ClientId == clientId && c.LawyerIds.Contains(currentUser.Id)))
                return true;

            var allAppointments = await appointmentsRepository.GetAllAsync(cancellationToken);
            return allAppointments.Any(a => a.ClientId == clientId && a.LawyerId == currentUser.Id);
        }
    }
}