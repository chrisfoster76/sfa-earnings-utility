using SFA.DAS.CommitmentsV2.Messages.Events;

namespace EarningsUtility.UI.ApprovalsStub;

public static class CocEventPrompts
{
    private static readonly (string Label, bool Value)[] Outcomes =
    {
        ("Approved (LearningChangeApprovedEvent)", true),
        ("Rejected (LearningChangeRejectedEvent)", false)
    };

    /// <summary>Returns null if the flow was cancelled (Escape on the learningKey or outcome,
    /// or nothing entered for either prices or fields).</summary>
    public static async Task<CocEventRequest?> CollectAsync(ApprovalsStubClient stubClient, string urlPrefix)
    {
        var mappings = await stubClient.FindMappings(urlPrefix);

        var learningKey = SelectLearningKey(mappings);
        if (learningKey == null)
            return null;

        ConsoleWriter.WriteColor($"Learning Key: {learningKey}", ConsoleColor.Cyan);

        var approved = Menu.SelectOption("Select the event to publish:", Outcomes);
        if (approved == null)
            return null;

        long apprenticeshipId;
        while (true)
        {
            ConsoleWriter.WriteColor("Enter the Apprenticeship ID (numeric): ", ConsoleColor.White, newLine: false);
            if (long.TryParse(Console.ReadLine()?.Trim(), out apprenticeshipId))
                break;
            ConsoleWriter.WriteColor("Invalid number, please try again.", ConsoleColor.Red);
        }

        // Null means no matching mapping with anything pending, so nothing is narrowed.
        var pendingKinds = FindPendingKinds(mappings, learningKey.Value);

        var prices = CollectPrices(pendingKinds);

        var fieldNames = pendingKinds?.SelectMany(k => k.ToEventFieldNames()).ToList();
        var offerFields = fieldNames == null || fieldNames.Count > 0;

        var changes = new List<LearningChangeEvent.Change>();
        while (offerFields && (prices == null || changes.Count > 0 || AskYes("Add a changed field as well? y/N: ")))
        {
            var field = SelectFieldName(fieldNames);
            if (field == null)
                break;

            ConsoleWriter.WriteColor("Old value (optional, press Enter to skip): ", ConsoleColor.White, newLine: false);
            var oldValue = Console.ReadLine()?.Trim();

            ConsoleWriter.WriteColor("New value (optional, press Enter to skip): ", ConsoleColor.White, newLine: false);
            var newValue = Console.ReadLine()?.Trim();

            // Re-entering a field replaces its earlier entry
            changes.RemoveAll(c => c.ChangeType == field);
            changes.Add(new LearningChangeEvent.Change
            {
                ChangeType = field,
                Data = new LearningChangeEvent.ChangeData
                {
                    Old = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue,
                    New = string.IsNullOrWhiteSpace(newValue) ? null : newValue
                }
            });

            if (!AskYes("Add another changed field? y/N: "))
                break;
        }

        if (changes.Count == 0 && prices == null)
            return null;

        return new CocEventRequest(approved.Value, learningKey.Value, apprenticeshipId, changes, prices);
    }

    private static List<LearningChangeEvent.Price>? CollectPrices(IReadOnlyCollection<CocChangeKind>? pendingKinds)
    {
        if (pendingKinds != null && !pendingKinds.Contains(CocChangeKind.Price))
            return null;
        if (pendingKinds == null && !AskYes("Include prices? y/N: "))
            return null;

        ConsoleWriter.WriteColor($"Enter the whole price list, one row at a time, as {CocPriceParser.RowFormat}.", ConsoleColor.Cyan);

        var prices = new List<LearningChangeEvent.Price>();
        while (true)
        {
            var input = Menu.ReadLine($"Price {prices.Count + 1} (Escape to stop): ");
            if (input == null)
                break;

            if (!CocPriceParser.TryParseRow(input, out var price, out var error))
            {
                ConsoleWriter.WriteColor(error, ConsoleColor.Red);
                continue;
            }

            prices.Add(price!);
            if (!AskYes("Add another price? y/N: "))
                break;
        }

        return prices.Count == 0 ? null : prices;
    }

