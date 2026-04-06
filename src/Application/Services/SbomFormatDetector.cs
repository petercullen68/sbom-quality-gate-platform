using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Application.Services;

public class SbomFormatDetector : ISbomFormatDetector
{
    public SbomFormat Detect(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return SbomFormat.Unknown;

        var trimmed = content.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');

        // JSON — starts with {
        if (trimmed.StartsWith('{'))
            return SbomFormat.Json;

        // Tag-Value — SPDX plain text format
        if (trimmed.StartsWith("SPDXVersion:", StringComparison.OrdinalIgnoreCase))
            return SbomFormat.TagValue;

        // XML — check namespace to distinguish CycloneDX from SPDX
        if (trimmed.StartsWith('<'))
        {
            if (trimmed.Contains("cyclonedx.org/schema/bom", StringComparison.OrdinalIgnoreCase))
                return SbomFormat.CycloneDxXml;

            if (trimmed.Contains("spdx.org/rdf/terms", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("<spdxVersion", StringComparison.OrdinalIgnoreCase))
                return SbomFormat.SpdxXml;
        }

        return SbomFormat.Unknown;
    }
}
