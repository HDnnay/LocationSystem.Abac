using Abac.WebApi.Models;
using Abac.WebApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Dynamic.Core;
using System.Security.Claims;

namespace Abac.WebApi.Authorization
{
    public class AbacAuthorizationHandler : AuthorizationHandler<AbacRequirement>
    {
        private readonly IPolicyRepository _policyRepo;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AbacAuthorizationHandler(
            IPolicyRepository policyRepo,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor)
        {
            _policyRepo = policyRepo;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AbacRequirement requirement)
        {
            // 快速通道：Admin 直接通过
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // 获取资源对象
            if (context.Resource is not Document document)
            {
                // 没有资源对象时，仅靠角色决策（由其他策略处理）
                context.Succeed(requirement);
                return;
            }

            // 构建 EvaluationContext
            var evalContext = BuildEvaluationContext(context.User, document);

            // 加载规则
            var policies = await _policyRepo.GetPoliciesByResourceTypeAsync(nameof(Document));
            var orderedPolicies = policies.OrderBy(p => p.Priority);

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
                        context.Fail();
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
                context.Fail();
        }

        private EvaluationContext BuildEvaluationContext(ClaimsPrincipal user, Document document)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid userId = Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : Guid.Empty;
            
            return new EvaluationContext
            {
                User = new UserAttributes
                {
                    Id = userId,
                    Department = user.FindFirstValue("department") ?? "",
                    Level = int.Parse(user.FindFirstValue("level") ?? "0"),
                    Roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList()
                },
                Resource = new ResourceAttributes
                {
                    Type = nameof(Document),
                    OwnerId = document.OwnerId,
                    Department = document.Department,
                    Status = document.Status,
                    Confidentiality = document.Confidentiality
                },
                Environment = new EnvironmentAttributes
                {
                    CurrentTime = DateTime.UtcNow,
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
