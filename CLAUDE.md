# SFA Earnings Utility

A .NET 8 console application that simulates an Employer Approval by publishing an `ApprenticeshipCreatedEvent` to Azure Service Bus via NServiceBus. Supports both Short Course and Apprenticeship approval types.

## Solution structure

- `EarningsUtility.UI` — the console app (entry point)
- `EarningsUtility.Types` — shared event/message types

## Running the utility

Build output: `EarningsUtility.UI/bin/Debug/net8.0/EarningsUtility.UI.exe`

### Interactive mode

Run without arguments. You will be prompted to:
1. Select an environment (numbered list from `appsettings.json`)
2. Select approval type: `1` = Short Course, `2` = Apprenticeship
3. Enter the learner's ULN
4. Enter the approving Employer's Account ID (numeric)
5. Enter Employer Type (e.g. `Levy`/`NonLevy`)
6. Enter the Apprenticeship ID (numeric)
7. Enter Transfer Sender ID (optional)

Press Escape to exit.

### One-shot mode (all args required)

```
EarningsUtility.UI.exe --env <name> --uln <uln> --employer <accountId> --employer-type <type> --apprenticeship-id <id> --type <ShortCourse|Apprenticeship>
```

Example (Short Course):
```
EarningsUtility.UI.exe --env demo --uln 1234567890 --employer 12345 --employer-type Levy --apprenticeship-id 99 --type ShortCourse
```

Example (Apprenticeship):
```
EarningsUtility.UI.exe --env demo --uln 1234567890 --employer 12345 --employer-type Levy --apprenticeship-id 99 --type Apprenticeship
```

Optional: `--transfer-sender <id>`

Connects, sends the approval, and exits. If `--env` doesn't match a configured environment, it prints the available names and exits with code 1.

## Configuration

Environments are defined in `EarningsUtility.UI/appsettings.json` (not committed — read this file to find available environment names). The key is the display name used with `--env`.

Authentication uses `DefaultAzureCredential` — no connection strings.

## What the event contains

The published `ApprenticeshipCreatedEvent` has mostly hardcoded values (name, dates, provider, training code etc). The fields driven by user input are:
- `Uln` — from `--uln` or interactive prompt
- `AccountId` — from `--employer` or interactive prompt
- `ApprenticeshipEmployerTypeOnApproval` — from `--employer-type` or interactive prompt
- `ApprenticeshipId` — from `--apprenticeship-id` or interactive prompt
- `LearningType` — `ApprenticeshipUnit` for Short Course, `Apprenticeship` for Apprenticeship
- `TransferSenderId` — from `--transfer-sender` or interactive prompt (optional)
