# LocationSystem.Abac - 基于属性的访问控制(ABAC)系统

## 项目概述

这是一个基于ASP.NET Core 10.0开发的ABAC(Attribute-Based Access Control)系统，结合了JWT认证、Casbin授权和自定义ABAC授权处理器。系统实现了细粒度的访问控制策略，支持基于用户属性、资源属性和环境属性的动态权限决策。

## 技术栈

- **后端框架**: ASP.NET Core 10.0
- **数据库**: Entity Framework Core + SQL Server
- **认证**: JWT Bearer Token
- **授权**: Casbin + 自定义ABAC Handler
- **动态表达式**: System.Linq.Dynamic.Core

## 项目结构

```
Abac.WebApi/
├── Authorization/           # 授权相关
│   ├── AbacAuthorizationHandler.cs    # 自定义ABAC授权处理器
│   └── AbacRequirement.cs             # 授权要求定义
├── CasbinConf/             # Casbin配置
│   ├── basic_model.conf               # Casbin模型定义
│   └── basic_policy.csv               # 策略规则
├── Controllers/            # API控制器
│   ├── AuthController.cs              # 认证控制器
│   ├── DocumentsController.cs         # 文档管理控制器
│   └── WeatherForecastController.cs   # 示例控制器
├── Models/                 # 数据模型
│   ├── Document.cs                    # 文档模型
│   ├── EvaluationContext.cs           # 评估上下文
│   ├── Policy.cs                      # 策略模型
│   └── User.cs                        # 用户模型
├── Repositories/           # 数据访问层
│   ├── IDocumentRepository.cs         # 文档仓储接口
│   └── IPolicyRepository.cs           # 策略仓储接口
└── Program.cs             # 应用启动配置
```

## 认证授权流程详解

### 1. JWT认证流程

#### 登录认证 (`AuthController.Login`)

1. **用户验证**: 检查用户名和密码
2. **生成Claims**: 基于用户属性创建声明
   ```csharp
   var claims = new List<Claim>
   {
       new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
       new Claim(ClaimTypes.Name, user.UserName),
       new Claim("department", user.Department),
       new Claim("level", user.Level.ToString())
   };
   ```
3. **生成JWT Token**: 使用配置的密钥和过期时间

#### JWT配置 (`Program.cs`)

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = builder.Environment.IsProduction();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = builder.Environment.IsProduction(),
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = builder.Environment.IsProduction(),
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
```

### 2. Casbin授权流程

#### Casbin配置 (`Program.cs`)

```csharp
builder.Services.AddCasbinAuthorization(options =>
{
    options.PreferSubClaimType = ClaimTypes.Name;  // 使用用户名作为主体
    options.DefaultModelPath = Path.Combine("CasbinConf", "basic_model.conf");
    options.DefaultPolicyPath = Path.Combine("CasbinConf", "basic_policy.csv");
    options.DefaultRequestTransformerType = typeof(BasicRequestTransformer);
});
```

#### 策略模型 (`basic_model.conf`)

```ini
[request_definition]
r = sub, obj, act  # 主体, 资源, 操作

[policy_definition]
p = sub, obj, act  # 策略格式

[policy_effect]
e = some(where (p.eft == allow))  # 只要有一个允许策略就通过

