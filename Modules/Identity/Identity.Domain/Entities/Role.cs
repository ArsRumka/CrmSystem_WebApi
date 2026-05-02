using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class Role : Entity
{
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = null!;

    public ICollection<ModuleRole> ModuleRoles { get; private set; } = new List<ModuleRole>();

    private Role() : base(Guid.Empty) { }

    public Role(Guid id, Guid organizationId, string name)
        : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required");

        OrganizationId = organizationId;
        Name = name;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required");

        Name = name;
    }
}