    private static bool AskYes(string prompt)
    {
        ConsoleWriter.WriteColor(prompt, ConsoleColor.White, newLine: false);
        return string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Offers a pick-list of learningKeys with a Pending (employerApprovalRequired) mapping
    /// registered in the stub — that's the normal precondition for sending an async
    /// approve/reject event, so testers can build the event straight from what they just set up
    /// instead of retyping the GUID. Falls back to manual entry if there's nothing pending, or
    /// if the tester picks that option explicitly. A mapping where only *some* fields (e.g.
    /// StartDate but not Price) are pending still counts, since the async event applies to
    /// whichever fields ended up pending.
    /// </summary>
    private static Guid? SelectLearningKey(IReadOnlyList<MappingData> mappings)
    {
        var pending = mappings
            .Select(m => (Mapping: m, LearningKey: CocMappingHelpers.ExtractLearningKey(m.Url)))
            .Where(m => m.LearningKey != null && CocMappingHelpers.TryParseChanges(m.Mapping.Data)?.Any(c => c.ApprovalStatus == "employerApprovalRequired") == true)
            .ToList();

        if (pending.Count == 0)
            return EnterLearningKeyManually();

        const string manualEntryLabel = "Enter a learningKey manually";
        var options = pending
            .Select(m => ($"{m.LearningKey} (pending)", new LearningKeyChoice(m.LearningKey)))
            .Append((manualEntryLabel, new LearningKeyChoice(null)))
            .ToArray();

        var choice = Menu.SelectOption("Select a pending mapping to build the event from:", options);
        if (choice == null)
            return null;

        return choice.Value.LearningKey ?? EnterLearningKeyManually();
    }

    /// <summary>
    /// The change kinds pending (employerApprovalRequired) on this learningKey's mapping.
    /// Null (don't restrict) when there's no matching mapping or nothing on it is pending.
    /// </summary>
    private static IReadOnlyList<CocChangeKind>? FindPendingKinds(IReadOnlyList<MappingData> mappings, Guid learningKey)
    {
        var mapping = mappings.FirstOrDefault(m => CocMappingHelpers.ExtractLearningKey(m.Url) == learningKey);
        var changes = mapping == null ? null : CocMappingHelpers.TryParseChanges(mapping.Data);
        if (changes is not { Count: > 0 })
            return null;

        var pendingKinds = changes
            .Where(c => c.ApprovalStatus == "employerApprovalRequired")
            .Select(c => CocChangeKindMapper.TryParse(c.ChangeType, out var kind) ? (CocChangeKind?)kind : null)
            .Where(k => k != null)
            .Select(k => k!.Value)
            .Distinct()
            .ToList();

        return pendingKinds.Count == 0 ? null : pendingKinds;
    }

    private const string CustomFieldLabel = "Custom field name...";

    /// <summary>
    /// Offers a pick-list of field names — restricted to <paramref name="allowedFieldNames"/>
    /// when given (the pending changeTypes on the mapping this event relates to), otherwise every
    /// field name CocChangeKind can produce (see CocChangeKindMapper.ToEventFieldNames) — plus a "Custom field name..." escape hatch for
    /// anything this tool's mapping-setup screen doesn't mock yet (e.g.
    /// ExpectedEndDate/PersonalDetails/DateOfBirthChanged). Returns null if Escape is pressed on
    /// either the picker or the custom-name entry — a genuine cancel, not a re-prompt, so the
    /// caller can decide what cancelling means at this point in the flow.
    /// </summary>
    private static string? SelectFieldName(IReadOnlyList<string>? allowedFieldNames)
    {
        var fieldNames = allowedFieldNames is { Count: > 0 }
            ? allowedFieldNames
            : CocChangeKindOptions.All.SelectMany(o => o.Value.ToEventFieldNames()).ToList();

        var options = fieldNames
            .Select(f => (f, new FieldNameChoice(f)))
            .Append((CustomFieldLabel, new FieldNameChoice(null)))
            .ToArray();

        var choice = Menu.SelectOption("Select the changed field:", options);
        if (choice == null)
            return null;

        if (choice.Value.Name != null)
            return choice.Value.Name;

        while (true)
        {
            var custom = Menu.ReadLine("Enter the custom field name (Escape to cancel): ");
            if (custom == null)
                return null;
            if (!string.IsNullOrWhiteSpace(custom))
                return custom.Trim();
            ConsoleWriter.WriteColor("Field name cannot be empty, please try again.", ConsoleColor.Red);
        }
    }

    // Menu.SelectOption<T> requires T : struct, which Guid? itself cannot satisfy — wrap it.
    private readonly record struct LearningKeyChoice(Guid? LearningKey);

    // Same reasoning as LearningKeyChoice, for the field-name picker's "Custom..." escape hatch.
    private readonly record struct FieldNameChoice(string? Name);

    private static Guid? EnterLearningKeyManually()
    {
        while (true)
        {
            var input = Menu.ReadLine("Enter the learningKey (GUID, Escape to cancel): ");
            if (input == null)
                return null;
            if (Guid.TryParse(input.Trim(), out var learningKey))
                return learningKey;
            ConsoleWriter.WriteColor("Invalid GUID, please try again.", ConsoleColor.Red);
        }
    }
}
