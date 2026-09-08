using LegalManager.Domain.Entities;

namespace LegalManager.Domain.Interfaces
{
    public interface ICaseRepository
    {
        void Add(Case caseItem);

        IReadOnlyList<Case> GetAll();

        Case? GetById(Guid id);

        void Save();
    }
}