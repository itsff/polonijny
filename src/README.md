# Source Overview

This directory contains the web application, shared domain/data-access libraries, and a few command-line tools used to maintain dictionary data.

## Project Map

| Project | Type | Purpose | Local Dependencies |
| --- | --- | --- | --- |
| `SlownikPolonijny.Web` | ASP.NET Core MVC app | Main dictionary website (browse/search entries, add/edit/admin, login) | `SlownikPolonijny.Dal`, `SlownikPolonijny.Dal.Json`, `SlownikPolonijny.Dal.Mongo` |
| `SlownikPolonijny.Dal` | Class library | Core domain model (`Entry`), repository interface, audit rules | - |
| `SlownikPolonijny.Dal.Json` | Class library | File-based `IRepository` implementation backed by JSON | `SlownikPolonijny.Dal` |
| `SlownikPolonijny.Dal.Mongo` | Class library | MongoDB `IRepository` implementation | `SlownikPolonijny.Dal` |
| `SlownikPolonijny.Tools.MongoToJson` | CLI tool | Exports Mongo data into JSON-store format | `SlownikPolonijny.Dal`, `SlownikPolonijny.Dal.Json`, `SlownikPolonijny.Dal.Mongo` |
| `SlownikPolonijny.Tools.HashPassword` | CLI tool | Generates ASP.NET Identity password hashes for `users.json` | - |
| `Auditor` | CLI tool | Runs offline audit checks on `entries.json` | `SlownikPolonijny.Dal`, `SlownikPolonijny.Dal.Json` |
| `SlownikPolonijny.Playground` | Console app | Developer playground for quick experiments | `SlownikPolonijny.Dal`, `SlownikPolonijny.Dal.Mongo` |

## Main App (`SlownikPolonijny.Web`)

- Startup wiring is in `SlownikPolonijny.Web/Startup.cs`.
- Data provider is selected via config (`Dal:Provider`): `Json` or `Mongo`.
- Default JSON data files:
  - `data/entries.json`
  - `data/users.json`
- Authentication is cookie-based; admin policy requires role `@admin`.

## Dictionary Data Model

Core dictionary model is `Entry` in `SlownikPolonijny.Dal/Entry.cs`.

Important fields:

- `name`
- `meanings`
- `englishMeanings`
- `seeAlso`
- `examples`
- admin/audit metadata (`approvedBy`, `timeAdded`, `lastModified`, `ipAddress`, `fromInternet`)

### JSON Store Shape

The JSON DAL expects an object with top-level arrays:

```json
{
  "entries": [
    {
      "id": "...",
      "name": "...",
      "meanings": [],
      "englishMeanings": [],
      "seeAlso": [],
      "examples": []
    }
  ],
  "deletedEntries": []
}
```

User auth JSON (`data/users.json`) is an array:

```json
[
  {
    "username": "admin",
    "passwordHash": "...",
    "roles": ["@admin"]
  }
]
```

Note: both `JsonRepository` and `FileUserService` are configured to allow JSON comments and trailing commas when reading these files.

## Linking Between Entries

There are two link mechanisms:

1. `seeAlso`
   - Explicit list of related entry names.
   - Rendered as direct links on the entry page.

2. Inline bracket markup in text fields
   - Supported syntax:
     - `[text]`
     - `[text|link]`
   - Regex is defined in `SlownikPolonijny.Dal/Entry.cs` (`Entry.LinkRegex`).
   - Renderer resolves links in:
     - `meanings`
     - `englishMeanings`
     - `examples`
   - Implemented in `SlownikPolonijny.Web/Views/Home/Entry.cshtml` (and similarly in the homepage example teaser).

How it resolves:

- `[text]` -> display `text`, link target `text`
- `[text|link]` -> display `text`, link target `link`

## Tools and Typical Uses

- `SlownikPolonijny.Tools.HashPassword`: generate hash for a new local/dev user password.
- `SlownikPolonijny.Tools.MongoToJson`: migrate/export dictionary data from MongoDB into JSON files.
- `Auditor`: validate entry quality and link integrity from a JSON dataset.

## Quick Start Notes

- If you run the web app with JSON DAL, ensure `data/entries.json` and `data/users.json` exist.
- If you run with Mongo DAL, provide Mongo config values in app settings or environment variables.
