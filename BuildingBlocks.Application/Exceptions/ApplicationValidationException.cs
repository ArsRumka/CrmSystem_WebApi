namespace BuildingBlocks.Application.Exceptions;

public class ApplicationValidationException : Exception
{
    public ApplicationValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
