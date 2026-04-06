using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Application.Interfaces;

public interface ISbomFormatDetector
{
    SbomFormat Detect(string content);
}
