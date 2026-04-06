namespace SbomQualityGate.Application.Interfaces;

public interface ISbomXmlConverter
{
    string ConvertToJson(string xmlContent);
}
