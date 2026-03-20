
using Abac.WebApi.Models;

namespace Abac.WebApi.Repositories
{
    public interface IDocumentRepository
    {
        Task<Document?> GetByIdAsync(Guid id);
    }

    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;

        public DocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Document?> GetByIdAsync(Guid id)
        {
            return await _context.Documents.FindAsync(id);
        }
    }
}
