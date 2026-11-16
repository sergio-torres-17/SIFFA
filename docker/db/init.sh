#!/bin/bash
# Crea la base SIFFA y aplica los scripts de SIFFA.Database (el proyecto SSDT no se puede compilar en Linux).
# Es idempotente: si la base ya tiene las tablas, no hace nada.
set -euo pipefail

SQLCMD=/opt/mssql-tools18/bin/sqlcmd
run() { "$SQLCMD" -C -b -f 65001 -S db -U sa -P "$MSSQL_SA_PASSWORD" "$@"; }
DB=/db

if [ "$(run -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = 'SIFFA'")" = "0" ]; then
  echo "Creando la base de datos SIFFA..."
  run -Q "CREATE DATABASE [SIFFA]"
fi

if [ "$(run -d SIFFA -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.tables WHERE name = 'User' AND SCHEMA_NAME(schema_id) = 'Security'")" != "0" ]; then
  echo "La base SIFFA ya tiene su estructura. Nada que hacer."
  exit 0
fi

apply() {
  local f
  for f in "$@"; do
    echo "  $f"
    run -d SIFFA -i "$f"
  done
}

echo "Aplicando estructura..."
apply "$DB"/Schemas/*.sql
apply "$DB"/Catalog/Tables/*.sql "$DB"/Employee/Tables/*.sql "$DB"/AccessControl/Tables/*.sql \
      "$DB"/Add/Tables/*.sql "$DB"/Security/Tables/*.sql
apply "$DB"/*/Indexes/*.sql
apply "$DB"/*/"Foreign Keys"/*.sql
apply "$DB"/Security/"Store Procedures"/*.sql

echo "Cargando datos básicos..."
run -d SIFFA -Q "EXEC [Security].[Usp_Insert_Basics]; DROP PROCEDURE [Security].[Usp_Insert_Basics];"
echo "Listo."
