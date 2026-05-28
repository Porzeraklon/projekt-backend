# Etap 1: Budowanie
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy pliki projektów
COPY ["WorkerService/WorkerService.csproj", "WorkerService/"]
COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]
RUN dotnet restore "WorkerService/WorkerService.csproj"

# Kopiujemy resztę
COPY . .
WORKDIR "/src/WorkerService"

# Publikacja
RUN dotnet publish "WorkerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etap 2: Środowisko uruchomieniowe
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WorkerService.dll"]