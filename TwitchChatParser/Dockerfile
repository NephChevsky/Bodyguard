FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY "TwitchChatParser" "TwitchChatParser/"
COPY "TwitchChat" "TwitchChat/"
COPY "Models" "Models/"
COPY "Db" "Db/"
COPY "TwitchApi" "TwitchApi/"
COPY "config.json" "."
COPY "secret.json" "."
COPY "nlog.config" "."

RUN dotnet restore "TwitchChatParser/TwitchChatParser.csproj"
WORKDIR "/src/TwitchChatParser"

RUN dotnet build "TwitchChatParser.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TwitchChatParser.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TwitchChatParser.dll"]