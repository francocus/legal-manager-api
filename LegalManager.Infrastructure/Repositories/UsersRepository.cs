using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;

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

        public void Save() => context.SaveChanges();
    }
}