# SFA Earnings Utility

A .NET 8 console application that simulates an Employer Approval by publishing an `ApprenticeshipCreatedEvent` to Azure Service Bus via NServiceBus (Short Course and Apprenticeship). It also sets up and manages Change-of-Circs (CoC) approval mappings in `das-api-stub`, so testers can exercise the "pending, requires explicit employer approval" journey against DEMO without a real `das-commitments` deployment. See `C:\code\sfa\projects\earnings\tickets\upcoming\FLP-2092.md` for the design context — this capability started as a standalone spike at `C:\code\sfa\approvals-stub-setup` (now parked, untouched) and was merged in here.

## Solution structure

- `EarningsUtility.UI` — the console app (entry point)
  - `Approvals/` — the "approve a learning" flow (event builder, publisher, interactive prompts)
  - `ApprovalsStub/` — the CoC stub-setup/find/delete flow (`ApprovalsStubClient`, request builder, prompts)
  - `Cli/` — one-shot argument parsing (`CliOptions`)
  - `Console/` — `ConsoleWriter` and the interactive `Menu`
- `EarningsUtility.Types` — shared event/message types
- `EarningsUtility.Tests` — xunit tests for the pure logic (request/URL building, CLI parsing, event building) — console I/O and NServiceBus publishing are not unit tested, verified manually instead

## Running the utility

Build output: `EarningsUtility.UI/bin/Debug/net8.0/EarningsUtility.UI.exe`

### Interactive mode

