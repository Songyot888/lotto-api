# ===== Build Stage =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj แยกเพื่อลด cache miss
COPY ["lotto_api.csproj", "./"]
RUN dotnet restore "lotto_api.csproj"

# copy source code
COPY . .
RUN dotnet publish "lotto_api.csproj" -c Release -o /app /p:UseAppHost=false

# ===== Runtime Stage =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "lotto_api.dll", "--urls", "http://0.0.0.0:${PORT}"]