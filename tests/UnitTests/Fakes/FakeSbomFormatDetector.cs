using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeSbomFormatDetector : ISbomFormatDetector
{
    public SbomFormat FormatToReturn { get; init; } = SbomFormat.Json;

    public SbomFormat Detect(string content) => FormatToReturn;
}
