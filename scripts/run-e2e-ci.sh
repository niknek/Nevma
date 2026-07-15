#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${root}"
mkdir -p .ci-logs
umask 077

db_secret="ci-database-password-0123456789"
rabbit_secret="ci-rabbit-password-0123456789"
redis_secret="ci-redis-password-0123456789"
cat > .env <<EOF
POSTGRES_ADMIN_PASSWORD=ci-postgres-admin-password-0123456789
NEVMA_IDENTITY_DB_PASSWORD=${db_secret}
NEVMA_PLANNING_DB_PASSWORD=${db_secret}
NEVMA_MESSAGING_DB_PASSWORD=${db_secret}
NEVMA_NOTIFICATIONS_DB_PASSWORD=${db_secret}
NEVMA_FILES_DB_PASSWORD=${db_secret}
NEVMA_COMMANDS_DB_PASSWORD=${db_secret}
RABBITMQ_DEFAULT_USER=nevma
RABBITMQ_DEFAULT_PASS=${rabbit_secret}
REDIS_PASSWORD=${redis_secret}
EOF

pids=()
cleanup() {
  status=$?
  for pid in "${pids[@]:-}"; do kill "${pid}" 2>/dev/null || true; done
  if (( status != 0 )); then
    for log in .ci-logs/*.log; do
      echo "--- ${log}"
      tail -n 100 "${log}" || true
    done
  fi
  docker compose down --volumes --remove-orphans >/dev/null 2>&1 || true
  rm -f .env
  exit "${status}"
}
trap cleanup EXIT

docker compose up -d --wait --wait-timeout 240

export ASPNETCORE_ENVIRONMENT=Development
export Authentication__Issuer=https://localhost:7293/
export MessageBroker__Enabled=true
export MessageBroker__Uri="amqp://nevma:${rabbit_secret}@localhost:5672/"
export Redis__Enabled=true
export Redis__ConnectionString="localhost:6379,password=${redis_secret},ssl=False,abortConnect=False"
export OpenTelemetry__Otlp__Enabled=false
export ConnectionStrings__IdentityDatabase="Host=localhost;Port=5432;Database=nevma_identity;Username=nevma_identity;Password=${db_secret}"
export ConnectionStrings__PlanningDatabase="Host=localhost;Port=5432;Database=nevma_planning;Username=nevma_planning;Password=${db_secret}"
export ConnectionStrings__MessagingDatabase="Host=localhost;Port=5432;Database=nevma_messaging;Username=nevma_messaging;Password=${db_secret}"
export ConnectionStrings__NotificationsDatabase="Host=localhost;Port=5432;Database=nevma_notifications;Username=nevma_notifications;Password=${db_secret}"
export ConnectionStrings__FilesDatabase="Host=localhost;Port=5432;Database=nevma_files;Username=nevma_files;Password=${db_secret}"
export ConnectionStrings__CommandsDatabase="Host=localhost;Port=5432;Database=nevma_commands;Username=nevma_commands;Password=${db_secret}"

cert_password="ci-certificate-password-0123456789"
cert_path="${RUNNER_TEMP:-/tmp}/nevma-ci.pfx"
cert_pem="${RUNNER_TEMP:-/tmp}/nevma-ci.crt"
dotnet dev-certs https --clean >/dev/null
dotnet dev-certs https --export-path "${cert_path}" --password "${cert_password}" >/dev/null
openssl pkcs12 -in "${cert_path}" -clcerts -nokeys -out "${cert_pem}" -passin "pass:${cert_password}" >/dev/null 2>&1
sudo cp "${cert_pem}" /usr/local/share/ca-certificates/nevma-ci.crt
sudo update-ca-certificates >/dev/null
export ASPNETCORE_Kestrel__Certificates__Default__Path="${cert_path}"
export ASPNETCORE_Kestrel__Certificates__Default__Password="${cert_password}"

dotnet tool restore
bash scripts/migrate-all.sh

start_service() {
  local name="$1" directory="$2" assembly="$3" urls="$4"
  (
    cd "${root}/${directory}"
    exec dotnet "bin/Release/net10.0/${assembly}" --urls "${urls}"
  ) > "${root}/.ci-logs/${name}.log" 2>&1 &
  pids+=("$!")
}

start_service identity src/Services/Identity/Nevma.Identity.Api Nevma.Identity.Api.dll "https://localhost:7293;http://localhost:5016"
start_service planning src/Services/Planning/Nevma.Planning.Api Nevma.Planning.Api.dll http://localhost:5276
start_service messaging src/Services/Messaging/Nevma.Messaging.Api Nevma.Messaging.Api.dll http://localhost:5085
start_service notifications src/Services/Notifications/Nevma.Notifications.Api Nevma.Notifications.Api.dll http://localhost:5095
start_service files src/Services/Files/Nevma.Files.Api Nevma.Files.Api.dll http://localhost:5106
start_service commands src/Services/Commands/Nevma.Commands.Api Nevma.Commands.Api.dll http://localhost:5116
start_service gateway src/Gateway/Nevma.Gateway Nevma.Gateway.dll http://localhost:5033

health_urls=(
  https://localhost:7293/health
  http://localhost:5276/health
  http://localhost:5085/health
  http://localhost:5095/health
  http://localhost:5106/health
  http://localhost:5116/health
  http://localhost:5033/health
)
for url in "${health_urls[@]}"; do
  ready=false
  for _ in {1..60}; do
    if curl --fail --silent --show-error --max-time 3 "${url}" >/dev/null; then
      ready=true
      break
    fi
    sleep 2
  done
  if [[ "${ready}" != true ]]; then
    echo "Service did not become healthy: ${url}" >&2
    exit 1
  fi
done

python3 scripts/verify-api-contracts.py

export NEVMA_RUN_E2E=true
dotnet test tests/Nevma.EndToEndTests/Nevma.EndToEndTests.csproj \
  --configuration Release \
  --no-restore \
  --no-build
