# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the main project file and restore dependencies
COPY DispatchPublic.csproj .
RUN dotnet restore DispatchPublic.csproj

# Copy the rest of the source code
COPY . .

# Build and publish only the main application project
RUN dotnet build DispatchPublic.csproj -c Release --no-restore --verbosity minimal
RUN dotnet publish DispatchPublic.csproj -c Release --no-build -o /app/publish --verbosity minimal

# Use the official .NET 9.0 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose the port the app runs on
EXPOSE 5600

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5600
ENV ASPNETCORE_ENVIRONMENT=Production

# Set the entry point
ENTRYPOINT ["dotnet", "DispatchPublic.dll"]
