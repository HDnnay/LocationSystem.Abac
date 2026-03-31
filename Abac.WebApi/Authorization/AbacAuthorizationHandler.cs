using Abac.WebApi.Models;
using Abac.WebApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Security.Claims;

namespace Abac.WebApi.Authorization
{
    public class AbacAuthorizationHandler : AuthorizationHandler<AbacRequirement>
    {
        private readonly IPolicyRepository _policyRepo;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ConcurrentDictionary<string, Lazy<Task<List<Policy>>>> _policiesCache;

        public AbacAuthorizationHandler(
            IPolicyRepository policyRepo,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor)
        {
            _policyRepo = policyRepo;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _policiesCache = new ConcurrentDictionary<string, Lazy<Task<List<Policy>>>>();
        }

        /// <summary>
        /// 清理指定资源类型的策略缓存
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <returns>是否成功清理</returns>
        public bool RefreshPoliciesCache(string resourceType)
        {
            return _policiesCache.TryRemove(resourceType, out _);
        }

        /// <summary>
        /// 清理所有资源类型的策略缓存
        /// </summary>
        public void RefreshAllPoliciesCache()
        {
            _policiesCache.Clear();
        }

        /// <summary>
        /// 获取当前缓存的所有资源类型
        /// </summary>
        /// <returns>已缓存的资源类型列表</returns>
        public IReadOnlyCollection<string> GetCachedResourceTypes()
        {
            return _policiesCache.Keys.ToList().AsReadOnly();
        }

        /// <summary>
        /// 强制重新加载指定资源类型的策略
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <returns>重新加载后的策略列表</returns>
        public async Task<List<Policy>> ForceReloadPoliciesAsync(string resourceType)
        {
            RefreshPoliciesCache(resourceType);
            return await GetPoliciesByResourceTypeAsync(resourceType);
        }

        private async Task<List<Policy>> GetPoliciesByResourceTypeAsync(string resourceType)
        {
            // 双重检查：先尝试从缓存获取
            if (_policiesCache.TryGetValue(resourceType, out var existingLazy))
            {
                return await existingLazy.Value;
            }

            // 创建新的Lazy实例（使用ExecutionAndPublication确保线程安全）
            var newLazy = new Lazy<Task<List<Policy>>>(async () =>
            {
                try
                {
                    var policies = await _policyRepo.GetPoliciesByResourceTypeAsync(resourceType);
                    return policies.OrderBy(p => p.Priority).ToList();
                }
                catch (Exception)
                {
                    // 记录异常并返回空列表，避免阻塞后续请求
                    return new List<Policy>();
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);

            // 原子性地添加到缓存
            var lazy = _policiesCache.GetOrAdd(resourceType, newLazy);

            // 如果添加的是我们创建的实例，返回它的值
            // 如果添加的是其他线程创建的实例，返回那个实例的值
            return await lazy.Value;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AbacRequirement requirement)
        {
            // 快速通道：Admin 直接通过
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // 获取资源对象并识别资源类型
            string resourceType;
            object resourceObject;

            switch (context.Resource)
            {
                case Document document:
                    resourceType = nameof(Document);
                    resourceObject = document;
                    break;
                default:
                    // 没有匹配的资源类型时，仅靠角色决策
                    context.Succeed(requirement);
                    return;
            }

            // 构建 EvaluationContext
            var evalContext = BuildEvaluationContext(context.User, resourceObject);

            // 动态获取对应资源类型的规则
            var orderedPolicies = await GetPoliciesByResourceTypeAsync(resourceType);

            bool? finalDecision = null;
            foreach (var policy in orderedPolicies)
            {
                var compiledFunc = _cache.GetOrCreate(
                    GetCacheKey(policy),
                    entry =>
                    {
                        entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                        entry.SetSlidingExpiration(TimeSpan.FromMinutes(1));
                        return CompileExpression(policy.RuleExpression);
                    });

                var matched = compiledFunc(evalContext);
                if (matched)
                {
                    if (policy.Effect == "Deny")
                    {
                        var failureReason = new AuthorizationFailureReason(this, $"策略评估失败: {policy.RuleExpression}");
                        context.Fail(failureReason);
                        return;
                    }
                    else if (policy.Effect == "Allow")
                    {
                        finalDecision = true;
                    }
                }
            }

            if (finalDecision == true)
                context.Succeed(requirement);
            else
            {
                var failureReason = new AuthorizationFailureReason(this, "所有策略评估均未通过授权");
                context.Fail(failureReason);
            }
        }

        private EvaluationContext BuildEvaluationContext(ClaimsPrincipal user, object resource)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid userId = Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : Guid.Empty;

            var resourceAttributes = resource switch
            {
                Document document => new ResourceAttributes
                {
                    Type = nameof(Document),
                    OwnerId = document.OwnerId,
                    Department = document.Department,
                    Status = document.Status,
                    Confidentiality = document.Confidentiality
                },
                _ => new ResourceAttributes { Type = resource.GetType().Name }
            };

            return new EvaluationContext
            {
                User = new UserAttributes
                {
                    Id = userId,
                    Department = user.FindFirstValue("department") ?? "",
                    Level = int.Parse(user.FindFirstValue("level") ?? "0"),
                    Roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList()
                },
                Resource = resourceAttributes,
                Environment = new EnvironmentAttributes
                {
                    CurrentTime = DateTime.Now,
                    ClientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? ""
                }
            };
        }

        private Func<EvaluationContext, bool> CompileExpression(string expression)
        {
            // 使用 DynamicExpressionParser.ParseLambda 替代 ExpressionParser 构造函数
            var lambda = DynamicExpressionParser.ParseLambda<EvaluationContext, bool>(
                new ParsingConfig(), false, expression);
            return lambda.Compile();
        }

        private string GetCacheKey(Policy policy)
        {
            return $"ABAC_Rule_{policy.Id}_{policy.RuleExpression.GetHashCode()}";
        }
    }
}
