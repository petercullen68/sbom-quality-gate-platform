using SbomQualityGate.Application.Interfaces;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeSbomXmlConverter : ISbomXmlConverter
{
    public string JsonToReturn { get; init; } = "{}";

    public string ConvertToJson(string xmlContent) => JsonToReturn;
}
