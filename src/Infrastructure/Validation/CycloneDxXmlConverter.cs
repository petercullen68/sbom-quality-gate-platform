using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using XmlSerializer = CycloneDX.Xml.Serializer;
using JsonSerializer = CycloneDX.Json.Serializer;

namespace SbomQualityGate.Infrastructure.Validation;

public class CycloneDxXmlConverter : ISbomXmlConverter
{
    public string ConvertToJson(string xmlContent)
    {
        try
        {
            var bom = XmlSerializer.Deserialize(xmlContent);
            return JsonSerializer.Serialize(bom);
        }
        catch (Exception ex)
        {
            throw new RequestValidationException(
                $"CycloneDX XML could not be converted to JSON: {ex.Message}");
        }
    }
}
