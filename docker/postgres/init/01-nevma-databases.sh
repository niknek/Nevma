#!/bin/sh
set -eu

create_database() {
  database="$1"
  username="$2"
  password="$3"

  psql --set=ON_ERROR_STOP=1 \
    --username "$POSTGRES_USER" \
    --dbname "$POSTGRES_DB" \
    --set=db_name="$database" \
    --set=db_user="$username" \
    --set=db_password="$password" <<-'EOSQL'
SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'db_user', :'db_password')
WHERE NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = :'db_user')
\gexec

SELECT format('CREATE DATABASE %I OWNER %I', :'db_name', :'db_user')
WHERE NOT EXISTS (SELECT FROM pg_catalog.pg_database WHERE datname = :'db_name')
\gexec
EOSQL
}

create_database nevma_identity nevma_identity "$NEVMA_IDENTITY_DB_PASSWORD"
create_database nevma_planning nevma_planning "$NEVMA_PLANNING_DB_PASSWORD"
create_database nevma_messaging nevma_messaging "$NEVMA_MESSAGING_DB_PASSWORD"
create_database nevma_notifications nevma_notifications "$NEVMA_NOTIFICATIONS_DB_PASSWORD"
create_database nevma_files nevma_files "$NEVMA_FILES_DB_PASSWORD"
create_database nevma_commands nevma_commands "$NEVMA_COMMANDS_DB_PASSWORD"
