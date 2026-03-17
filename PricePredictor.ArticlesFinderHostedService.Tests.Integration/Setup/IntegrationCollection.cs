namespace PricePredictor.ArticlesFinderHostedService.Tests.Integration.Setup;

[CollectionDefinition("integration")]
public sealed class IntegrationCollection : ICollectionFixture<PostgresContainerFixture>;
