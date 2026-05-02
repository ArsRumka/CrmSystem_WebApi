using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class ModuleRole : Entity
{
    public Guid OrganizationId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid ModuleId { get; private set; }
    public bool CanRead { get; private set; }
    public bool CanCreate { get; private set; }
    public bool CanUpdate { get; private set; }
    public bool CanDelete { get; private set; }

    public Role Role { get; private set; } = null!;
    public Module Module { get; private set; } = null!;

    private ModuleRole() : base(Guid.Empty) { }

    public ModuleRole(
        Guid id,
        Guid organizationId,
        Guid roleId,
        Guid moduleId,
        bool canRead,
        bool canCreate,
        bool canUpdate,
        bool canDelete)
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
        CanRead = canRead;
        CanCreate = canCreate;
        CanUpdate = canUpdate;
        CanDelete = canDelete;
    }

    public void UpdatePermissions(bool canRead, bool canCreate, bool canUpdate, bool canDelete)
    {
        CanRead = canRead;
        CanCreate = canCreate;
        CanUpdate = canUpdate;
        CanDelete = canDelete;
    }
}
