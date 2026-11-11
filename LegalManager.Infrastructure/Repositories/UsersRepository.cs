using System.Text.Json;
using LegalManager.Domain.Entities;
using LegalManager.Domain.Enums;
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

        public async Task<User?> GetByEmailConfirmationTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Active && u.EmailConfirmationToken == tokenHash, cancellationToken);
        }

        public async Task<User?> GetByPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Active && u.PasswordResetToken == tokenHash, cancellationToken);
        }

        public async Task<User?> ChangeRoleAsync(Guid id, string targetRole, string? barNumber, string? specialtiesJson, CancellationToken cancellationToken = default)
        {
            var user = await _dbSet.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user == null) return null;

            if (user is Admin)
                throw new InvalidOperationException("Los administradores se crean por comando y no cambian de rol.");

            if (user is Client sourceClient)
            {
                if (targetRole != "abogado")
                    throw new ArgumentException("El rol indicado no es válido.");

                var specialties = specialtiesJson == null
                    ? []
                    : JsonSerializer.Deserialize<List<string>>(specialtiesJson) ?? [];

                var targetLawyer = Lawyer.FromClient(sourceClient, barNumber ?? string.Empty, sourceClient.Phone, specialties);

                _dbSet.Remove(sourceClient);
                _dbSet.Add(targetLawyer);
                await SaveAsync(cancellationToken);
                return targetLawyer;
            }

            if (user is Lawyer sourceLawyer)
            {
                if (targetRole != "cliente")
                    throw new ArgumentException("El rol indicado no es válido.");

                var targetClient = Client.FromLawyer(sourceLawyer);

                _dbSet.Remove(sourceLawyer);
                _dbSet.Add(targetClient);
                await SaveAsync(cancellationToken);
                return targetClient;
            }

            throw new ArgumentException("El rol indicado no es válido.");
        }

        public override async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await base.SaveAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
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

        public async Task<(IReadOnlyList<User> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => await GetPagedByTypeAsync(_dbSet.Where(u => u.Active), page, pageSize, cancellationToken);

        public async Task<(IReadOnlyList<User> Items, int Total)> GetClientsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => await GetPagedByTypeAsync(_dbSet.OfType<Client>().Where(u => u.Active), page, pageSize, cancellationToken);

        public async Task<(IReadOnlyList<User> Items, int Total)> GetLawyersPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => await GetPagedByTypeAsync(_dbSet.OfType<Lawyer>().Where(u => u.Active), page, pageSize, cancellationToken);

        public async Task<(IReadOnlyList<User> Items, int Total)> GetAdminsPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => await GetPagedByTypeAsync(_dbSet.OfType<Admin>().Where(u => u.Active), page, pageSize, cancellationToken);

        private static async Task<(IReadOnlyList<User> Items, int Total)> GetPagedByTypeAsync(IQueryable<User> query, int page, int pageSize, CancellationToken cancellationToken)
        {
            var ordered = query.OrderBy(u => u.Id);

            var total = await ordered.CountAsync(cancellationToken);
            var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

            return (items, total);
        }
    }
}