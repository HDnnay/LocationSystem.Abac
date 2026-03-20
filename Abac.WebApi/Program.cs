using Abac.WebApi;
using Abac.WebApi.Authorization;
using Abac.WebApi.Repositories;
using Casbin.AspNetCore.Authorization;
using Casbin.AspNetCore.Authorization.Transformers;
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
// ���� EF Core InMemory ���ݿ⣨������ʾ��
var conn = builder.Configuration.GetConnectionString("SqlServerConnectionString");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(conn));

// ע��ִ��ͷ���
builder.Services.AddScoped<IPolicyRepository, EfCorePolicyRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// ע����Ȩ������
builder.Services.AddScoped<IAuthorizationHandler, AbacAuthorizationHandler>();

// ע���ڴ滺�棨���ڻ�������ı���ʽ��
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

// ע����Ȩ����
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DocumentAccessPolicy", policy =>
        policy.Requirements.Add(new AbacRequirement()));
});

//Add Casbin Authorization
builder.Services.AddCasbinAuthorization(options =>
{
    options.PreferSubClaimType = ClaimTypes.Name;
    options.DefaultModelPath = Path.Combine("CasbinConf", "basic_model.conf");
    options.DefaultPolicyPath = Path.Combine("CasbinConf", "basic_policy.csv");

    // Use BasicRequestTransformer for simple policy matching
    // This will match the policy format: p, sub, obj, act
    options.DefaultRequestTransformerType = typeof(BasicRequestTransformer);
});

var app = builder.Build();
// ��ʼ����������
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
//Casbin
app.UseCasbinAuthorization();
app.UseAuthorization();

app.MapControllers();

app.Run();
