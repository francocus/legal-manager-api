using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalManager.Infrastructure.Repositories
{
    public class CasesRepository : ICaseRepository
    {
        private readonly LegalManagerDbContext context;

        public CasesRepository(LegalManagerDbContext context)
        {
            this.context = context;
        }

        public void Add(Case caseItem) => context.Cases.Add(caseItem);

        public IReadOnlyList<Case> GetAll()
            => context.Cases.Where(c => c.Active).ToList();

        public Case? GetById(Guid id)
            => context.Cases.FirstOrDefault(c => c.Id == id && c.Active);

        public void Save() => context.SaveChanges();
    }
}