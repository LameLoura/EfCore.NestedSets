using Microsoft.EntityFrameworkCore;

namespace EfCore.NestedSets.Tests
{
    public class AppDbContext : DbContext
    {
        public DbSet<ModuleStructure> ModuleStructures { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<ModuleEntry> ModuleEntries { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // define the database to use
            optionsBuilder.UseSqlServer(DbSql.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureNestedSets<ModuleStructure, Module, int, int?>();
        }
    }
}