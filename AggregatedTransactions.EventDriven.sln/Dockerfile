# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Aggregator.Api/Aggregator.Api.csproj", "Aggregator.Api/"]
RUN dotnet restore "Aggregator.Api/Aggregator.Api.csproj"
COPY . .
WORKDIR "/src/Aggregator.Api"
RUN dotnet build "Aggregator.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Aggregator.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aggregator.Api.dll"]