[matchers]
m = r.sub == p.sub && regexMatch(r.obj, "(?i)" + p.obj) && regexMatch(r.act, "(?i)" + p.act)
```

#### 策略规则 (`basic_policy.csv`)

```csv
# Policy for GetDocument access
p, wang, GetDocument, GET
```

#### 控制器授权 (`DocumentsController.cs`)

```csharp
[HttpGet("{id}")]
[CasbinAuthorize("GetDocument", "Get")]
public async Task<IActionResult> GetDocument(Guid id)
{
    // Casbin验证通过后执行业务逻辑
}
```

### 3. 自定义ABAC授权处理器流程

#### 授权处理器 (`AbacAuthorizationHandler`)

**核心流程**:

1. **快速通道检查**: 如果是Admin角色直接通过
2. **资源验证**: 检查是否有Document资源对象
3. **构建评估上下文**: 创建包含用户、资源、环境属性的上下文
4. **加载策略**: 从数据库获取相关策略并按优先级排序
5. **动态表达式评估**: 使用System.Linq.Dynamic.Core编译和执行策略表达式
6. **决策执行**: 根据策略效果(Allow/Deny)决定授权结果

#### 评估上下文构建

```csharp
private EvaluationContext BuildEvaluationContext(ClaimsPrincipal user, Document document)
{
    return new EvaluationContext
    {
        User = new UserAttributes
        {
            Id = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""),
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
```

#### 动态表达式编译和执行

```csharp
private Func<EvaluationContext, bool> CompileExpression(string expression)
{
    var lambda = DynamicExpressionParser.ParseLambda<EvaluationContext, bool>(
        new ParsingConfig(), false, expression);
    return lambda.Compile();
}
```

#### 示例策略表达式

```csharp
// Admin用户拥有所有权限
"User.Roles.Contains(\"Admin\")"

// 高机密文档在工作时间外禁止访问
"Resource.Confidentiality == \"High\" && (Environment.CurrentTime.Hour < 9 || Environment.CurrentTime.Hour > 18)"

// 经理可以访问同部门文档
"User.Roles.Contains(\"Manager\") && Resource.Department == User.Department"

// 文档所有者可以访问自己的文档
"Resource.OwnerId == User.Id"
```

## 完整的请求处理流程

1. **请求到达**: 客户端发送带有JWT Token的请求
2. **JWT认证**: ASP.NET Core验证Token有效性
3. **Casbin授权**: 检查基于角色的简单权限
4. **ABAC授权**: 执行细粒度的属性基授权检查
5. **业务逻辑**: 授权通过后执行控制器方法
6. **响应返回**: 返回处理结果给客户端

## 配置说明

### 开发环境配置 (`appsettings.json`)

```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-at-least-16-chars-long",
    "Issuer": "LocationSystem.Abac",
    "Audience": "LocationSystem.Abac.Clients",
    "ExpirationMinutes": 120
  }
}
```

### 生产环境配置 (`appsettings.Production.json`)

```json
{
  "Jwt": {
    "SecretKey": "PRODUCTION_SECURE_KEY_AT_LEAST_32_CHARS_LONG",
    "Issuer": "LocationSystem.Abac",
    "Audience": "LocationSystem.Abac.Clients",
    "ExpirationMinutes": 60
  },
  "Security": {
    "RequireHttps": true
  }
}
```

## 运行和测试

### 启动应用

```bash
cd Abac.WebApi
dotnet run
```

### 测试用户

系统预置了以下测试用户：
- **admin** (密码: admin) - IT部门，Admin角色
- **zhang** (密码: zhang) - 销售部，Manager角色
- **li** (密码: li) - 销售部，Employee角色
- **wang** (密码: wang) - 财务部，Employee角色

### API测试

1. **登录获取Token**:
   ```http
   POST /api/auth/login
   {
     "username": "wang",
     "password": "wang"
   }
   ```

2. **访问受保护资源**:
   ```http
   GET /api/documents/{id}
   Authorization: Bearer {token}
   ```

## 扩展和定制

### 添加新的属性类型

1. 在 `EvaluationContext` 中添加新属性
2. 在 `BuildEvaluationContext` 方法中设置属性值
3. 创建相应的策略表达式

### 自定义策略逻辑

可以通过修改 `AbacAuthorizationHandler` 中的决策逻辑来实现更复杂的授权策略。

### 性能优化

- 策略表达式使用内存缓存
- 数据库查询优化
- 策略预编译和缓存

## 故障排除

### 常见问题

1. **JWT Token无效**: 检查密钥配置和Token过期时间
2. **授权失败**: 检查策略配置和用户属性匹配
3. **表达式解析错误**: 确保策略表达式语法正确

### 调试技巧

- 启用详细日志记录
- 检查评估上下文中的属性值
- 验证策略表达式执行结果

## 总结

本项目展示了如何结合JWT认证、Casbin授权和自定义ABAC处理器来实现一个完整的权限管理系统。通过属性基的访问控制，可以实现非常灵活和细粒度的权限管理，满足复杂业务场景的需求。