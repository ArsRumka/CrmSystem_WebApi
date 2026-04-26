using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContext : AppDbContext
    {
        private readonly IEnumerable<IEfConfigurationAssemblyProvider> _configurationAssemblyProviders;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IEnumerable<IEfConfigurationAssemblyProvider> configurationAssemblyProviders)
            : base(options)
        {
            _configurationAssemblyProviders = configurationAssemblyProviders;
        }

        protected override void ApplyConfigurations(ModelBuilder modelBuilder)
        {
            foreach (var provider in _configurationAssemblyProviders)
            {
                modelBuilder.ApplyConfigurationsFromAssembly(provider.Assembly);
            }
        }
    }
}
