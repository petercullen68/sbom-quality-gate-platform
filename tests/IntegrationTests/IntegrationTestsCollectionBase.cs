using SbomQualityGate.IntegrationTests.Infrastructure;

namespace SbomQualityGate.IntegrationTests;

[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class IntegrationTestsCollectionBase : ICollectionFixture<SbomQualityGateApiFactory>;
