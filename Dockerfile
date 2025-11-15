# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Api/SpaceTerminal.sln ./
COPY Api/SpaceTerminal.Api/SpaceTerminal.Api.csproj ./SpaceTerminal.Api/
COPY Api/SpaceTerminal.Core/SpaceTerminal.Core.csproj ./SpaceTerminal.Core/
COPY Api/SpaceTerminal.Infrastructure/SpaceTerminal.Infrastructure.csproj ./SpaceTerminal.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY Api/ ./

# Build and publish
RUN dotnet publish SpaceTerminal.Api/SpaceTerminal.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000

# Run the application
ENTRYPOINT ["dotnet", "SpaceTerminal.Api.dll"]
