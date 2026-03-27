# SFA Earnings Utility

A .NET 8 console application that simulates a Short Course Approval by publishing an `ApprenticeshipCreatedEvent` to Azure Service Bus via NServiceBus.

## Solution structure

- `EarningsUtility.UI` — the console app (entry point)
- `EarningsUtility.Types` — shared event/message types

## Running the utility

Build output: `EarningsUtility.UI/bin/Debug/net8.0/EarningsUtility.UI.exe`

### Interactive mode

Run without arguments. You will be prompted to:
1. Select an environment (numbered list from `appsettings.json`)
2. Enter the learner's ULN
3. Enter the approving Employer's Account ID (numeric)

Press Escape to exit.

### One-shot mode (all three args required)

```
EarningsUtility.UI.exe --env <name> --uln <uln> --employer <accountId>
```

Example:
```
EarningsUtility.UI.exe --env demo --uln 1234567890 --employer 12345
```

Connects, sends the approval, and exits. If `--env` doesn't match a configured environment, it prints the available names and exits with code 1.

## Configuration

Environments are defined in `EarningsUtility.UI/appsettings.json` (not committed — read this file to find available environment names). The key is the display name used with `--env`.

Authentication uses `DefaultAzureCredential` — no connection strings.

## What the event contains

The published `ApprenticeshipCreatedEvent` has mostly hardcoded values (name, dates, provider, training code etc). The only fields driven by user input are:
- `Uln` — from `--uln` or interactive prompt
- `AccountId` — from `--employer` or interactive prompt
