using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface IUserRepository
    {
        void Add(User user);

        IReadOnlyList<User> GetAll();

        User? GetById(Guid id);

        User? GetByEmail(string email);

        void Save();
    }
}