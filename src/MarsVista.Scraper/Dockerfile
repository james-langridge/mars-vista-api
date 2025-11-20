# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/MarsVista.Api/MarsVista.Api.csproj", "MarsVista.Api/"]
COPY ["src/MarsVista.Scraper/MarsVista.Scraper.csproj", "MarsVista.Scraper/"]

# Restore dependencies
RUN dotnet restore "MarsVista.Scraper/MarsVista.Scraper.csproj"

# Copy source code
COPY src/MarsVista.Api/ MarsVista.Api/
COPY src/MarsVista.Scraper/ MarsVista.Scraper/

# Build and publish
WORKDIR "/src/MarsVista.Scraper"
RUN dotnet publish "MarsVista.Scraper.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish .

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production

# Run the scraper
ENTRYPOINT ["dotnet", "MarsVista.Scraper.dll"]
