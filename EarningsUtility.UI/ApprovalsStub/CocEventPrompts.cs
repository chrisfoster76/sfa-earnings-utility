using SFA.DAS.CommitmentsV2.Messages.Events;

namespace EarningsUtility.UI.ApprovalsStub;

public static class CocEventPrompts
{
    private static readonly (string Label, bool Value)[] Outcomes =
    {
        ("Approved (LearningChangeApprovedEvent)", true),
        ("Rejected (LearningChangeRejectedEvent)", false)
    };

    /// <summary>Returns null if the flow was cancelled (Escape on the learningKey, outcome, or
    /// first field-name prompt).</summary>
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

        // Only offered when we can find the mapping this event relates to and it has at least
        // one pending changeType — narrows the field picker to what an employer decision could
        // plausibly apply to. Null (mapping not found, e.g. a manually-typed learningKey for
        // something this tool didn't set up, or nothing on it is pending) falls back to the
        // full field-name list.
        var pendingFieldNames = FindPendingFieldNames(mappings, learningKey.Value);

        var changes = new Dictionary<string, LearningChangeEvent.Change>();
        while (true)
        {
            var field = SelectFieldName(pendingFieldNames);
            if (field == null)
            {
                // Escape here cancels adding *this* field. If nothing's been collected yet
                // that means cancelling the whole event (consistent with every other Escape
                // point in this flow); once at least one field is in, treat it the same as
                // answering "n" to "add another?" rather than discarding what's already valid.
                if (changes.Count == 0)
                    return null;
                break;
            }

            ConsoleWriter.WriteColor("Old value (optional, press Enter to skip): ", ConsoleColor.White, newLine: false);
            var oldValue = Console.ReadLine()?.Trim();

            ConsoleWriter.WriteColor("New value (optional, press Enter to skip): ", ConsoleColor.White, newLine: false);
            var newValue = Console.ReadLine()?.Trim();

            DateTime? effectiveFrom = null;
            ConsoleWriter.WriteColor("Effective from date (optional, yyyy-MM-dd, press Enter to skip): ", ConsoleColor.White, newLine: false);
            var effectiveFromInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(effectiveFromInput))
            {
                if (DateTime.TryParse(effectiveFromInput, out var parsed))
                    effectiveFrom = parsed;
                else
                    ConsoleWriter.WriteColor("Could not parse date, leaving blank.", ConsoleColor.Red);
            }

            changes[field] = new LearningChangeEvent.Change
            {
                Old = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue,
                New = string.IsNullOrWhiteSpace(newValue) ? null : newValue,
                EffectiveFromDate = effectiveFrom
            };

            ConsoleWriter.WriteColor("Add another changed field? y/N: ", ConsoleColor.White, newLine: false);
            if (!string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                break;
        }

        return new CocEventRequest(approved.Value, learningKey.Value, apprenticeshipId, changes);
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
    /// Looks up the mapping this learningKey relates to (whether it was picked from the pending
    /// list above or typed manually) and returns the event field names for whichever changeTypes
    /// are currently pending (employerApprovalRequired) on it — an approve/reject event should
    /// only plausibly report on fields that were actually left pending, not ones already
    /// auto-decided. Returns null (meaning "don't restrict") when no matching mapping is found
    /// — e.g. a manually-typed learningKey for something this tool didn't set up — or when
    /// nothing on the matched mapping is pending.
    /// </summary>
    private static IReadOnlyList<string>? FindPendingFieldNames(IReadOnlyList<MappingData> mappings, Guid learningKey)
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

        if (pendingKinds.Count == 0)
            return null;

        return pendingKinds.SelectMany(k => k.ToEventFieldNames()).ToList();
    }

    private const string CustomFieldLabel = "Custom field name...";

    /// <summary>
    /// Offers a pick-list of field names — restricted to <paramref name="allowedFieldNames"/>
    /// when given (the pending changeTypes on the mapping this event relates to), otherwise every
    /// field name CocChangeKind can produce (TrainingPrice, AssessmentPrice, StartDate — see
    /// CocChangeKindMapper.ToEventFieldNames) — plus a "Custom field name..." escape hatch for
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
