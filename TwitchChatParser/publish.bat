cd ..
docker rm twitch-chat-parser-*
docker build -t twitch-chat-parser -f TwitchChatParser/Dockerfile .
pause