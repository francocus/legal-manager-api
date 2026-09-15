using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;

namespace LegalManager.Infrastructure.Repositories
{
    public class CasesRepository(LegalManagerDbContext context) : ICaseRepository
    {
        public void Add(Case caseItem) => context.Cases.Add(caseItem);

        public IReadOnlyList<Case> GetAll()
            => [.. context.Cases.Where(c => c.Active)];

        public Case? GetById(Guid id)
            => context.Cases.FirstOrDefault(c => c.Id == id && c.Active);

        public void Save() => context.SaveChanges();
    }
}