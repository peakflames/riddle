namespace Riddle.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Collection definition that shares a SignalRTestFixture across all tests in the collection.
/// This avoids creating a new WebApplicationFactory for each test class.
/// </summary>
[CollectionDefinition("SignalR")]
public class SignalRTestCollection : ICollectionFixture<SignalRTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
