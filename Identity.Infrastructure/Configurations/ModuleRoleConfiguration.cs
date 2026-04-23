using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Configurations
{
    public class ModuleRoleConfiguration : IEntityTypeConfiguration<ModuleRole>
    {
        public void Configure(EntityTypeBuilder<ModuleRole> builder)
        {
            builder.ToTable("ModuleRoles");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.RoleId, x.ModuleId })
                .IsUnique();

            builder.HasOne(x => x.Role)
                .WithMany(r => r.ModuleRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Module)
                .WithMany(m => m.ModuleRoles)
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
