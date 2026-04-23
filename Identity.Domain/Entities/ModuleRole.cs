using Identity.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Entities
{
    public class ModuleRole : Entity
    {
        public Guid OrganizationId { get; private set; }

        public Guid RoleId { get; private set; }
        public Guid ModuleId { get; private set; }

        public Role Role { get; private set; } = null!;
        public Module Module { get; private set; } = null!;

        private ModuleRole() : base(Guid.Empty) { }

        public ModuleRole(Guid id, Guid organizationId, Guid roleId, Guid moduleId)
            : base(id)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId is required");

            if (roleId == Guid.Empty)
                throw new ArgumentException("RoleId is required");

            if (moduleId == Guid.Empty)
                throw new ArgumentException("ModuleId is required");

            OrganizationId = organizationId;
            RoleId = roleId;
            ModuleId = moduleId;
        }
    }
}
