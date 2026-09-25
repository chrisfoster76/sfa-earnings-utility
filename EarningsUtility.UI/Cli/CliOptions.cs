using SFA.DAS.CommitmentsV2.Types;

namespace EarningsUtility.UI.Cli;

public class CliOptions
{
    public string? Env { get; init; }
    public string? Uln { get; init; }
    public string? Employer { get; init; }
    public string? EmployerType { get; init; }
    public string? ApprenticeshipId { get; init; }
    public string? TransferSender { get; init; }
    public string? Type { get; init; }
    public string? Ukprn { get; init; }
    public string? TrainingCode { get; init; }
    public string? Action { get; init; }
    public string? LearningKey { get; init; }
    public string? Outcome { get; init; }
    public string? ChangeType { get; init; }
    public string? EventType { get; init; }
    public string? Field { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? EffectiveFrom { get; init; }
    public bool All { get; init; }

    /// <summary>
    /// "approve" when --action is omitted, so every existing script/CI call site that predates
    /// --action keeps working unchanged.
    /// </summary>
    public string ActionOrDefault => string.IsNullOrWhiteSpace(Action) ? "approve" : Action.ToLowerInvariant();

    public bool IsOneShot =>
        Env != null && Uln != null && Employer != null && long.TryParse(Employer, out _)
        && EmployerType != null && Enum.TryParse<ApprenticeshipEmployerType>(EmployerType, ignoreCase: true, out _)
        && ApprenticeshipId != null && long.TryParse(ApprenticeshipId, out _)
        && Type != null && (Type.Equals("ShortCourse", StringComparison.OrdinalIgnoreCase) || Type.Equals("Apprenticeship", StringComparison.OrdinalIgnoreCase))
        && Ukprn != null && long.TryParse(Ukprn, out _)
        && !string.IsNullOrWhiteSpace(TrainingCode);

    public static CliOptions Parse(string[] args)
    {
        // --all is a bare boolean flag (no value) — pull it out before the pair-based parser
        // below runs, so it can't be swallowed as another flag's value or dropped for being
        // the last element.
        var all = args.Any(a => a.Equals("--all", StringComparison.OrdinalIgnoreCase));
        var remainingArgs = args.Where(a => !a.Equals("--all", StringComparison.OrdinalIgnoreCase)).ToArray();

        var values = ParseFlags(remainingArgs);

        return new CliOptions
        {
            Env = values.GetValueOrDefault("--env"),
            Uln = values.GetValueOrDefault("--uln"),
            Employer = values.GetValueOrDefault("--employer"),
            EmployerType = values.GetValueOrDefault("--employer-type"),
            ApprenticeshipId = values.GetValueOrDefault("--apprenticeship-id"),
            TransferSender = values.GetValueOrDefault("--transfer-sender"),
            Type = values.GetValueOrDefault("--type"),
            Ukprn = values.GetValueOrDefault("--ukprn"),
            TrainingCode = values.GetValueOrDefault("--training-code"),
            Action = values.GetValueOrDefault("--action"),
            LearningKey = values.GetValueOrDefault("--learning-key"),
            Outcome = values.GetValueOrDefault("--outcome"),
            ChangeType = values.GetValueOrDefault("--change-type"),
            EventType = values.GetValueOrDefault("--event-type"),
            Field = values.GetValueOrDefault("--field"),
            OldValue = values.GetValueOrDefault("--old"),
            NewValue = values.GetValueOrDefault("--new"),
            EffectiveFrom = values.GetValueOrDefault("--effective-from"),
            All = all
        };
    }

    private static Dictionary<string, string> ParseFlags(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i].StartsWith("--"))
                result[args[i]] = args[i + 1];
        return result;
    }
}
