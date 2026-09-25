using EarningsUtility.UI.Cli;

namespace EarningsUtility.Tests.Cli;

public class CliOptionsTests
{
    private static readonly string[] FullApprenticeshipArgs =
    [
        "--env", "demo",
        "--uln", "1234567890",
        "--employer", "12345",
        "--employer-type", "Levy",
        "--apprenticeship-id", "99",
        "--ukprn", "10005077",
        "--training-code", "21",
        "--type", "Apprenticeship"
    ];

    [Fact]
    public void Parse_reads_all_existing_flags()
    {
        var options = CliOptions.Parse(FullApprenticeshipArgs);

        Assert.Equal("demo", options.Env);
        Assert.Equal("1234567890", options.Uln);
        Assert.Equal("12345", options.Employer);
        Assert.Equal("Levy", options.EmployerType);
        Assert.Equal("99", options.ApprenticeshipId);
        Assert.Equal("10005077", options.Ukprn);
        Assert.Equal("21", options.TrainingCode);
        Assert.Equal("Apprenticeship", options.Type);
        Assert.Null(options.TransferSender);
    }

    [Fact]
    public void Parse_reads_optional_transfer_sender()
    {
        var args = FullApprenticeshipArgs.Append("--transfer-sender").Append("555").ToArray();

        var options = CliOptions.Parse(args);

        Assert.Equal("555", options.TransferSender);
    }

    [Fact]
    public void IsOneShot_true_when_all_required_approve_flags_are_present_and_valid()
    {
        var options = CliOptions.Parse(FullApprenticeshipArgs);

        Assert.True(options.IsOneShot);
    }

    [Fact]
    public void IsOneShot_false_when_no_args_given_so_interactive_mode_is_used()
    {
        var options = CliOptions.Parse([]);

        Assert.False(options.IsOneShot);
    }

    [Theory]
    [InlineData("--employer", "not-a-number")]
    [InlineData("--employer-type", "NotARealType")]
    [InlineData("--apprenticeship-id", "not-a-number")]
    [InlineData("--type", "NotARealLearningType")]
    [InlineData("--ukprn", "not-a-number")]
    public void IsOneShot_false_when_a_required_value_is_invalid(string flag, string invalidValue)
    {
        var args = FullApprenticeshipArgs.ToList();
        var flagIndex = args.IndexOf(flag);
        args[flagIndex + 1] = invalidValue;

        var options = CliOptions.Parse(args.ToArray());

        Assert.False(options.IsOneShot);
    }

    [Fact]
    public void IsOneShot_false_when_training_code_is_missing()
    {
        var args = FullApprenticeshipArgs.Where(a => a != "--training-code" && a != "21").ToArray();

        var options = CliOptions.Parse(args);

        Assert.False(options.IsOneShot);
    }

    [Fact]
    public void IsOneShot_true_for_ShortCourse_type()
    {
        var args = FullApprenticeshipArgs.Select(a => a == "Apprenticeship" ? "ShortCourse" : a).ToArray();

        var options = CliOptions.Parse(args);

        Assert.True(options.IsOneShot);
    }

    [Fact]
    public void ActionOrDefault_is_approve_when_action_flag_omitted_for_backward_compatibility()
    {
        var options = CliOptions.Parse(FullApprenticeshipArgs);

        Assert.Equal("approve", options.ActionOrDefault);
    }

    [Fact]
    public void ActionOrDefault_lowercases_whatever_was_passed()
    {
        var options = CliOptions.Parse(["--action", "Coc-Setup"]);

        Assert.Equal("coc-setup", options.ActionOrDefault);
    }

    [Fact]
    public void Parse_reads_coc_setup_flags()
    {
        var learningKey = Guid.NewGuid();
        var options = CliOptions.Parse(
        [
            "--action", "coc-setup",
            "--env", "demo",
            "--learning-key", learningKey.ToString(),
            "--outcome", "rejected"
        ]);

        Assert.Equal("demo", options.Env);
        Assert.Equal(learningKey.ToString(), options.LearningKey);
        Assert.Equal("rejected", options.Outcome);
    }

    [Fact]
    public void Parse_reads_all_flag()
    {
        var options = CliOptions.Parse(
            ["--action", "coc-delete", "--env", "demo", "--all"]);

        Assert.True(options.All);
    }

    [Fact]
    public void Parse_All_is_false_when_flag_not_given()
    {
        var options = CliOptions.Parse(["--action", "coc-find", "--env", "demo"]);

        Assert.False(options.All);
    }

    [Fact]
    public void Parse_all_flag_at_the_end_of_args_is_still_detected()
    {
        // Regression guard: --all has no value, so it must not be swallowed as another flag's
        // value, nor dropped because it's the last element (the existing pair-parsing loop
        // stops at args.Length - 1).
        var options = CliOptions.Parse(["--action", "coc-delete", "--env", "demo", "--all"]);

        Assert.True(options.All);
    }

    [Fact]
    public void Parse_reads_coc_event_flags()
    {
        var learningKey = Guid.NewGuid();
        var options = CliOptions.Parse(
        [
            "--action", "coc-event",
            "--env", "demo",
            "--learning-key", learningKey.ToString(),
            "--apprenticeship-id", "99",
            "--event-type", "approved",
            "--field", "TrainingPrice",
            "--old", "25000",
            "--new", "27000",
            "--effective-from", "2026-08-01"
        ]);

        Assert.Equal(learningKey.ToString(), options.LearningKey);
        Assert.Equal("99", options.ApprenticeshipId);
        Assert.Equal("approved", options.EventType);
        Assert.Equal("TrainingPrice", options.Field);
        Assert.Equal("25000", options.OldValue);
        Assert.Equal("27000", options.NewValue);
        Assert.Equal("2026-08-01", options.EffectiveFrom);
    }
}
