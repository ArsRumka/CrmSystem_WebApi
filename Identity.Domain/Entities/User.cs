using Identity.Domain.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Entities
{
    public class User : Entity
    {
        public Guid OrganizationId { get; private set; }
        public Guid RoleId { get; private set; }

        public string Name { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }

        public bool IsActive { get; private set; }

        // Навигация (ОПЦИОНАЛЬНО)
        // необходимость данной навигации под вопросом
        public Role? Role { get; private set; }


        private User() : base(Guid.Empty) { }

        public User(Guid id, Guid organizationId, Guid roleId,
            string name, string email, string passwordHash)
            : base(id)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId is required");

            if (roleId == Guid.Empty)
                throw new ArgumentException("RoleId is required");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash is required");

            OrganizationId = organizationId;
            RoleId = roleId;
            Name = name;
            Email = email;
            PasswordHash = passwordHash;
            IsActive = true;
        }

        public void ChangeRole(Guid newRoleId)
        {
            if (newRoleId == Guid.Empty)
                throw new ArgumentException("Invalid role");

            RoleId = newRoleId;
            Role = null;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
