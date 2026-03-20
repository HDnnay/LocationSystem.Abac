using Abac.WebApi.Repositories;
using Casbin.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abac.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IAuthorizationService _authService;
        private readonly IDocumentRepository _docRepo;

        public DocumentsController(IAuthorizationService authService, IDocumentRepository docRepo)
        {
            _authService = authService;
            _docRepo = docRepo;
        }

        [HttpGet("{id}")]
        [CasbinAuthorize("GetDocument", "Get")]
        public async Task<IActionResult> GetDocument(Guid id)
        {
            var document = await _docRepo.GetByIdAsync(id);
            if (document == null)
                return NotFound();

            var authResult = await _authService.AuthorizeAsync(User, document, Policies.DocumentAccess);

            if (!authResult.Succeeded)
                return Forbid();

            return Ok(document);
        }
    }
}
