# Build application
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /source
COPY * ./
RUN build.cmd --target TestNoGameData Compile

# Create run stage
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /artifacts/* ./
ENTRYPOINT ["dotnet", "Universalis.Application.dll"]