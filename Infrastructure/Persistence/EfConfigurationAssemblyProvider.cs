using System.Reflection;

namespace Infrastructure.Persistence;

public sealed class EfConfigurationAssemblyProvider : IEfConfigurationAssemblyProvider
{
    public EfConfigurationAssemblyProvider(Assembly assembly)
    {
        Assembly = assembly;
    }

    public Assembly Assembly { get; }
}
