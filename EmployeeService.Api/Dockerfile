# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the whole repo (because we build from repo root)
COPY . .

# Restore explicitly pointing to the csproj
RUN dotnet restore EmployeeService.Api/EmployeeService.Api.csproj

# Publish explicitly pointing to the csproj
RUN dotnet publish EmployeeService.Api/EmployeeService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EmployeeService.Api.dll"]

