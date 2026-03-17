# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "."]

COPY ["PricePredictor.FinanceTrackerApp/PricePredictor.FinanceTrackerApp.csproj", "PricePredictor.FinanceTrackerApp/"]
COPY ["PricePredictor.Application/PricePredictor.Application.csproj", "PricePredictor.Application/"]
COPY ["PricePredictor.Domain/PricePredictor.Domain.csproj", "PricePredictor.Domain/"]
COPY ["PricePredictor.Infrastructure/PricePredictor.Infrastructure.csproj", "PricePredictor.Infrastructure/"]
COPY ["PricePredictor.Persistence/PricePredictor.Persistence.csproj", "PricePredictor.Persistence/"]

RUN dotnet restore "PricePredictor.FinanceTrackerApp/PricePredictor.FinanceTrackerApp.csproj"

COPY . .

RUN dotnet build "PricePredictor.FinanceTrackerApp/PricePredictor.FinanceTrackerApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PricePredictor.FinanceTrackerApp/PricePredictor.FinanceTrackerApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PricePredictor.FinanceTrackerApp.dll"]
