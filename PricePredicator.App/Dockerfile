# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["PricePredicator.App/PricePredicator.App.csproj", "PricePredicator.App/"]
COPY ["PricePredicator.Infrastructure/PricePredicator.Infrastructure.csproj", "PricePredicator.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "PricePredicator.App/PricePredicator.App.csproj"

# Copy source code
COPY . .

# Build application
RUN dotnet build "PricePredicator.App/PricePredicator.App.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "PricePredicator.App/PricePredicator.App.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose gRPC port
EXPOSE 50051

ENTRYPOINT ["dotnet", "PricePredicator.App.dll"]

