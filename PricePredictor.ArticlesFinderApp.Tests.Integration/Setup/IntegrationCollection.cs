namespace PricePredictor.ArticlesFinderApp.Tests.Integration.Setup;

[CollectionDefinition("integration")]
public sealed class IntegrationCollection : ICollectionFixture<PostgresContainerFixture>;
