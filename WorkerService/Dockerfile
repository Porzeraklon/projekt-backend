# 1. Etap budowania (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy pliki projektów
COPY ["WorkerService/WorkerService.csproj", "WorkerService/"]
COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]
RUN dotnet restore "WorkerService/WorkerService.csproj"

# Kopiujemy całą resztę kodu
COPY . .

# Budujemy Workera
WORKDIR "/src/WorkerService"
RUN dotnet publish "WorkerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Etap uruchamiania (Lekki obraz Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "WorkerService.dll"]