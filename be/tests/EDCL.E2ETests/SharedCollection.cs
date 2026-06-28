using EDCL.IntegrationTests.Infrastructure;
using Xunit;

namespace EDCL.E2ETests;

[CollectionDefinition("IntegrationTestCollection")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces for the E2E Tests assembly.
}
