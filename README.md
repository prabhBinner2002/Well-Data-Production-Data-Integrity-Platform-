# Well Production Data Integrity Platform

A backend that loads Alberta well-production data from [Petrinex](https://www.petrinex.ca/PD/Pages/APD.aspx) into SQL Server, checks it for data-quality problems, and serves the results through a REST API. Approved months can be closed and exported as an ERP-ready CSV.

**Stack:** C# / .NET 10, ASP.NET Core Web API, EF Core 10 (plus raw SQL), SQL Server 2022 in Docker, xUnit + Testcontainers.

## How it works

```
Petrinex  ->  Download & cache  ->  Stage raw rows  ->  Validate  ->  Promote
                                                                        |
                                    Clean records + quality issues + import log
                                                                        |
                                    REST API  ->  Close month  ->  ERP CSV export
```

For each month:

1. **Fetch** the CSV and cache it locally.
2. **Skip** it if the same file version (SHA-256 hash) is already loaded.
3. **Stage** the rows with `SqlBulkCopy`.
4. **Validate** in C#. Rows with errors are held back with an explanation.
5. **Promote** the valid rows in a single transaction.
6. **Run SQL rules** across the loaded month.

The records table is a temporal table, so if Petrinex revises a month, the old values are kept and the changes can be reported.

## Validation rules

**File checks (C#):** bad formats, missing volumes, negative volumes, hours over the month's limit, volume with no hours, unknown codes, and masked (`***`) values.

**Data checks (SQL):** duplicates, unknown wells, gaps in reporting, month-over-month spikes, outliers against a 12-month average, unusual water cut or gas-oil ratio, revised values, and wells missing an ERP cost centre.

Rules can be enabled or disabled through the API. Thresholds live in `appsettings.json`.

## Setup

Requirements: .NET 10 SDK, Docker Desktop, PowerShell.

```
./scripts/setup.ps1                 # secrets, SQL Server, migrations
./scripts/backfill.ps1 -Months 12   # load data
dotnet run --project src/WellData.Api
```

Swagger is at `http://localhost:5119/swagger`. To run everything in Docker instead, use `docker compose up -d --build` and go to `http://localhost:8080/swagger`.

Tests: `./scripts/run-tests.ps1` (add `-UnitOnly` to skip the Docker-based tests).

## Using the API

Get a token from `POST /api/auth/token`, then click **Authorize** in Swagger. There are two roles: `viewer` (read-only) and `steward` (can review issues, run imports, close months, and export). Their passwords are in `.env`.

A typical month looks like this:

1. `POST /api/imports` to load the month
2. `GET /api/issues?month=2026-06&severity=Error` to review problems
3. `PUT /api/erp/cost-centres/{wellId}` to fill in any missing cost centres
4. `POST /api/periods/2026-06/close`
5. `POST /api/erp/exports/2026-06`

Lists are paged. Errors use RFC 7807 problem details. Updates use optimistic concurrency.

## Results on real data

12 months of data for 11 operators:

- **1,033,021** rows loaded, **15** rejected
- **74,541** wells and **18,371** facilities in master data
- About **24 s** to load and validate each month

## Performance highlights

- A columnstore index sped up month-wide totals from 77 ms to 12 ms. However, including the well ID column made per-well queries about 6x slower, so it was left out.
- A text-type mismatch (`nvarchar` vs `varchar`) was blocking an index. Fixing it cut well lookups from roughly 370 ms to 16 ms.

## Security

- Secrets are kept in user-secrets or environment variables, never in the repo.
- All SQL is parameterized.
- The API returns DTOs, not database entities.
- All input is validated, both Petrinex files and API requests.