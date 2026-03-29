namespace SbomQualityGate.Application.Exceptions;

public sealed class RequestValidationException(string message) : Exception(message)
{
}
