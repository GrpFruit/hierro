# Hierro

*Hierro* is Spanish for "iron". It's a home-gym strength training log with built-in progression coaching,
grown out of the **Home Gym Strength Split** artifact.

- **frontend/**: Next.js 16 (App Router, TypeScript, React 19)
- **backend/**: ASP.NET Core minimal API on .NET 10, EF Core + SQLite

Where it's headed and why: [docs/ROADMAP.md](docs/ROADMAP.md).

## Run it

Two terminals:

```powershell
# 1. API on http://localhost:5115
cd backend/Hierro.Api
dotnet run --launch-profile http

# 2. Web app on http://localhost:3000
cd frontend
npm install   # first time only
npm run dev
```

Open http://localhost:3000. The Next dev server proxies `/api/*` to the API (see `frontend/next.config.ts`),
so the browser never calls the API cross-origin. Point it somewhere else with `HIERRO_API_URL`.

The API creates `backend/Hierro.Api/hierro.db` on first run. Delete that file to start over.

## Tests

```powershell
cd backend
dotnet test            # coaching rules + API round trips
cd ../frontend
npm run lint
npm run build
```

## How it fits together

| Piece | Where |
| --- | --- |
| Plans, days, exercises, demo videos | `backend/Hierro.Api/Data/plans.json` (embedded resource) |
| Progression rules (add weight, hold, 10% reset, bodyweight level-ups), plate math, warm-ups | `backend/Hierro.Api/Domain/Coach.cs` |
| Storage (one row per day per date; sets stored as JSON) | `backend/Hierro.Api/Data/HierroDb.cs` |
| HTTP endpoints | `backend/Hierro.Api/Endpoints/WorkoutEndpoints.cs` |
| Session sheet, progress charts, history | `frontend/src/components/` |
| "Leave a weight blank to reuse the one above" logic | `frontend/src/lib/sheet.ts` |

### API

| Method | Path | |
| --- | --- | --- |
| GET | `/api/plans` | Both plans with their days and exercises |
| GET | `/api/plans/{planId}/days/{dayId}/sheet?date=YYYY-MM-DD` | A day's exercises with coaching based on sessions before that date, plus anything already saved for that date |
| GET | `/api/sessions` | Logged sessions, newest first (summaries) |
| PUT | `/api/sessions/{date}/{dayId}` | Save or replace a session. Body: `{ planId, entries: [{ name, sets: [{ w, r }], note }] }` |
| DELETE | `/api/sessions/{id}` | Delete a session (`id` is `{date}_{dayId}`) |
| GET | `/api/progress` | Heaviest set per session for each main lift |
| GET | `/health` | Liveness |

In development, the OpenAPI document is at `/openapi/v1.json`.

## Placeholder decisions to revisit

- **Single user, no auth.** Anyone who can reach the API shares one log. Add authentication before hosting it.
- **`EnsureCreated` instead of migrations.** Fine while the schema is still changing. Switch to EF Core migrations before there's data you want to keep.
- **Plans live in a JSON file.** Editing a plan means editing `plans.json`. Moving plans into the database is the next step if you want to edit them in the app.
- **Name.** "Hierro" is a placeholder.
