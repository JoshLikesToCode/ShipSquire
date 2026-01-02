FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ShipSquire.Domain/ShipSquire.Domain.csproj ShipSquire.Domain/
COPY ShipSquire.Application/ShipSquire.Application.csproj ShipSquire.Application/
COPY ShipSquire.Infrastructure/ShipSquire.Infrastructure.csproj ShipSquire.Infrastructure/
COPY ShipSquire.Api/ShipSquire.Api.csproj ShipSquire.Api/
RUN dotnet restore ShipSquire.Api/ShipSquire.Api.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/ShipSquire.Api
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "ShipSquire.Api.dll"]
