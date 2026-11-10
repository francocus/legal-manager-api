using LegalManager.Domain.Entities;
using LegalManager.Domain.Interfaces;
using LegalManager.Infrastructure.Persistence;

namespace LegalManager.Infrastructure.Repositories
{
    public class CasesRepository(LegalManagerDbContext context) : GenericRepository<Case>(context), ICaseRepository
    {
    }
}
