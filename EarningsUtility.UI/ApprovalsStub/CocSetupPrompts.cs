namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// Prompts for a CoC approval setup. Reused for both first-time setup and modification —
/// das-api-stub's /api-stub/save is an upsert keyed on method+url, so registering again for
/// the same learningKey with a different outcome just replaces it.
/// </summary>
public static class CocSetupPrompts
{
    /// <summary>Returns null if the change-kind or any outcome selection was cancelled (Escape).</summary>
    public static (Guid LearningKey, Dictionary<CocChangeKind, CocOutcome> Outcomes)? Collect()
    {
        Guid learningKey;
        while (true)
        {
            ConsoleWriter.WriteColor("Enter the learningKey (GUID): ", ConsoleColor.White, newLine: false);
            var input = Console.ReadLine()?.Trim();
            if (Guid.TryParse(input, out learningKey))
                break;
            ConsoleWriter.WriteColor("Invalid GUID, please try again.", ConsoleColor.Red);
        }

        // Multi-select: a tester may want to mock a response for more than one change kind at
        // once (e.g. an ILR submission that moved both StartDate and price together).
        var changeKinds = Menu.SelectMultiple("Select the type(s) of change being mocked:", CocChangeKindOptions.All);
        if (changeKinds == null)
            return null;

        var outcomes = CollectOutcomes(changeKinds);
        return outcomes == null ? null : (learningKey, outcomes);
    }

    /// <summary>
    /// Prompts once per selected change kind rather than once overall — per the swagger contract
    /// (confirmed by the other team's lead dev, 2026-09-25), each field is decided independently
    /// (e.g. a Name change can auto-approve while a Price change on the same call still needs the
    /// employer), so a single outcome applied to every kind would misrepresent that. Returns null
    /// if any prompt was cancelled (Escape).
    /// </summary>
    public static Dictionary<CocChangeKind, CocOutcome>? CollectOutcomes(IEnumerable<CocChangeKind> changeKinds)
    {
        var outcomes = new Dictionary<CocChangeKind, CocOutcome>();
        foreach (var kind in changeKinds.OrderBy(k => k))
        {
            var label = CocChangeKindOptions.All.First(o => o.Value == kind).Label;
            var outcome = Menu.SelectOption($"Select the outcome for {label}:", CocOutcomeOptions.All);
            if (outcome == null)
                return null;
            outcomes[kind] = outcome.Value;
        }

        return outcomes;
    }
}
