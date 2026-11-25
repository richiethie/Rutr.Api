# Rutr.Api

ASP.NET Core 8 Web API for Project RUTR (Connect / Reporting pilot). Mosaic-style single project: controllers stay thin; domain + mock integrations live under `Services/`.

**GitHub:** https://github.com/richiethie/Rutr.Api (`main`)

## Intended local home

```text
~/practice/Rutr.Api
C:\Users\rthiesfeldt\practice\Rutr.Api
```

Cloud clone: `/agent/repos/Rutr.Api`. Sibling frontend: `Rutr.Web`.

## Run (local)

```bash
dotnet restore
dotnet run --urls http://localhost:5188
```

- Swagger: http://localhost:5188/swagger
- Health: http://localhost:5188/health
- SQLite DB file: `rutr-local.db` (migrated + seeded on first start)
- Integrations mock mode: `Integrations:UseMocks=true` (default)
- Optional Postgres: `docker compose --profile postgres up -d` then set `ConnectionStrings__Default`

Copy `secrets.example.json` → `secrets.json` for local overrides (gitignored).

### Demo staff login

- Email: `pm@rutters.demo`
- Password: `rutters-demo`
- Or send header `X-Demo-Auth: pm` instead of a JWT

### Key endpoints

| Method | Path | Auth |
|--------|------|------|
| POST | `/api/v1/auth/login` | none |
| GET | `/api/v1/jobs` | JWT / demo |
| GET | `/api/v1/jobs/{id}` | JWT / demo |
| GET | `/api/v1/approvals` | JWT / demo |
| POST | `/api/v1/approvals/{draftId}/approve` | JWT / demo |
| GET | `/api/v1/portal/{token}` | none |

Demo portal tokens: `keller-maple-demo`, `ellis-prairie-demo`, `nguyen-oak-demo`.

## Docker

```bash
docker build -t rutr-api .
docker run --rm -p 5188:5188 rutr-api
```

## Tests

```bash
dotnet test
```

## Layout

```text
Authentication/ Components/ Configuration/ Controllers/ Data/
Migrations/ Models/ Properties/ Services/ RutrApi.Tests/
Program.cs  Rutr.Api.csproj  Dockerfile  secrets.example.json
```
