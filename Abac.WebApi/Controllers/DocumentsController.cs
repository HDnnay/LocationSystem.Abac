﻿using Abac.WebApi.Repositories;
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
        [CasbinAuthorize("getdocument", "GET")]
        public async Task<IActionResult> Get(Guid id)
        {
            var model = await _docRepo.GetByIdAsync(id);
            return Ok(model);
        }

    }
}
