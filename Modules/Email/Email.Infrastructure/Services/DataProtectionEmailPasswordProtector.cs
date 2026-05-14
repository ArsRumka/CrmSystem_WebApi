using Email.Application.Abstractions.Services;
using Microsoft.AspNetCore.DataProtection;

namespace Email.Infrastructure.Services;

public sealed class DataProtectionEmailPasswordProtector : IEmailPasswordProtector
{
    private const string Purpose = "EmailSettings.SmtpPassword";
    private readonly IDataProtector _protector;

    public DataProtectionEmailPasswordProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Protect(string plainPassword)
    {
        return _protector.Protect(plainPassword);
    }

    public string Unprotect(string encryptedPassword)
    {
        return _protector.Unprotect(encryptedPassword);
    }
}
