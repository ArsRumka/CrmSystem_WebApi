using Identity.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Entities
{
    public class Module : Entity
    {
        public Guid OrganizationId { get; private set; }
        public string Name { get; private set; }

        public ICollection<ModuleRole> ModuleRoles { get; private set; } = new List<ModuleRole>();

        private Module() : base(Guid.Empty) { }

        public Module(Guid id, Guid organizationId, string name)
            : base(id)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId is required");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Module name is required");

            OrganizationId = organizationId;
            Name = name;
        }
    }
}
