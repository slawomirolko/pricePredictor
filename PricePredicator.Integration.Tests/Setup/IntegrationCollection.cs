namespace PricePredicator.Integration.Tests.Setup;

[CollectionDefinition("integration")]
public class IntegrationCollection : ICollectionFixture<PostgresContainerFixture>;