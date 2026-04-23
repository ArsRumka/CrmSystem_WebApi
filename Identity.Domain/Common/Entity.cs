using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Common
{
    public abstract class Entity
    {
        public Guid Id { get; protected set; }

        protected Entity(Guid id)
        {
            Id = id;
        }
    }
}
