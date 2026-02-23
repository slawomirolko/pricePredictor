var builder = DistributedApplication.CreateBuilder(args);


var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithGPUSupport()
    .WithEndpoint(port: 11434, targetPort: 11434);


var mongo = builder.AddContainer("mongo-atlas", "mongodb/mongodb-atlas-local")
    .WithEndpoint(port: 27017, targetPort: 27017, name: "mongodb");



var phi3 = ollama.AddModel("phi3:latest");
var app = builder.AddProject<Projects.PricePredicator_App>("pricepredicator-app");

builder.Build().Run();