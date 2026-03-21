# Architecture (v1)

## Overview
- API: ASP.NET Core
- Worker: Background service for SBOM validation
- Database: PostgreSQL
- Storage: JSONB (SBOM) + report files

## Core Flow
1. SBOM uploaded via API
2. Stored in database
3. Validation job queued
4. Worker runs SBOMQS
5. Result stored (Pass/Fail)
