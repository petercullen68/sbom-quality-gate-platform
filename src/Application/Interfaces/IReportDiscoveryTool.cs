using SbomQualityGate.Application.Models;

namespace SbomQualityGate.Application.Interfaces;

public interface IReportDiscoveryTool
{
    ReportDiscoveryResult Discover(string reportJson);
}
