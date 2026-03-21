# Decisions

## Storage
- PostgreSQL selected over MongoDB
- Reason: relational data + JSONB support for SBOM querying

## SBOM Storage
- Store SBOM as JSONB in Postgres
- Extract key metadata into columns
