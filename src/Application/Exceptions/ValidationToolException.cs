namespace SbomQualityGate.Application.Exceptions;

public class ValidationToolException(string message, int exitCode) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}
