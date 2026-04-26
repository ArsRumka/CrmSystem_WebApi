using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContext : AppDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void ApplyConfigurations(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(Identity.Infrastructure.Configurations.UserConfiguration).Assembly);

            modelBuilder.ApplyConfigurationsFromAssembly(
                Assembly.Load("Clients.Infrastructure"));
        }
    }
}
