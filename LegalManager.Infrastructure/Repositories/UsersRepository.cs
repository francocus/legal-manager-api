using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class UsersRepository(LegalManagerDbContext context) : IUserRepository
    {
        public void Add(User user) => context.Users.Add(user);

        public IReadOnlyList<User> GetAll()
            => [.. context.Users.Where(u => u.Active)];

        public User? GetById(Guid id)
            => context.Users.FirstOrDefault(u => u.Id == id && u.Active);

        public User? GetByEmail(string email)
        {
            var normalized = (email ?? string.Empty).Trim().ToLower();

            return context.Users.FirstOrDefault(u => u.Active && u.Email.Trim().ToLower() == normalized);
        }

        public void Save()
        {
            try
            {
                context.SaveChanges();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Dos requests simultaneos pasaron el chequeo en memoria de EnsureUnique y el
                // indice unico de Email/Dni (mismo filtro [Active] = 1) cerro la carrera en la
                // base. Se traduce al mismo InvalidOperationException que EnsureUnique, para que
                // el controller responda 409 y no un 500 crudo.
                throw new InvalidOperationException(UniqueViolationMessage(ex));
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
            => ex.InnerException is SqlException { Number: 2601 or 2627 };

        private static string UniqueViolationMessage(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? string.Empty;
            return message.Contains("Email", StringComparison.OrdinalIgnoreCase)
                ? "Ya existe un usuario con ese email."
                : "Ya existe un usuario con ese DNI.";
        }
    }
}