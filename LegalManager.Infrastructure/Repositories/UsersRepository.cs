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

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
            => await context.Users.Where(u => u.Active).ToListAsync(cancellationToken);

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Active, cancellationToken);

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = (email ?? string.Empty).Trim().ToLower();

            return await context.Users.FirstOrDefaultAsync(u => u.Active && u.Email.Trim().ToLower() == normalized, cancellationToken);
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Dos requests simultáneos pasaron el chequeo en memoria de EnsureUnique y el
                // índice único de Email/Dni (mismo filtro [Active] = 1) cerró la carrera en la
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