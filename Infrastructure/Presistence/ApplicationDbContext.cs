using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Presistence
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
                typeof(Identity.Infrastructure.Configurations.OrganizationConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(Identity.Infrastructure.Configurations.RoleConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(Identity.Infrastructure.Configurations.ModuleConfiguration).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(Identity.Infrastructure.Configurations.ModuleRoleConfiguration).Assembly);
        }
    }
}
