using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeReportDiscoveryTool : IReportDiscoveryTool
{
    public bool WasCalled { get; private set; }
    public bool ShouldThrow { get; init; }

    public ReportDiscoveryResult ResultToReturn { get; init; } = new ReportDiscoveryResult();

    public ReportDiscoveryResult Discover(string reportJson)
    {
        WasCalled = true;

        if (ShouldThrow)
            throw new Application.Exceptions.RequestValidationException("Simulated discovery tool failure.");

        return ResultToReturn;
    }
}
