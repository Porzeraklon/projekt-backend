# Etap 1: Budowanie
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy pliki projektów (.csproj), aby przywrócić pakiety (warstwa cache)
COPY ["MainApi/MainApi.csproj", "MainApi/"]
COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]
RUN dotnet restore "MainApi/MainApi.csproj"

# Kopiujemy całą resztę kodu
COPY . .
WORKDIR "/src/MainApi"

# Budujemy i publikujemy aplikację w trybie Release
RUN dotnet publish "MainApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etap 2: Środowisko uruchomieniowe (lekki obraz)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# W .NET 8 domyślny port w kontenerze to 8080
EXPOSE 8080 
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MainApi.dll"]