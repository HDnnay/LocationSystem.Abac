using Abac.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Abac.WebApi
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Document> Documents { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Policy> Policies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置索引等
            modelBuilder.Entity<Document>().HasIndex(d => d.OwnerId);
            modelBuilder.Entity<Document>().HasIndex(d => d.Department);
        }
    }
}
