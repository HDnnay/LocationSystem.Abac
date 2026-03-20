using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Abac.WebApi
{
    public class LocalCasbinDbContextFactory : IDesignTimeDbContextFactory<LocalCasbinDbContext>
    {
        public LocalCasbinDbContext CreateDbContext(string[] args)
        {
            // 构建配置
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // 获取连接字符串
            var connectionString = configuration.GetConnectionString("SqlServerConnectionString");

            // 构建DbContext选项
            var optionsBuilder = new DbContextOptionsBuilder<LocalCasbinDbContext>();
            optionsBuilder.UseSqlServer(connectionString, 
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName));

            return new LocalCasbinDbContext(optionsBuilder.Options);
        }
    }
}