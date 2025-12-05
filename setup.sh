#!/bin/bash

cd src/MyGamingBot.Worker

echo "Setting up secrets..."

dotnet user-secrets set "Discord:Token" "PASTE_YOUR_BOT_TOKEN_HERE"

# Google AI Gemini Key (Required for /ask)
dotnet user-secrets set "GoogleAi:Key" "PASTE_YOUR_GEMINI_KEY_HERE"

# Google Search Keys (Required for /ask to be up-to-date)
dotnet user-secrets set "GoogleSearch:Key" "PASTE_YOUR_SEARCH_API_KEY_HERE"
dotnet user-secrets set "GoogleSearch:Cx" "PASTE_YOUR_SEARCH_ENGINE_ID_HERE"

# (Right-click your "Member" role in Discord -> Copy Role ID)
dotnet user-secrets set "GuildSettings:WelcomeRole" "PASTE_YOUR_ROLE_ID_HERE"

echo "✅ Secrets configured successfully!"

cd ../..

# for Lavalink
cd Lavalink

wget https://github.com/lavalink-devs/Lavalink/releases/download/3.7.13/Lavalink.jar

java -jar Lavalink.jar