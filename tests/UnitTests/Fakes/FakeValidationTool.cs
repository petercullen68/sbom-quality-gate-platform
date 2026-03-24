using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeValidationTool : IValidationTool
{
    public bool WasCalled { get; private set; }
    public bool ShouldThrow { get; init; }
    public ValidationToolResult ResultToReturn { get; init; } = new ValidationToolResult{            
        Status = ValidationStatus.Pass,
        Score = 95,
        ReportJson = "{}"};

    public Task<ValidationToolResult> ValidateAsync(
        string sbomJson,
        string profile,
        CancellationToken cancellationToken)
    {
        WasCalled = true;

        if (ShouldThrow)
        {
            throw new ValidationToolException(
                "Simulated validation tool failure",
                exitCode: -1);
        }
        
        return Task.FromResult(ResultToReturn);
    }
}
