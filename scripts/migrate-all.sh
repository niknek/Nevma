#!/usr/bin/env bash
set -euo pipefail

projects=(
  "src/Services/Identity/Nevma.Identity.Api/Nevma.Identity.Api.csproj"
  "src/Services/Planning/Nevma.Planning.Api/Nevma.Planning.Api.csproj"
  "src/Services/Messaging/Nevma.Messaging.Api/Nevma.Messaging.Api.csproj"
  "src/Services/Notifications/Nevma.Notifications.Api/Nevma.Notifications.Api.csproj"
  "src/Services/Files/Nevma.Files.Api/Nevma.Files.Api.csproj"
  "src/Services/Commands/Nevma.Commands.Api/Nevma.Commands.Api.csproj"
)

export ASPNETCORE_ENVIRONMENT=Development
for project in "${projects[@]}"; do
  echo "Applying migrations for ${project}"
  dotnet tool run dotnet-ef database update \
    --project "${project}" \
    --startup-project "${project}" \
    --configuration Release \
    --no-build
done
