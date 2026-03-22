using SbomQualityGate.Application.UseCases;

namespace SbomQualityGate.Application.Interfaces;

public interface ISubmitSbomHandler
{
    Task<Guid> HandleAsync(SubmitSbomCommand command, CancellationToken cancellationToken);
}
