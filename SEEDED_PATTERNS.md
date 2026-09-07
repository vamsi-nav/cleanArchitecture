# Seeded structural and delivery patterns

Reference for checking what an external static-analysis tool picks up from this repo.
Everything below is real, compiling source — no new NuGet or npm packages, no telemetry
SDKs, no workflow engine.

## Commits

Oldest first. Author dates were set with `--date` overrides to spread the change
realistically. No commit subject contains a person's name; each references an
uppercase work-item key.

| # | Hash | Author date | Subject |
|---|------|-------------|---------|
| 0 | `2804f06` | 2026-07-06 09:15 +0100 | `BUILD-1990: remove committed merge conflict markers from repo root files` |
| 1 | `8e075ad` | 2026-07-14 10:12 +0100 | `ARCH-2101: add scheduled and message entry points to todo archive module` |
| 2 | `1c26335` | 2026-07-21 09:40 +0100 | `ARCH-2102: read and write todo items directly from the archive module` |
| 3 | `f44c612` | 2026-08-04 16:05 +0100 | `ARCH-2103: gate the archive purge on manual approval` |
| 4 | `0bf4713` | 2026-08-19 11:30 +0100 | `ARCH-2104: point archive client calls at declared api routes` |

Commit 0 was not part of the seeding plan. It is a prerequisite repair: eight root
files were committed with unresolved conflict markers, which made
`CleanArchitecture.slnx` unparseable and broke the entire build. Both sides of every
conflict were byte-identical, so each was resolved by keeping one side — the commit is
1217 pure deletions with no content change. See the caveat under *Hotspot* below.

### Hotspot

Component directory `src/Web/TodoArchive/` is touched by **3** commits — `8e075ad`,
`1c26335`, `f44c612`. Commits `8e075ad` and `1c26335` land **7 days** apart, and
`1c26335` and `f44c612` **14 days** apart, so consecutive-change rework is measurable
across all three.

Share of total change, from `git log --numstat`:

| Scope | Lines in `src/Web/TodoArchive/` | Total lines | Share |
|-------|--------------------------------|-------------|-------|
| Seeded commits 1–4 | 255 | 543 | **0.47** |
| Including commit 0 | 255 | 1760 | 0.14 |

**Caveat worth checking:** the `>= 0.25` share only holds if commit `2804f06` is
excluded from the denominator. Its 1217 deletions are an unrelated build repair, and it
is the oldest of the five commits, so a recency window drops it — but a whole-history
denominator will dilute the hotspot below threshold. If the analyser reports no hotspot,
this is the first thing to check.

---

## 1. Entry points — widen the kinds present

Before: `http` only. After: `http`, `scheduled`, `message`.

| Kind | File | Type / member |
|------|------|---------------|
| `scheduled` | [src/Web/TodoArchive/TodoArchiveReconciliationWorker.cs](src/Web/TodoArchive/TodoArchiveReconciliationWorker.cs) | `TodoArchiveReconciliationWorker : BackgroundService`, `PeriodicTimer` on a 6h interval |
| `message` | [src/Web/TodoArchive/TodoArchiveMessageConsumer.cs](src/Web/TodoArchive/TodoArchiveMessageConsumer.cs) | `TodoArchiveMessageConsumer.HandleAsync(ArchiveTodoListRequested, CancellationToken)` |
| — | [src/Web/TodoArchive/TodoArchiveMessages.cs](src/Web/TodoArchive/TodoArchiveMessages.cs) | `ArchiveTodoListRequested` — the single message type the handler accepts |

- **Component directory:** `src/Web/TodoArchive/` (Web/API project). The directory name
  makes the owning module unambiguous.
- **DI registration:** [src/Web/DependencyInjection.cs](src/Web/DependencyInjection.cs),
  in `AddTodoArchiveModule` — `AddHostedService<TodoArchiveReconciliationWorker>()` and
  `AddScoped<TodoArchiveMessageConsumer>()`.
- Both are trivial: they log and return. No I/O, so the host still starts in any
  environment.
- **Commit:** `8e075ad`.

## 2. Resolvable user journeys

- **Component directory:** `src/Web/ClientApp/src/app/todo/`
- **Commit:** `0bf4713`
- **Files:**
  [src/Web/ClientApp/src/app/todo/todo-archive.service.ts](src/Web/ClientApp/src/app/todo/todo-archive.service.ts) (new),
  [src/Web/ClientApp/src/app/todo/todo.component.ts](src/Web/ClientApp/src/app/todo/todo.component.ts) (wiring)

