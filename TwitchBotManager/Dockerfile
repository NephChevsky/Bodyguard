FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
ENV TZ=Europe/Paris
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY "TwitchBotManager" "TwitchBotManager/"
COPY "Models" "Models/"
COPY "Db" "Db/"
COPY "TwitchApi" "TwitchApi/"
COPY "config.json" "."
COPY "secret.json" "."
COPY "nlog.config" "."

RUN dotnet restore "TwitchBotManager/TwitchBotManager.csproj"
WORKDIR "/src/TwitchBotManager"

RUN dotnet build "TwitchBotManager.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TwitchBotManager.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TwitchBotManager.dll"]