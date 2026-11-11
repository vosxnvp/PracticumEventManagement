
using Xunit;

namespace PracticumEventManagement.IntegrationTests.Fixtures;

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgreSqlCollection
    : ICollectionFixture<PostgreSqlFixture>
{
}
