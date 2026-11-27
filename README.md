# Rutr.Api

ASP.NET Core 8 Web API for Project RUTR (expanded Connect MVP). Mosaic single project; demo data seeded in EF.

**GitHub:** https://github.com/richiethie/Rutr.Api (`main`)

## Run

```bash
cd C:\Users\rthiesfeldt\practice\Rutr.Api
dotnet restore
dotnet run --urls http://localhost:5188
```

- Swagger: http://localhost:5188/swagger  
- OpenAPI: http://localhost:5188/swagger/v1/swagger.json (`openapi/openapi.json` for Web `gen:api`)  
- Health: http://localhost:5188/health  
- CORS: `http://localhost:3317`  
- Demo: `pm@rutters.demo` / `rutters-demo` (also `admin@` / `sales@`) or `X-Demo-Auth: pm`

## In-scope CRUD (staff)

| Resource | Base path |
|----------|-----------|
| Organization + members | `/api/v1/organizations/current` |
| Contacts | `/api/v1/contacts` |
| Properties | `/api/v1/properties` |
| Jobs (+ status) | `/api/v1/jobs` |
| Notes | `/api/v1/notes` |
| Tasks | `/api/v1/tasks` |
| Evidence (+ soft-delete, visibility) | `/api/v1/evidence` |
| Drafts | `/api/v1/drafts` |
| Approvals queue | `/api/v1/approvals` |
| Milestones | `/api/v1/milestones` |
| Consents | `/api/v1/consents` |
| Integrations mock connect/disconnect | `/api/v1/integrations/connections/{provider}/connect\|disconnect` |
| Portal (read-only) | `/api/v1/portal/{token}` |

Portal tokens: `keller-maple-demo`, `ellis-prairie-demo`, `nguyen-oak-demo`.

## Tests

```bash
dotnet test
```
