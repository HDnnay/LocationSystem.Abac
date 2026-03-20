using Abac.WebApi;
using Abac.WebApi.Authorization;
using Abac.WebApi.Repositories;
using Casbin.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCasbin(option =>
{
    option.DefaultModelPath = "";
    option.DefaultPolicyPath = "";
});
// 添加 EF Core InMemory 数据库（便于演示）
var conn = builder.Configuration.GetConnectionString("SqlServerConnectionString");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(conn));
// 注册仓储和服务
builder.Services.AddScoped<IPolicyRepository, EfCorePolicyRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// 注册授权处理器
builder.Services.AddScoped<IAuthorizationHandler, AbacAuthorizationHandler>();

// 注册内存缓存（用于缓存编译后的表达式）
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// 配置 JWT 认证（演示用）
var key = Encoding.ASCII.GetBytes("your-secret-key-at-least-16-chars-long");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// 注册授权策略
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DocumentAccessPolicy", policy =>
        policy.Requirements.Add(new AbacRequirement()));
});
var app = builder.Build();
// 初始化测试数据
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(dbContext);
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseAuthentication();
app.UseCasbinAuthorization();
app.UseAuthorization();

app.MapControllers();

app.Run();
