using Abac.WebApi;
using Abac.WebApi.Authorization;
using Abac.WebApi.Middleware;
using Abac.WebApi.Repositories;
using Casbin;
using Casbin.AspNetCore.Authorization;
using Casbin.AspNetCore.Authorization.Transformers;
using Casbin.Persist.Adapter.EFCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCasbin(option =>
{
    option.DefaultModelPath = "";
    option.DefaultPolicyPath = "";
});
var conn = builder.Configuration.GetConnectionString("SqlServerConnectionString");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(conn));
builder.Services.AddDbContext<LocalCasbinDbContext>(options =>
{
    options.UseSqlServer(conn, m => m.MigrationsAssembly(typeof(Program).Assembly));
});
builder.Services.AddScoped<IPolicyRepository, EfCorePolicyRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IAuthorizationHandler, AbacAuthorizationHandler>();

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? "your-secret-key-at-least-16-chars-long";
var key = Encoding.ASCII.GetBytes(secretKey);

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DocumentAccessPolicy", policy =>
        policy.Requirements.Add(new AbacRequirement()));

    // 配置授权失败时返回详细信息
    options.InvokeHandlersAfterFailure = false; // 确保在第一个失败时停止
});

//Add Casbin Authorization
builder.Services.AddCasbinAuthorization(options =>
{
    options.PreferSubClaimType = ClaimTypes.Name;
    options.DefaultModelPath = Path.Combine("CasbinConf", "basic_model.conf");
    //options.DefaultPolicyPath = Path.Combine("CasbinConf", "basic_policy.csv");
    options.DefaultEnforcerFactory = (p, m) =>
                    new Enforcer(m, new EFCoreAdapter<Guid>(p.GetRequiredService<LocalCasbinDbContext>()));
    // Use BasicRequestTransformer for simple policy matching
    // This will match the policy format: p, sub, obj, act
    options.DefaultRequestTransformerType = typeof(BasicRequestTransformer);
});

var app = builder.Build();
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
//认证
app.UseAuthentication();
//Casbin 授权
app.UseCasbinAuthorization();
//系统自带授权
app.UseAuthorization();

// 添加授权失败处理中间件
app.UseAuthorizationFailureHandling();

app.MapControllers();

app.Run();