Run without arguments (or without any recognised one-shot flag combination). You'll be prompted to:
1. Select an environment (arrow keys to move, Enter to select, from `appsettings.json`). The Service Bus connection is opened immediately after this, once per session — not deferred until "Approve" or "Publish a CoC event" is first chosen. The screen then clears and redraws with a header showing the connected environment.
2. Select an action (arrow keys to move, Enter to select), each loop iteration:
   - **Approve a learning (as-is)**: same flow as before — learning type, ULN, Employer Account ID, Employer Type, Apprenticeship ID, UKPRN, Training Code, optional Transfer Sender ID.
   - **Maintain Change-of-Circs approval mappings** (`ApprovalsStub/CocMappingsScreen.cs`): a full-screen two-pane list/detail view. Left pane lists what's registered under the fixed `approvals` prefix (`AppSettings.ApprovalsUrlPrefix`), with a **"+ Create a new response"** row pinned at the top and each mapping tagged with a coloured outcome badge — green "Auto approved", red "Rejected", yellow "Pending", cyan "Mixed" when the mapping's changeTypes carry different statuses (e.g. Price pending, StartDate auto-approved — see below), grey "Unknown" if the mapping's stored JSON doesn't parse as an `ApprovalsResult` at all (e.g. it wasn't created by this tool). Icons are deliberately plain ASCII text badges, not Unicode ✓/✗/– glyphs — those don't render on every console font/code page. The on-screen hint line spells out the key bindings, including that **Delete** removes the highlighted mapping. Up/Down moves the highlight, and the right pane live-updates with the highlighted mapping's parsed fields (Method/Url/Status, one line per Change Type + its own Approval Status, plus an echoed price schedule when Price is one of the changeTypes) plus the raw JSON response body underneath. Enter on the create row runs the `learningKey` + change-kind(s) + **per-kind** outcome prompt — response shape is swagger-aligned (`ApprovalsStub/ApprovalsResult.cs`): a `changes` array with one entry per selected change kind (each carrying its own `changeType`/`approvalStatus`, decided independently — a Name/StartDate change can auto-approve while a same-call Price change still needs the employer), and, only when Price is selected, a `prices` array that's a plain echo of a sample submitted schedule (no per-entry `approvalStatus` — the one `Price` entry in `changes` is the whole schedule's verdict). Enter on an existing mapping opens a **Modify outcome** / **Delete** / **Cancel** sub-menu — modify re-registers the same `learningKey` with newly chosen change kinds and outcomes (upsert, so there's no separate "replace" step). The **Delete** key deletes the highlighted mapping directly, behind a modal confirmation drawn over the current screen (Enter confirms, Escape cancels back to the list) — a quicker path than going through the sub-menu. **Ctrl+Delete** deletes every mapping currently listed (i.e. everything under `ApprovalsUrlPrefix`), behind the same modal-confirmation pattern but naming the count; individual delete failures during the bulk pass are collected and shown in a single error modal rather than aborting partway. Escape at the list returns to the main menu.
   - **Publish a Change-of-Circs event (Approved/Rejected)**: the async half of the flow — publishes a `LearningChangeApprovedEvent`/`LearningChangeRejectedEvent` directly onto the service bus (the connection opened up front, same one "Approve" uses). Prompts for Approved/Rejected (arrow-selected), `learningKey`, Apprenticeship ID, then one or more changed fields — field name is arrow-selected from a list derived from `CocChangeKind` (`TrainingPrice`, `AssessmentPrice`, `StartDate` — the real event's post-mapping field names, `CocChangeKindMapper.ToEventFieldNames`), with a "Custom field name..." option for anything not yet in that list (e.g. `ExpectedEndDate`), followed by old value, new value, optional effective-from date. When the `learningKey` resolves to a mapping this tool set up (picked from the pending list, or a manually-typed key that still matches one), the field picker is narrowed further to only that mapping's currently-pending changeTypes — e.g. if only `StartDate` is pending on it, `TrainingPrice`/`AssessmentPrice` aren't offered — since an employer decision should only plausibly land on what's actually pending; falls back to the full list when no matching mapping is found. Escape on the field picker (or the custom-name prompt) cancels adding that field — cancels the whole event if nothing's been added yet, otherwise just stops adding more (same as answering "n" to "add another?").
3. Escape cancels the current selection (or exits, at the action menu).

CLI one-shot mode keeps the older `coc-setup`/`coc-find`/`coc-delete` actions as separate scriptable commands (see below) — only the interactive menu was consolidated into the single maintain screen.

### One-shot mode (all required args for the chosen action)

**Approve** (unchanged — omitting `--action`, or passing `--action approve`, both work):
```
EarningsUtility.UI.exe --env <name> --uln <uln> --employer <accountId> --employer-type <type> --apprenticeship-id <id> --ukprn <ukprn> --training-code <code> --type <ShortCourse|Apprenticeship> [--transfer-sender <id>]
```

**Set up / modify a CoC approval:**
```
EarningsUtility.UI.exe --action coc-setup --env <name> --learning-key <guid> --outcome <approved|rejected|pending>
```

**Find CoC mappings:**
```
EarningsUtility.UI.exe --action coc-find --env <name>
```
Always scoped to `ApprovalsUrlPrefix` (default `approvals`) — no filter argument.

**Delete CoC mappings:**
```
EarningsUtility.UI.exe --action coc-delete --env <name> [--all]
```
Same fixed `ApprovalsUrlPrefix` scope. If more than one mapping matches, it refuses to delete anything and lists the matches — pass `--all` to confirm a bulk delete.

**Publish a CoC event (Approved/Rejected):**
```
EarningsUtility.UI.exe --action coc-event --env <name> --learning-key <guid> --apprenticeship-id <id> --event-type <approved|rejected> --field <name> [--old <value>] [--new <value>] [--effective-from <date>]
```
One-shot mode only supports a single changed field per call — use interactive mode to publish an event with multiple changed fields.

Exit codes: `0` success, `1` failure (unknown environment, missing config, invalid/incomplete args, HTTP failure, or an ambiguous delete without `--all`).

## Configuration

`EarningsUtility.UI/appsettings.json` (not committed — see `appsettings.example.json` for the shape):
```json
{
  "Environments": { "demo": "das-demo-shared-ns.servicebus.windows.net" },
  "ApprovalsStubBaseUrl": { "demo": "https://demo-stub.apprenticeships.education.gov.uk" }
}
```
- `Environments` — Azure Service Bus namespace per environment, used by the "approve" and "CoC event" flows. Authentication uses `DefaultAzureCredential` — no connection strings.
- `ApprovalsStubBaseUrl` — `das-api-stub`'s **management API** base URL per environment (not the data-plane `{env}-stub-api...` host — this app only ever calls the management API's `/api-stub/*` routes). Keyed by the same environment names as `Environments`.
- `ApprovalsUrlPrefix` — the fixed URL fragment CoC find/delete scope themselves to (default `approvals`); this tool only ever sets up and tears down `approvals/*` mappings, so there's no per-run filter to type.
- **`local` has no `ApprovalsStubBaseUrl` entry, deliberately** — there's no deployed stub for it, only an in-process one inside `das-api-stub`'s own test suite. Selecting a CoC action against `local` fails with a clear message rather than an HTTP exception.
- The AT environment's `das-api-stub` is known to be multi-instance and flaky (a mapping can inconsistently return `404` depending on which backend instance handles the request) — DEMO is single-instance and reliable. Prefer DEMO for CoC testing.
- DEMO's data-plane WireMock doesn't reload from table storage on its own after a redeploy — if CoC calls are unexpectedly 404ing across the board, someone may need to hit `GET {ApprovalsStubBaseUrl}/api-stub/refresh` to resync it.

## What the "approve" event contains

The published `ApprenticeshipCreatedEvent` has mostly hardcoded values (name, dates, provider etc — see `Approvals/ApprovalEventBuilder.cs`). The fields driven by user input are:
- `Uln`, `AccountId` (Employer Account ID), `ApprenticeshipEmployerTypeOnApproval`, `ApprenticeshipId`, `LearningType` (`ApprenticeshipUnit` for Short Course, `Apprenticeship` for Apprenticeship), `ProviderId` (UKPRN), `TrainingCode`, `TransferSenderId` (optional)

## What the CoC approval setup does

Registers a mapping in `das-api-stub` so a subsequent real `PUT approvals/{learningKey}` call (from `das-apprenticeships`/`das-apim-endpoints`, once that outbound call exists — see FLP-2092) comes back with a chosen outcome per changeType — **automatic approval** (`autoApproved`), **automatic rejection** (`autoRejected`), or **pending** (`employerApprovalRequired`) — instead of always instantly approved. Response shape and casing (camelCase; `employerApprovalRequired`, not the live implementation's current `EmployerApprovalRequested`) target the swagger-aligned contract confirmed by the other team's lead dev on 2026-09-25 — see [approvals-integration.md](../../projects/earnings/tickets/upcoming/approvals-integration.md) ("Swagger-aligned CoC payload examples"), not today's live `das-commitments` implementation. Only covers the synchronous "submit change, get verdict" leg.

## What the CoC event publisher does

Covers the async "employer later approves/rejects a pending change" leg, which isn't a second HTTP call from Commitments back to us — it's Commitments publishing a `LearningChangeApprovedEvent`/`LearningChangeRejectedEvent` on the service bus (`SFA.DAS.CommitmentsV2.Messages.Events.LearningChangeEvents.cs`, shape mirrored locally in `EarningsUtility.Types/LearningChangeEvents.cs` the same way `ApprenticeshipCreatedEvent` is). `das-api-stub` can't fake this half, so this action publishes the event directly, letting a Learning-side consumer be exercised without a real Commitments deployment.
