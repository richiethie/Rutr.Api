# Rutr.Api

ASP.NET Core 8 Web API for Project RUTR (Connect / Reporting pilot). Mosaic-style single project: controllers stay thin; domain + mock integrations live under `Services/`.

**GitHub:** https://github.com/richiethie/Rutr.Api (`main`)

## Intended local home

```text
~/practice/Rutr.Api
C:\Users\rthiesfeldt\practice\Rutr.Api
```

Sibling frontend: `Rutr.Web` on port **3317**.

## Run (local)

```bash
dotnet restore
dotnet run --urls http://localhost:5188
```

(Or F5 the `http` launch profile — same port.)

- Swagger UI: http://localhost:5188/swagger
- OpenAPI JSON: http://localhost:5188/swagger/v1/swagger.json (committed `openapi/openapi.json` for `Rutr.Web` `gen:api`)
- Health: http://localhost:5188/health
- CORS allows `http://localhost:3317`
- SQLite: `rutr-local.db` (migrate + seed on start)
- Integrations mock mode: `Integrations:UseMocks=true` (default)
- Optional Postgres: `docker compose --profile postgres up -d` then set `ConnectionStrings__Default`

Copy `secrets.example.json` → `secrets.json` for local overrides (gitignored). JWT `SigningKey` must be **≥ 32 characters**.

### Demo staff login

- Email: `pm@rutters.demo`
- Password: `rutters-demo`
- Or header `X-Demo-Auth: pm` instead of a JWT

### Connect flow (MVP)

1. `POST /api/v1/integrations/sync` — mock JobNimbus upserts jobs, CompanyCam adds evidence, pending digests are drafted, portal tokens ensured  
2. `GET /api/v1/approvals` — PM queue  
3. `POST /api/v1/approvals/{id}/approve` — marks related photos customer-visible  
4. `POST /api/v1/approvals/{id}/send` — mock SMS/email digest (audit: draft → approve → send)  
5. Homeowner: `GET /api/v1/portal/{token}`

### Key endpoints

| Method | Path | Auth |
|--------|------|------|
| POST | `/api/v1/auth/login` | none |
| GET | `/api/v1/jobs` | JWT / demo |
| GET | `/api/v1/jobs/{id}` | JWT / demo |
| GET | `/api/v1/jobs/{id}/audit` | JWT / demo |
| PATCH | `/api/v1/jobs/{id}/photos/{photoId}/visibility` | JWT / demo |
| GET | `/api/v1/approvals` | JWT / demo |
| POST | `/api/v1/approvals/{draftId}/approve` | JWT / demo |
| POST | `/api/v1/approvals/{draftId}/reject` | JWT / demo |
| POST | `/api/v1/approvals/{draftId}/send` | JWT / demo |
| GET | `/api/v1/integrations/status` | JWT / demo |
| POST | `/api/v1/integrations/sync` | JWT / demo |
| GET | `/api/v1/portal/{token}` | none |

Seed portal tokens: `keller-maple-demo`, `ellis-prairie-demo`, `nguyen-oak-demo`. Sync may add others (e.g. new Birch job).

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
