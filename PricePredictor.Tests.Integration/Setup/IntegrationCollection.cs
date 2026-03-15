namespace PricePredictor.Tests.Integration.Setup;

[CollectionDefinition("integration")]
public class IntegrationCollection : ICollectionFixture<PostgresContainerFixture>;