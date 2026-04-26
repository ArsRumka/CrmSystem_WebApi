using System.Reflection;

namespace Infrastructure.Persistence;

public interface IEfConfigurationAssemblyProvider
{
    Assembly Assembly { get; }
}
