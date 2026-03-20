using Abac.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abac.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // 只有管理员可以操作缓存
    public class CacheManagementController : ControllerBase
    {
        private readonly AbacAuthorizationHandler _abacAuthorizationHandler;

        public CacheManagementController(AbacAuthorizationHandler abacAuthorizationHandler)
        {
            _abacAuthorizationHandler = abacAuthorizationHandler;
        }

        /// <summary>
        /// 获取当前缓存的所有资源类型
        /// </summary>
        [HttpGet("resource-types")]
        public IActionResult GetCachedResourceTypes()
        {
            var resourceTypes = _abacAuthorizationHandler.GetCachedResourceTypes();
            return Ok(new { ResourceTypes = resourceTypes });
        }

        /// <summary>
        /// 清理指定资源类型的策略缓存
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        [HttpDelete("{resourceType}")]
        public IActionResult RefreshCache(string resourceType)
        {
            var result = _abacAuthorizationHandler.RefreshPoliciesCache(resourceType);
            return result ? Ok(new { Message = $"已清理 {resourceType} 的策略缓存" })
                         : NotFound(new { Message = $"未找到 {resourceType} 的缓存记录" });
        }

        /// <summary>
        /// 清理所有资源类型的策略缓存
        /// </summary>
        [HttpDelete("all")]
        public IActionResult RefreshAllCache()
        {
            _abacAuthorizationHandler.RefreshAllPoliciesCache();
            return Ok(new { Message = "已清理所有策略缓存" });
        }

        /// <summary>
        /// 强制重新加载指定资源类型的策略
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        [HttpPost("{resourceType}/reload")]
        public async Task<IActionResult> ForceReload(string resourceType)
        {
            try
            {
                var policies = await _abacAuthorizationHandler.ForceReloadPoliciesAsync(resourceType);
                return Ok(new
                {
                    Message = $"已重新加载 {resourceType} 的策略",
                    PolicyCount = policies.Count,
                    Policies = policies.Select(p => new { p.Id, p.ResourceType, p.Priority })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"重新加载失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 批量清理多个资源类型的缓存
        /// </summary>
        /// <param name="request">包含资源类型列表的请求</param>
        [HttpPost("batch-refresh")]
        public IActionResult BatchRefreshCache([FromBody] BatchRefreshRequest request)
        {
            if (request?.ResourceTypes == null || !request.ResourceTypes.Any())
            {
                return BadRequest(new { Message = "请提供要清理的资源类型列表" });
            }

            var results = new List<BatchRefreshResult>();
            foreach (var resourceType in request.ResourceTypes)
            {
                var success = _abacAuthorizationHandler.RefreshPoliciesCache(resourceType);
                results.Add(new BatchRefreshResult
                {
                    ResourceType = resourceType,
                    Success = success,
                    Message = success ? "清理成功" : "缓存不存在"
                });
            }

            return Ok(new { Results = results });
        }
    }

    public class BatchRefreshRequest
    {
        public List<string> ResourceTypes { get; set; } = new();
    }

    public class BatchRefreshResult
    {
        public string ResourceType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}