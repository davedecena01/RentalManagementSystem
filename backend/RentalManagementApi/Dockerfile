# syntax=docker/dockerfile:1
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RentalManagementApi.csproj ./
RUN dotnet restore RentalManagementApi.csproj

COPY . ./
RUN dotnet publish RentalManagementApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
# Railway injects $PORT. Bind to it on all interfaces.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=build /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "RentalManagementApi.dll"]
