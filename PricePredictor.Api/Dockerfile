# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["PricePredictor.Api/PricePredictor.Api.csproj", "PricePredictor.Api/"]
COPY ["PricePredictor.Infrastructure/PricePredictor.Infrastructure.csproj", "PricePredictor.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "PricePredictor.Api/PricePredictor.Api.csproj"

# Copy source code
COPY . .

# Build application
RUN dotnet build "PricePredictor.Api/PricePredictor.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "PricePredictor.Api/PricePredictor.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose REST + gRPC ports
EXPOSE 5000
EXPOSE 50051

ENTRYPOINT ["dotnet", "PricePredictor.Api.dll"]
