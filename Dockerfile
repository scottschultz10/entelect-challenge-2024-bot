FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /entelect-sproutopia-bot

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /entelect-sproutopia-bot
COPY --from=build-env /entelect-sproutopia-bot/out .
ENTRYPOINT ["dotnet", "SproutReferenceBot.dll"]