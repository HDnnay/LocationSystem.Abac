using Casbin.Persist.Adapter.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Abac.WebApi
{
    public class LocalCasbinDbContext : CasbinDbContext<Guid>
    {
        public LocalCasbinDbContext(DbContextOptions<LocalCasbinDbContext> options) 
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // 如果需要自定义表名或架构，可以在这里配置
            // modelBuilder.Entity<CasbinRule<Guid>>().ToTable("CasbinRules", "security");
        }
    }
}