# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY MarsVista.sln .
COPY src/MarsVista.Core/MarsVista.Core.csproj src/MarsVista.Core/
COPY src/MarsVista.Api/MarsVista.Api.csproj src/MarsVista.Api/

# Restore dependencies
RUN dotnet restore src/MarsVista.Api/MarsVista.Api.csproj

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/MarsVista.Api/MarsVista.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Railway provides PORT environment variable
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-5000}

# Expose port (Railway will override with $PORT)
EXPOSE 5000

# Run the application
ENTRYPOINT ["dotnet", "MarsVista.Api.dll"]
