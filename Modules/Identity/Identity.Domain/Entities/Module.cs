using Identity.Domain.Common;

namespace Identity.Domain.Entities;

public class Module : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    public ICollection<ModuleRole> ModuleRoles { get; private set; } = new List<ModuleRole>();

    private Module() : base(Guid.Empty) { }

    public Module(Guid id, string code, string name)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Module code is required");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Module name is required");

        Code = code;
        Name = name;
    }
}
