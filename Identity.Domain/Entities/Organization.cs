using Identity.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Entities
{
    public class Organization : Entity
    {
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public bool IsActive { get; private set; }

        private Organization() : base(Guid.Empty) { }

        public Organization(Guid id, string name, string email, string passwordHash)
            : base(id)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Organization name is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash is required");

            Name = name;
            Email = email;
            PasswordHash = passwordHash;
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
