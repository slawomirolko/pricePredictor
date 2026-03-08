namespace PricePredictor.Integration.Tests.Setup;

[CollectionDefinition("integration")]
public class IntegrationCollection : ICollectionFixture<PostgresContainerFixture>;