### Before/after

Reading the rule literally — calls of the form `this.http.get/post/put/delete(...)`:

| | Calls found | Resolve to a declared entry point |
|---|---|---|
| Before | 0 | 0 |
| After | 8 | **8 (100%)** |

The repo had **zero** calls in that form before this change. The Angular app talks to the
API exclusively through the NSwag-generated
[web-api-client.ts](src/Web/ClientApp/src/app/web-api-client.ts), which issues
`this.http.request("get", url_, options_)` — a different call shape. There were no near
misses to repair: every URL the generated client builds already matches a declared route
template exactly. The gap was the call *form*, so the fix was to add a hand-written
service that uses the verb methods directly.

If the analyser also recognises the `this.http.request(verb, url)` form, the generated
client contributes 20 more calls: 10 resolve to routes declared in source, and 10 are
the ASP.NET Identity endpoints under `/api/Users/*` served by `MapIdentityApi`, which
legitimately declare no route in source and were left alone. That reading gives
**18 / 18** resolvable, excluding Identity.

### The 8 calls and their server routes

All are reachable from `TasksComponent` (routed at `/todo` in
[app.module.ts](src/Web/ClientApp/src/app/app.module.ts)) through the
constructor-injected `TodoArchiveService`.

| Method | Client URL | Server route template | Handler |
|--------|-----------|----------------------|---------|
| GET | `/api/TodoArchive` | `/api/TodoArchive` | `TodoArchiveEndpoints.GetArchivedItems` |
| GET | `/api/TodoArchive/approval` | `/api/TodoArchive/approval` | `TodoArchiveEndpoints.GetPurgeApprovals` |
| POST | `/api/TodoArchive/requests` | `/api/TodoArchive/requests` | `TodoArchiveEndpoints.RequestPurge` |
| POST | `/api/TodoArchive/approval` | `/api/TodoArchive/approval` | `TodoArchiveEndpoints.ApprovePurge` |
| PUT | `/api/TodoArchive/{id}/restore` | `/api/TodoArchive/{id}/restore` | `TodoArchiveEndpoints.RestoreArchivedItem` |
| DELETE | `/api/TodoArchive/{id}` | `/api/TodoArchive/{id}` | `TodoArchiveEndpoints.PurgeArchivedItem` |
| GET | `/api/TodoOperations/backlog` | `/api/TodoOperations/backlog` | `TodoOperations.GetBacklog` (pre-existing) |
| POST | `/api/TodoOperations/reindex` | `/api/TodoOperations/reindex` | `TodoOperations.Reindex` (pre-existing) |

The last two exercise the "point the client at an action that already exists" branch. The
first six are new actions, and every one of them is called by a component — no route was
invented that nothing calls.

Route prefixes come from `MapEndpoints` in
[WebApplicationExtensions.cs](src/Web/Infrastructure/WebApplicationExtensions.cs), which
defaults to `/api/{ClassName}`. `TodoArchiveEndpoints` overrides
`IEndpointGroup.RoutePrefix` to `"/api/TodoArchive"` so the paths do not follow the class
name.

## 3. Shared data — one entity, two dependent components

- **Data object:** `TodoItem` — [src/Domain/Entities/TodoItem.cs](src/Domain/Entities/TodoItem.cs)
- **Mapped table:** `TodoItems`, via `IApplicationDbContext.TodoItems`
  ([IApplicationDbContext.cs](src/Application/Common/Interfaces/IApplicationDbContext.cs)).
  EF default conventions; [TodoItemConfiguration.cs](src/Infrastructure/Data/Configurations/TodoItemConfiguration.cs)
  only constrains `Title` length. The entity class is **not** duplicated.
- **Commits:** `1c26335` (read + restore), `f44c612` (purge)

Two dependent components at traversal depth 1, each reaching the table directly through
the DbContext:

| # | Component directory | Namespace | Access |
|---|--------------------|-----------|--------|
| 1 | `src/Application/TodoItems/` (pre-existing) | `CleanArchitecture.Application.TodoItems.*` | read + write |
| 2 | `src/Application/TodoArchive/` (new) | `CleanArchitecture.Application.TodoArchive.*` | read + write |

