# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the entire project
COPY . .

# Restore dependencies
RUN dotnet restore sample/SampleServer/SampleServer.csproj

# Build the sample server
RUN dotnet build sample/SampleServer/SampleServer.csproj -c Release --no-restore

# Publish the application
RUN dotnet publish sample/SampleServer/SampleServer.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Create a volume for workspace files
VOLUME ["/workspace"]

# Set the working directory to workspace
WORKDIR /workspace

# The LSP server communicates via stdin/stdout
ENTRYPOINT ["dotnet", "/app/SampleServer.dll"]