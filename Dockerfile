# Use the official .NET 9 SDK image to build and publish the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the project files
COPY . ./

# Restore dependencies
RUN dotnet restore

# Build the application
RUN dotnet publish -c Release -o out

# Use the official .NET 9 runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/out .

# Expose the port the application runs on
EXPOSE 5000
EXPOSE 5001

# Set the entry point for the container
ENTRYPOINT ["dotnet", "appp.dll"]
