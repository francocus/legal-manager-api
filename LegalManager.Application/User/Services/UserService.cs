using System.Text.Json;
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
        ICurrentUser currentUser,
        AccountConfirmationService confirmationService) : IUserService
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

            // New accounts start unconfirmed: set the token before the save and email the
            // link right after it succeeds, keeping the single save and the 201 semantics.
            var rawToken = await confirmationService.StartForNewUserAsync(client, cancellationToken);

            usersRepository.Add(client);
            await usersRepository.SaveAsync(cancellationToken);

            await confirmationService.NotifyConfirmationAsync(client, rawToken, cancellationToken);

            return UserResponse.Desde(client, CanSeePrivateData(client));
        }

        public async Task<UserResponse> CreateLawyerAsync(CreateLawyerRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureUniqueAsync(request.Email, request.Dni, null, cancellationToken);

            var lawyer = new Lawyer(request.FirstName, request.LastName, request.Dni, request.Email, HashPassword(request.Password), request.BarNumber, request.Phone, request.Specialties);

            // New accounts start unconfirmed: set the token before the save and email the
            // link right after it succeeds, keeping the single save and the 201 semantics.
            var rawToken = await confirmationService.StartForNewUserAsync(lawyer, cancellationToken);

            usersRepository.Add(lawyer);
            await usersRepository.SaveAsync(cancellationToken);

            await confirmationService.NotifyConfirmationAsync(lawyer, rawToken, cancellationToken);

            return UserResponse.Desde(lawyer, CanSeePrivateData(lawyer));
        }

        public async Task<UserResponse> CreateAdminAsync(CreateAdminRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureUniqueAsync(request.Email, request.Dni, null, cancellationToken);

            var admin = new Admin(request.FirstName, request.LastName, request.Dni, request.Email, HashPassword(request.Password));

            // New accounts start unconfirmed: set the token before the save and email the
            // link right after it succeeds, keeping the single save and the 201 semantics.
            var rawToken = await confirmationService.StartForNewUserAsync(admin, cancellationToken);

            usersRepository.Add(admin);
            await usersRepository.SaveAsync(cancellationToken);

            await confirmationService.NotifyConfirmationAsync(admin, rawToken, cancellationToken);

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

        public async Task<PagedResponse<UserResponse>> GetPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default)
        {
            var (currentPage, currentPageSize) = PagedResponse.Normalize(page, pageSize);
            var (users, total) = await usersRepository.GetPagedAsync(currentPage, currentPageSize, cancellationToken);
            return ToPagedResponse(users, currentPage, currentPageSize, total);
        }

        public async Task<PagedResponse<UserResponse>> GetClientsPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default)
        {
            var (currentPage, currentPageSize) = PagedResponse.Normalize(page, pageSize);
            var (users, total) = await usersRepository.GetClientsPagedAsync(currentPage, currentPageSize, cancellationToken);
            return ToPagedResponse(users, currentPage, currentPageSize, total);
        }

        public async Task<PagedResponse<UserResponse>> GetLawyersPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default)
        {
            var (currentPage, currentPageSize) = PagedResponse.Normalize(page, pageSize);
            var (users, total) = await usersRepository.GetLawyersPagedAsync(currentPage, currentPageSize, cancellationToken);
            return ToPagedResponse(users, currentPage, currentPageSize, total);
        }

        public async Task<PagedResponse<UserResponse>> GetAdminsPagedAsync(int? page, int? pageSize, CancellationToken cancellationToken = default)
        {
            var (currentPage, currentPageSize) = PagedResponse.Normalize(page, pageSize);
            var (users, total) = await usersRepository.GetAdminsPagedAsync(currentPage, currentPageSize, cancellationToken);
            return ToPagedResponse(users, currentPage, currentPageSize, total);
        }

        // Same mapping (and same private-data rule) as the unpaged listings, so paged
        // items are byte-identical to their plain-array counterparts.
        private PagedResponse<UserResponse> ToPagedResponse(IReadOnlyList<User> users, int page, int pageSize, int total)
            => new(users.Select(u => UserResponse.Desde(u, CanSeePrivateData(u))).ToList(), page, pageSize, total);

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

        // Admin rescue hatch: confirms an account by id when the emailed link was lost
        // (or for automated QA). Idempotent: an already-confirmed account returns unchanged.
        public async Task<UserResponse?> ConfirmEmailByAdminAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByIdAsync(id, cancellationToken);
            if (user == null) return null;

            if (!user.EmailConfirmed)
            {
                user.ConfirmEmail();
                await usersRepository.SaveAsync(cancellationToken);
            }

            return UserResponse.Desde(user, CanSeePrivateData(user));
        }

        // Admin flow: lawyers self-register through the public client signup and an admin
        // fixes the role afterwards. The user keeps the same Id; only the TPC row changes.
        // Admins are out of this flow: they are command-seeded and never change role.
        public async Task<UserResponse?> ChangeRoleAsync(Guid id, RoleChangeRequest request, CancellationToken cancellationToken = default)
        {
            var user = await usersRepository.GetByIdAsync(id, cancellationToken);
            if (user == null) return null;

            if (request.Role is not ("abogado" or "cliente"))
                throw new ArgumentException("El rol indicado no es válido.");

            if (user is Admin)
                throw new InvalidOperationException("Los administradores se crean por comando y no cambian de rol.");

            var targetIsLawyer = request.Role == "abogado";
            var userIsLawyer = user is Lawyer;

            // Idempotent: asking for the role the user already has returns it unchanged.
            if (userIsLawyer == targetIsLawyer)
                return UserResponse.Desde(user, CanSeePrivateData(user));

            if (targetIsLawyer)
            {
                if (string.IsNullOrWhiteSpace(request.BarNumber))
                    throw new ArgumentException("La matrícula es obligatoria para promover a abogado.");

                // Promote: any case or appointment as client in ANY state blocks the move,
                // the Restrict FKs would reject it in the database anyway.
                var hasCasesAsClient = await casesRepository.HasAnyCaseAsClientAsync(id, cancellationToken);
                var hasAppointmentsAsClient = await appointmentsRepository.HasAnyAppointmentAsClientAsync(id, cancellationToken);
                if (hasCasesAsClient || hasAppointmentsAsClient)
                    throw new InvalidOperationException("El usuario tiene expedientes o turnos como cliente y no puede cambiar de rol.");

                var promoted = await usersRepository.ChangeRoleAsync(
                    id, request.Role, request.BarNumber, JsonSerializer.Serialize(request.Specialties ?? []), cancellationToken);

                return promoted == null ? null : UserResponse.Desde(promoted, CanSeePrivateData(promoted));
            }

            // Demote: any case or appointment as lawyer in ANY state blocks the move.
            var hasCasesAsLawyer = await casesRepository.HasAnyCaseAsLawyerAsync(id, cancellationToken);
            var hasAppointmentsAsLawyer = await appointmentsRepository.HasAnyAppointmentAsLawyerAsync(id, cancellationToken);
            if (hasCasesAsLawyer || hasAppointmentsAsLawyer)
                throw new InvalidOperationException("El usuario tiene expedientes o turnos como abogado y no puede cambiar de rol.");

            var demoted = await usersRepository.ChangeRoleAsync(id, request.Role, null, null, cancellationToken);
            return demoted == null ? null : UserResponse.Desde(demoted, CanSeePrivateData(demoted));
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