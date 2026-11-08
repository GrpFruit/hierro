# Hierro roadmap

This file records what Hierro is for, the decisions we've made and why, and the order we'll build things in.
Decided on 2026-11-07. When a decision changes, edit this file in the same PR that changes the code.

## Vision

A strength training log for people who train at home, with a built-in coach. It knows the equipment you own,
tells you what to lift today, and lets you log an ordinary set with one tap.

Hierro is a **learning project first** and a possible business later.

- **Audience, in order:** me and friends → the public → coaches and their clients
- **What makes it different, in priority order:**
  1. **Coaching.** Progression rules decide today's weight and reps.
  2. **Equipment awareness.** Every suggestion is something you can actually load.
  3. **Fast logging.** Fewer taps per set than other apps.

## How we work

- **The owner writes the code.** Claude writes a brief for each task, reviews the PR and explains.
  When something is completely new, we pair: Claude explains, the owner types.
- Every change goes on a branch and through a pull request. Nothing is pushed straight to `main`.
- GitHub Actions runs `dotnet test`, `npm run lint` and `npm run build` on every PR (arrives in v0.1).
- Work is tracked as GitHub Issues grouped into one Milestone per release. Each issue should fit in one PR.

## Decisions

| Area | Decision | Why |
| --- | --- | --- |
| Stack | Keep Next.js 16 + ASP.NET Core (.NET 10). Move SQLite to **Postgres**. | It already works, and Postgres is what we'd host. |
| Schema changes | **EF Core migrations** instead of `EnsureCreated` | Switch before any data is worth keeping. |
| Architecture | **Modular monolith**: one API with clear modules, and a background worker later | Microservices cost too much operational work for a solo project. Pieces can be split out later if needed. |
| Platform | Responsive web app; PWA later | Works on every phone with no app store. Go native only if we hit a real limit. |
| Sign-in | ASP.NET Core Identity with cookie auth through the Next proxy | Free, the data stays in our database, and it's worth learning. Google sign-in comes before public launch. |
| Sign-up | Invite codes. The first account is Admin. | Keeps the friends phase closed. Coaches will reuse the same mechanism to invite clients. |
| Email | Mailpit in development. No confirmation emails until there's a prod environment. | Build the real flows without sending real mail. |
| Roles | Additive roles: `Athlete` (everyone), `Coach`, `Admin`. Programs have `OwnerId` and an optional `AssignedToId`. | Coaches train too. Putting these in the schema now avoids a painful migration later. The `CoachClient` link table comes in the coach phase. |
| Programs | Built-in templates you copy and edit → program builder → one-time swaps → permanent swaps | Coaches will use the same builder later. |
| Exercises | A curated catalog imported from a dataset, plus user-created custom exercises. About 150 are marked **featured**. | Equipment filtering hides what you can't do, and the featured list keeps the builder manageable. |
| Equipment | Two layers. **What you own**: a checklist using the dataset's vocabulary, plus rack, adjustable bench and dip station. **What you can load**: plates and their unit, bar weights, dumbbells, kettlebells. | Lets the coach round to weights you can actually make, and lets the builder hide exercises you can't do. |
| Progression | Each exercise gets a default inferred from its data (barbell compound → linear, dumbbell or isolation → double progression, bodyweight → level-ups). A program slot can override it. | Thousands of exercises can't be tagged by hand. |
| Units | Chosen per user. Every set is stored in the unit it was entered in, and converted only for display. Plates carry their own unit. | No conversion drift, and mixed-unit home gyms work. |
| Privacy | Data export (JSON and CSV), real account deletion, coach access only with the client's consent, and a privacy policy before public launch | Workout and body data is health-adjacent. |
| Secrets | `dotnet user-secrets` in development, and a git-ignored `.env` with a committed `.env.example` | The repo is public. |
| Business model | Free for individuals; coaches pay per client. No billing code until the coach phase. | The coaching engine is the hook, so it shouldn't be behind a paywall. |

### Exercise dataset

The catalog comes from the *Functional Fitness Exercise Database v2.9* (3,242 exercises with muscles, equipment,
movement patterns and about 2,964 YouTube demo links).

- **Its license is personal use only.** We use it during the friends phase only. Before any public launch, either get
  written permission from the author (strengthtoovercome.com) or switch to an openly licensed dataset.
- **It never gets committed.** This repo is public. The `.xlsx` file lives in the git-ignored `data/` folder or another
  configured path. CI uses a small file of made-up exercises instead.
- Every imported row records its `Source`, so the whole dataset can be swapped out later.

## Environments

| Now | Later |
| --- | --- |
| A single **dev** environment on the owner's PC: `dotnet run` + `npm run dev`, and Postgres in Docker Compose | k3s cluster with `develop`, `stage` and `prod` namespaces |
| **All data is disposable.** If you log real sessions, copy the database file somewhere safe. | Nightly `pg_dump` backups stored off the machine |
| Reachable on home Wi-Fi over plain HTTP. No PWA install and no screen wake lock (both need HTTPS). | HTTPS through Cloudflare Tunnel; Tailscale for admin access |
| CI on GitHub Actions only | Jenkins on-site for CD (deploying to k3s) |

## Release plan

### Quick wins (on the current code)
Small, visible features that teach the codebase one layer at a time.

0. **Walkthrough.** Follow a request from start to finish, then write `docs/ARCHITECTURE.md` as your first PR.
1. **Phone access on home Wi-Fi**
2. **Rest timer.** Store rest as structured data, add a ✓ button to each set (it fills in the suggested numbers and
   starts the timer), and add a timer bar based on timestamps.
3. **Bodyweight tracking.** Your first full-stack feature: entity, endpoints, form and chart.

### v0.1 Foundations
*Done when a friend can sign up with an invite and log a workout on the built-in plans.*
Postgres + migrations, GitHub Actions CI, Identity auth, roles, invite codes, Mailpit flows, workouts belonging to users,
built-in plans moved into the database (with `OwnerId`/`AssignedToId`), the exercise importer, data export,
account deletion.

### v0.2 Your gym
Equipment profile, filtering exercises by available equipment, coaching rounded to weights you can load,
and a mobile layout pass.

### v0.3 Programs
Copy a template, the program builder, one-time exercise swaps.

### v0.4 Progress
Estimated 1RM and PR detection, coming back after time off, permanent swaps, more history.

### Coach roadmap after v0.4
RPE/RIR input, then planned deload weeks.

## Deferred (decided, not forgotten)

- **Dataset license**: get it sorted before going public.
- **Offline logging and sync**: as a PWA over HTTPS.
- **Prod environment and backups**: once friends use it outside the house.
- **k3s, Jenkins, Cloudflare Tunnel**: when the owner is ready for that work.
- **Body measurements** (soon), **progress photos** (much later).
- **Cardio**: simple logging first (activity, duration, distance, RPE, heart rate). Imports later (Strava, Garmin),
  each one a module in the monolith. Apple Health needs a native iOS app, because it has no web API.
- **Coach phase**: the coach UI and the `CoachClient` link table (pending, active or revoked).
- **Billing**: per coach client, added in the coach phase.
- **Name**: "Hierro" is still a placeholder.
