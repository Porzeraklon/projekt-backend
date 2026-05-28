# 1. Etap budowania (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy pliki projektów i przywracamy zależności (nugety)
COPY ["MainApi/MainApi.csproj", "MainApi/"]
COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]
RUN dotnet restore "MainApi/MainApi.csproj"

# Kopiujemy całą resztę kodu
COPY . .

# Przechodzimy do folderu API i budujemy aplikację
WORKDIR "/src/MainApi"
RUN dotnet publish "MainApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Etap uruchamiania (Lekki obraz Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Ustawiamy port, na którym nasłuchuje kontener (domyślnie 8080 w .NET 8)
EXPOSE 8080

ENTRYPOINT ["dotnet", "MainApi.dll"]