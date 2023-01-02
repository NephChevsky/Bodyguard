cd ..
docker build -t twitch-bot-manager -f TwitchBotManager/Dockerfile .
docker rm twitch-bot-manager
docker create --name twitch-bot-manager -v "c:/logs:/app/logs" -v "/var/run/docker.sock:/var/run/docker.sock" twitch-bot-manager
pause