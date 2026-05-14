namespace Email.Application.Abstractions.Services;

public interface IEmailPasswordProtector
{
    string Protect(string plainPassword);

    string Unprotect(string encryptedPassword);
}