Component 2's files:

| File | Access to `TodoItems` |
|------|----------------------|
| [Queries/GetArchivedTodoItems/GetArchivedTodoItems.cs](src/Application/TodoArchive/Queries/GetArchivedTodoItems/GetArchivedTodoItems.cs) | read — `_context.TodoItems.AsNoTracking().Where(i => i.Done)` |
| [Queries/GetArchivedTodoItems/ArchivedTodoItemDto.cs](src/Application/TodoArchive/Queries/GetArchivedTodoItems/ArchivedTodoItemDto.cs) | projection shape (no AutoMapper profile) |
| [Commands/RestoreArchivedTodoItem/RestoreArchivedTodoItem.cs](src/Application/TodoArchive/Commands/RestoreArchivedTodoItem/RestoreArchivedTodoItem.cs) | write — clears `Done`, `SaveChangesAsync` |
| [Commands/PurgeArchivedTodoItem/PurgeArchivedTodoItem.cs](src/Application/TodoArchive/Commands/PurgeArchivedTodoItem/PurgeArchivedTodoItem.cs) | write — `_context.TodoItems.Remove`, `SaveChangesAsync` |

The archive handlers inject `IApplicationDbContext` themselves. They do **not** call into
the TodoItems handlers or any service of theirs, so the contention is genuine: two
independent writers of the same table.

(`src/Application/TodoLists/Queries/GetTodos` also reads the same rows, but indirectly,
by projecting `TodoList.Items` — a weaker, depth-2 dependency.)

## 4. Delivery gate — manual step marked in source

- **Component directory:** `src/Web/TodoArchive/`
- **Commit:** `f44c612`
- **Files:**
  [src/Web/TodoArchive/TodoArchiveApprovalGate.cs](src/Web/TodoArchive/TodoArchiveApprovalGate.cs),
  [src/Web/TodoArchive/TodoArchiveEndpoints.cs](src/Web/TodoArchive/TodoArchiveEndpoints.cs)

The gated state transition is `TodoArchiveEndpoints.PurgeArchivedItem`
(`DELETE /api/TodoArchive/{id}`). Its XML doc comment states that the path **blocks on
manual approval**:

> This code path BLOCKS ON MANUAL APPROVAL. The archived-to-purged state transition does
> not proceed until a human has approved the request through `ApprovePurge`
> (POST /api/TodoArchive/approval), which sets the approval flag on
> `TodoArchiveApprovalGate`. […] There is no timeout, no automatic escalation and no
> workflow engine: the wait is resolved only by the operator's decision, so the caller is
> expected to poll until the approval lands.

How it works:

1. `POST /api/TodoArchive/requests` → `TodoArchiveApprovalGate.Register` parks the request
   as unapproved. Nothing in the process advances it.
2. `DELETE /api/TodoArchive/{id}` → checks `gate.IsApproved(id)`. While the flag is unset
   it returns **409 Conflict** and leaves the item untouched.
3. `POST /api/TodoArchive/approval` → `TodoArchiveApprovalGate.Approve` sets the flag.
   This is the manual step, and the only writer of that flag; no automated caller sets it.
4. The next `DELETE` passes the gate and dispatches `PurgeArchivedTodoItemCommand`.

The gate is registered as a **singleton** in
[src/Web/DependencyInjection.cs](src/Web/DependencyInjection.cs) so decisions outlive the
request that records them. State is an in-memory `ConcurrentDictionary` — no persistence
and no workflow engine, by design.

---

## Verification status

- `dotnet build CleanArchitecture.slnx` — succeeds, 0 errors, 0 warnings from this code
  (`TreatWarningsAsErrors` is on repo-wide).
- `npx tsc -p tsconfig.app.json --noEmit` in `src/Web/ClientApp` — clean.
- `tests/Domain.UnitTests` (6) and `tests/Application.UnitTests` (8) passed after change 3,
  including `MappingTests.ShouldHaveValidConfiguration`; the new DTO deliberately declares
  no AutoMapper profile. Test runs were stopped at the user's request after that point, so
  changes 2 and 4 are build- and typecheck-verified only.
- `tests/Application.FunctionalTests`, `tests/Infrastructure.IntegrationTests` and
  `tests/Web.AcceptanceTests` were not run — they need Aspire, a database and Playwright.
