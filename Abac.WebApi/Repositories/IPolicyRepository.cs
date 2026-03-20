using Abac.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Abac.WebApi.Repositories
{
    public interface IPolicyRepository
    {
        Task<List<Policy>> GetPoliciesByResourceTypeAsync(string resourceType);
    }

    public class EfCorePolicyRepository : IPolicyRepository
    {
        private readonly AppDbContext _context;

        public EfCorePolicyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Policy>> GetPoliciesByResourceTypeAsync(string resourceType)
        {
            return await _context.Policies
                .Where(p => p.ResourceType == resourceType && p.IsEnabled)
                .ToListAsync();
        }
    }
}
