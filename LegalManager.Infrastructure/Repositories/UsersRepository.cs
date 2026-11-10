using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class UsersRepository(LegalManagerDbContext context) : GenericRepository<User>(context), IUserRepository
    {
        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = (email ?? string.Empty).Trim().ToLower();

            return await _dbSet.FirstOrDefaultAsync(u => u.Active && u.Email.Trim().ToLower() == normalized, cancellationToken);
        }

        // TPC maps these lookups to one query per concrete table (Clients/Lawyers/Admins);
        // every one of them holds all base User columns. Active filter matches the repo rule.
        public async Task<User?> GetByEmailConfirmationTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Active && u.EmailConfirmationToken == tokenHash, cancellationToken);
        }

        public async Task<User?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Active && u.PasswordResetToken == tokenHash, cancellationToken);
        }

        public override async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await base.SaveAsync(cancellationToken);
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
