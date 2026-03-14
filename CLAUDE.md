# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

SlownikPolonijny ("Słownik Hentremicki") is a Polish diaspora slang dictionary web application. It's an ASP.NET Core MVC app with a pluggable data access layer.

## Build & Run Commands

```bash
# Build the solution (from repo root)
dotnet build src/SlownikPolonijny.sln

# Build the Auditor (not in the solution file)
dotnet build src/Auditor/Auditor.csproj

# Run the web app
dotnet run --project src/SlownikPolonijny.Web

# Docker build
cd src && docker build . -f Dockerfile -t polonijny:latest

# Data migration tool
dotnet run --project src/SlownikPolonijny.Tools.MongoToJson -- <input.json> <output.json>
```

There are no test projects in this codebase.

## Architecture

**DAL abstraction (SlownikPolonijny.Dal):** Defines `IRepository` (CRUD for dictionary entries), `Entry` (domain model with Name, Meanings, Examples, SeeAlso, cross-link syntax `[word]` / `[word|link]`), and `IEntryAuditor` (data quality checks).

**Two DAL implementations:**
- **Dal.Mongo** — MongoDB via MongoDB.Driver. Uses Polish collation, BSON class mapping with snake_case fields, soft deletes to a separate collection. Contains `MongoEntryAuditor` with cross-link validation and auto-fix.
- **Dal.Json** — File-based via System.Text.Json. Single JSON file with `ReaderWriterLockSlim` for thread safety. Stores entries and deleted entries in a `JsonStore` wrapper.

**SlownikPolonijny.Web** — ASP.NET Core MVC with Razor views. MongoDB-backed Identity for auth. Role-based authorization (`@admin` role). Key routes: `/haslo/{name}` (entry), `/litera/{letter}` (browse), `/nowe` (latest), `/losuj` (random). Custom `DashedParameterTransformer` converts spaces to dashes in URLs. SHA256-based CAPTCHA for public entry submission. Admin features: edit, approve, remove/restore, audit, mega-audit (with 5-min cache).

**Standalone tools:**
- **Auditor** — Console app for bulk cross-link auditing and auto-fixing against MongoDB directly.
- **Tools.MongoToJson** — Converts MongoDB export (BSON Extended JSON) to the JsonRepository format.
- **Playground** — Dev scratchpad for testing queries and regex.

## Deployment

CI/CD via GitHub Actions (`.github/workflows/deploy.yml`): pushes to `master` trigger a multi-platform Docker build (amd64/arm64) published to GitHub Container Registry. The Dockerfile builds only the Web project.

Configuration is via environment variables: `Mongo__ConnectionString`, `Mongo__DatabaseName`, `Mongo__CollectionName`.

## Issue Tracking

This project uses **bd** (beads). See `AGENTS.md` for the workflow. Key commands: `bd ready`, `bd show <id>`, `bd close <id>`, `bd sync`.
