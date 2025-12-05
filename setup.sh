#!/bin/bash

cd src/MyGamingBot.Worker

echo "Setting up secrets..."

dotnet user-secrets set "Discord:Token" "PASTE_YOUR_BOT_TOKEN_HERE"

# 2. Google AI Gemini Key (Required for /ask)
dotnet user-secrets set "GoogleAi:Key" "PASTE_YOUR_GEMINI_KEY_HERE"

# 3. Google Search Keys (Required for /ask to be up-to-date)
dotnet user-secrets set "GoogleSearch:Key" "PASTE_YOUR_SEARCH_API_KEY_HERE"
dotnet user-secrets set "GoogleSearch:Cx" "PASTE_YOUR_SEARCH_ENGINE_ID_HERE"

# 4. Welcome Role ID (Required for auto-role feature)
# (Right-click your "Member" role in Discord -> Copy Role ID)
dotnet user-secrets set "GuildSettings:WelcomeRole" "PASTE_YOUR_ROLE_ID_HERE"

# 5. Tracker.gg Key (Required for /stats - paused feature)
dotnet user-secrets set "TrackerApi:Key" "PASTE_YOUR_TRACKER_GG_KEY_HERE"

echo "✅ Secrets configured successfully!"

# Go back to root
cd ../..