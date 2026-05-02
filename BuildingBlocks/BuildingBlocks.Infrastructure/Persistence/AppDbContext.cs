using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Infrastructure.Persistence
{
    public abstract class AppDbContext : DbContext
    {
        protected AppDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ApplyConfigurations(modelBuilder);
        }

        protected virtual void ApplyConfigurations(ModelBuilder modelBuilder)
        {
        }
    }
}
