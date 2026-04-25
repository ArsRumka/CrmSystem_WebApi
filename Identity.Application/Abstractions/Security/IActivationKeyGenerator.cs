namespace Identity.Application.Abstractions.Security;

public interface IActivationKeyGenerator
{
    string Generate();
}
