using LegalManager.Application.DTOs;
using LegalManager.Application.Interfaces;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;

namespace LegalManager.Application.Services
{
    public class UserService(
        IUserRepository usersRepository,
        ICaseRepository casesRepository,
        IAppointmentRepository appointmentsRepository) : IUserService
    {
        private void EnsureUnique(string email, string dni, Guid? excludeId = null)
        {
            var users = usersRepository.GetAll();
            var normalizedEmail = email?.Trim();

            if (users.Any(u => u.Id != excludeId && string.Equals(u.Email.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Ya existe un usuario con ese email.");

            if (users.Any(u => u.Id != excludeId && u.Dni == dni))
                throw new InvalidOperationException("Ya existe un usuario con ese DNI.");
        }

        public Client CreateClient(CreateClientRequest request)
        {
            EnsureUnique(request.Email, request.Dni);

            var client = new Client(request.FirstName, request.LastName, request.Dni, request.Email, request.Password, request.Phone, request.Address);
            usersRepository.Add(client);
            usersRepository.Save();
            return client;
        }

        public Lawyer CreateLawyer(CreateLawyerRequest request)
        {
            EnsureUnique(request.Email, request.Dni);

            var lawyer = new Lawyer(request.FirstName, request.LastName, request.Dni, request.Email, request.Password, request.BarNumber, request.Phone, request.Specialties);
            usersRepository.Add(lawyer);
            usersRepository.Save();
            return lawyer;
        }

        public Admin CreateAdmin(CreateAdminRequest request)
        {
            EnsureUnique(request.Email, request.Dni);

            var admin = new Admin(request.FirstName, request.LastName, request.Dni, request.Email, request.Password);
            usersRepository.Add(admin);
            usersRepository.Save();
            return admin;
        }

        public IReadOnlyList<User> GetAll() => usersRepository.GetAll();

        public IReadOnlyList<User> GetClients() => usersRepository.GetAll().OfType<Client>().ToList().AsReadOnly();

        public IReadOnlyList<User> GetLawyers() => usersRepository.GetAll().OfType<Lawyer>().ToList().AsReadOnly();

        public IReadOnlyList<User> GetAdmins() => usersRepository.GetAll().OfType<Admin>().ToList().AsReadOnly();

        public User? GetById(Guid id) => usersRepository.GetById(id);

        public User? Update(Guid id, UpdateUserRequest request)
        {
            var user = GetById(id);
            if (user == null) return null;

            EnsureUnique(request.Email, request.Dni, id);

            user.UpdateDetails(request.FirstName, request.LastName, request.Dni, request.Email);
            usersRepository.Save();
            return user;
        }

        public User? UpdateClientPhone(Guid id, string? phone)
        {
            var user = GetById(id);
            if (user == null) return null;

            if (user is not Client client)
                throw new ArgumentException("El usuario indicado no es un cliente.");

            client.UpdatePhone(phone);
            usersRepository.Save();
            return client;
        }

        public User? UpdateLawyerPhone(Guid id, string? phone)
        {
            var user = GetById(id);
            if (user == null) return null;

            if (user is not Lawyer lawyer)
                throw new ArgumentException("El usuario indicado no es un abogado.");

            lawyer.UpdatePhone(phone);
            usersRepository.Save();
            return lawyer;
        }

        public User? UpdateClientAddress(Guid id, string? address)
        {
            var user = GetById(id);
            if (user == null) return null;

            if (user is not Client client)
                throw new ArgumentException("El usuario indicado no es un cliente.");

            client.UpdateAddress(address);
            usersRepository.Save();
            return client;
        }

        public User? UpdateBarNumber(Guid id, string barNumber)
        {
            var user = GetById(id);
            if (user == null) return null;

            if (user is not Lawyer lawyer)
                throw new ArgumentException("El usuario indicado no es un abogado.");

            lawyer.UpdateBarNumber(barNumber);
            usersRepository.Save();
            return lawyer;
        }

        public User? UpdateSpecialties(Guid id, IEnumerable<string> specialties)
        {
            var user = GetById(id);
            if (user == null) return null;

            if (user is not Lawyer lawyer)
                throw new ArgumentException("El usuario indicado no es un abogado.");

            lawyer.UpdateSpecialties(specialties);
            usersRepository.Save();
            return lawyer;
        }

        public bool Delete(Guid id)
        {
            var user = GetById(id);
            if (user == null) return false;

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
    }
}