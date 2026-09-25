using System.Text.Json;

namespace EarningsUtility.UI.ApprovalsStub;

/// <summary>
/// The "Maintain Change-of-Circs approval mappings" screen — a two-pane list/detail view over
/// das-api-stub's approvals/* mappings. Left pane lists what's registered (plus a "create new"
/// row at the top), each row showing the learningKey, the change kind(s) it mocks (Price /
/// StartDate / Price & StartDate), and a colour-coded outcome badge; right pane shows the
/// highlighted mapping's parsed fields and raw JSON body. Enter on an existing mapping opens a
/// Modify/Delete sub-menu; the Delete key deletes it directly, behind a modal confirmation drawn
/// over the current screen. Enter on "create new" runs the same GUID+change-kind+outcome prompt
/// as before. Escape returns to the main menu.
/// </summary>
public static class CocMappingsScreen
{
    private const int LeftWidth = 80;
    private const int RightWidth = 64;
    private const int Gap = 2;
    private const int BadgeWidth = 16;

    private enum MappingAction { Modify, Delete, Cancel }

    public static async Task RunAsync(ApprovalsStubClient stubClient, string urlPrefix)
    {
        var mappings = await stubClient.FindMappings(urlPrefix);
        var selected = 0;

        while (true)
        {
            Console.Clear();
            Draw(mappings, selected, urlPrefix);

            var itemCount = mappings.Count + 1;
            var keyInfo = Console.ReadKey(intercept: true);
            var key = keyInfo.Key;

            if (key == ConsoleKey.Delete && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) && mappings.Count > 0)
            {
                if (ShowDeleteAllConfirmation(mappings.Count))
                {
                    Console.Clear();
                    var failures = new List<string>();
                    await Spinner.RunAsync($"Deleting {mappings.Count} mapping(s)...", async () =>
                    {
                        foreach (var mapping in mappings)
                        {
                            try
                            {
                                await stubClient.DeleteMapping(mapping.HttpMethod, mapping.Url);
                            }
                            catch (HttpRequestException ex)
                            {
                                failures.Add($"{mapping.Url}: {ex.Message}");
                            }
                        }
                    });

                    if (failures.Count > 0)
                        ShowErrorModal(new[] { $"Failed to delete {failures.Count} of {mappings.Count} mapping(s):" }.Concat(failures));

                    mappings = await stubClient.FindMappings(urlPrefix);
                    selected = 0;
                }
                continue;
            }

            switch (key)
            {
                case ConsoleKey.UpArrow:
                    selected = (selected - 1 + itemCount) % itemCount;
                    break;

                case ConsoleKey.DownArrow:
                    selected = (selected + 1) % itemCount;
                    break;

                case ConsoleKey.Enter when selected == 0:
                    Console.Clear();
                    await CreateAsync(stubClient);
                    mappings = await stubClient.FindMappings(urlPrefix);
                    selected = 0;
                    break;

                case ConsoleKey.Enter:
                    Console.Clear();
                    await ActOnMappingAsync(stubClient, mappings[selected - 1]);
                    mappings = await stubClient.FindMappings(urlPrefix);
                    selected = Math.Min(selected, mappings.Count);
                    break;

                case ConsoleKey.Delete when selected != 0:
                    var toDelete = mappings[selected - 1];
                    if (ShowDeleteConfirmation(toDelete))
                    {
                        Console.Clear();
                        try
                        {
                            await Spinner.RunAsync("Deleting mapping...", () => stubClient.DeleteMapping(toDelete.HttpMethod, toDelete.Url));
                        }
                        catch (HttpRequestException ex)
                        {
                            ShowErrorModal($"Failed to delete mapping: {ex.Message}");
                        }
                        mappings = await stubClient.FindMappings(urlPrefix);
                        selected = Math.Min(selected, mappings.Count);
                    }
                    break;

                case ConsoleKey.Escape:
                    return;
            }
        }
    }

    private static async Task CreateAsync(ApprovalsStubClient stubClient)
    {
        ConsoleWriter.WriteColor("Create a new Change-of-Circs response", ConsoleColor.White);
        Console.WriteLine();

        var collected = CocSetupPrompts.Collect();
        if (collected == null)
        {
            ConsoleWriter.WriteColor("Cancelled.", ConsoleColor.DarkGray);
        }
        else
        {
            var (learningKey, outcomes) = collected.Value;
            try
            {
                await stubClient.RegisterApproval(learningKey, outcomes);
                ConsoleWriter.WriteColor($"Registered approvals/{learningKey} as {DescribeOutcomes(outcomes)}.", ConsoleColor.Green);
            }
            catch (HttpRequestException ex)
            {
                ConsoleWriter.WriteColor($"Failed to register mapping: {ex.Message}", ConsoleColor.Red);
            }
        }

        PromptContinue();
    }

    private static async Task ActOnMappingAsync(ApprovalsStubClient stubClient, MappingData mapping)
    {
        ConsoleWriter.WriteColor($"Mapping: {mapping.HttpMethod} {mapping.Url}", ConsoleColor.White);
        Console.WriteLine();

        var options = new (string Label, MappingAction Value)[]
        {
            ("Modify outcome", MappingAction.Modify),
            ("Delete", MappingAction.Delete),
            ("Cancel", MappingAction.Cancel)
        };
        var action = Menu.SelectOption("What would you like to do?", options) ?? MappingAction.Cancel;
        Console.WriteLine();

        switch (action)
        {
            case MappingAction.Modify:
                var learningKey = ExtractLearningKey(mapping.Url);
                if (learningKey == null)
                {
                    ConsoleWriter.WriteColor("Could not determine a learningKey from this mapping's URL — skipping.", ConsoleColor.Red);
                    break;
                }

                var changeKinds = Menu.SelectMultiple("Select the type(s) of change being mocked:", CocChangeKindOptions.All);
                if (changeKinds == null)
                {
                    ConsoleWriter.WriteColor("Cancelled.", ConsoleColor.DarkGray);
                    break;
                }

                var outcomes = CocSetupPrompts.CollectOutcomes(changeKinds);
                if (outcomes == null)
                {
                    ConsoleWriter.WriteColor("Cancelled.", ConsoleColor.DarkGray);
                    break;
                }

                try
                {
                    await stubClient.RegisterApproval(learningKey.Value, outcomes);
                    ConsoleWriter.WriteColor($"Updated {mapping.Url} to {DescribeOutcomes(outcomes)}.", ConsoleColor.Green);
                }
                catch (HttpRequestException ex)
                {
                    ConsoleWriter.WriteColor($"Failed to update mapping: {ex.Message}", ConsoleColor.Red);
                }
                break;

            case MappingAction.Delete:
                if (HousekeepingPrompts.ConfirmBulkDelete(1))
                {
                    try
                    {
                        await Spinner.RunAsync("Deleting mapping...", () => stubClient.DeleteMapping(mapping.HttpMethod, mapping.Url));
                        ConsoleWriter.WriteColor($"Deleted {mapping.HttpMethod} {mapping.Url}", ConsoleColor.Green);
                    }
                    catch (HttpRequestException ex)
                    {
                        ConsoleWriter.WriteColor($"Failed to delete mapping: {ex.Message}", ConsoleColor.Red);
                    }
                }
                else
                {
                    ConsoleWriter.WriteColor("Cancelled.", ConsoleColor.DarkGray);
                }
                break;

            case MappingAction.Cancel:
                break;
        }

        PromptContinue();
    }

    private static string DescribeOutcomes(IReadOnlyDictionary<CocChangeKind, CocOutcome> outcomes) =>
        string.Join(", ", outcomes.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key} -> {kv.Value.ToApprovalStatus()}"));

    private static void PromptContinue()
    {
        Console.WriteLine();
        ConsoleWriter.WriteColor("Press any key to continue...", ConsoleColor.DarkGray);
        Console.ReadKey(intercept: true);
    }

    /// <summary>Draws a Yes/No modal over whatever's already on screen. Returns true on confirm.</summary>
    private static bool ShowDeleteConfirmation(MappingData mapping)
    {
        var lines = new[]
        {
            "Delete this mapping?",
            "",
            Fit(mapping.Url, 46),
            "",
            "Enter: delete    Esc: cancel"
        };

        DrawModal(lines, ConsoleColor.Red);

        while (true)
        {
            var key = Console.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.Enter) return true;
            if (key == ConsoleKey.Escape) return false;
        }
    }

    private static void ShowErrorModal(string message) => ShowErrorModal(new[] { message });

    private static void ShowErrorModal(IEnumerable<string> messageLines)
    {
        var lines = messageLines.Append("").Append("Press any key to continue...").ToArray();
        DrawModal(lines, ConsoleColor.Red);
        Console.ReadKey(intercept: true);
    }

    /// <summary>Confirms a bulk "delete everything in scope" action — same modal style as the
    /// single-mapping confirmation, but calls out the count so it's clear the whole list goes.</summary>
    private static bool ShowDeleteAllConfirmation(int count)
    {
        var lines = new[]
        {
            $"Delete all {count} mapping(s)?",
            "This removes everything in this scope.",
            "",
            "Enter: delete all    Esc: cancel"
        };

        DrawModal(lines, ConsoleColor.Red);

        while (true)
        {
            var key = Console.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.Enter) return true;
            if (key == ConsoleKey.Escape) return false;
        }
    }

    private static void DrawModal(IReadOnlyList<string> lines, ConsoleColor borderColor)
    {
        var width = Math.Max(lines.Max(l => l.Length), 20) + 4;
        var height = lines.Count + 2;
        var left = Math.Max((Console.WindowWidth - width) / 2, 0);
        var top = Math.Max((Console.WindowHeight - height) / 2, 0);

        Console.SetCursorPosition(left, top);
        ConsoleWriter.WriteColor("┌" + new string('─', width - 2) + "┐", borderColor, newLine: false);

        for (int i = 0; i < lines.Count; i++)
        {
            Console.SetCursorPosition(left, top + 1 + i);
            ConsoleWriter.WriteColor("│ ", borderColor, newLine: false);
            ConsoleWriter.WriteColor(lines[i].PadRight(width - 4), ConsoleColor.White, newLine: false);
            ConsoleWriter.WriteColor(" │", borderColor, newLine: false);
        }

        Console.SetCursorPosition(left, top + 1 + lines.Count);
        ConsoleWriter.WriteColor("└" + new string('─', width - 2) + "┘", borderColor, newLine: false);
    }

    private static void Draw(IReadOnlyList<MappingData> mappings, int selected, string urlPrefix)
    {
        ConsoleWriter.WriteColor("========================================", ConsoleColor.Cyan);
        ConsoleWriter.WriteColor("  Change-of-Circs approval mappings", ConsoleColor.Cyan);
        ConsoleWriter.WriteColor("========================================", ConsoleColor.Cyan);
        ConsoleWriter.WriteColor($"Scope: {urlPrefix}    (Up/Down move, Enter select, Delete removes, Ctrl+Delete removes all, Esc back)", ConsoleColor.DarkGray);
        Console.WriteLine();

        var top = Console.CursorTop;
        var leftContent = BuildLeftContent(mappings, selected);
        var rightContent = BuildRightContent(selected == 0 ? null : mappings[selected - 1], selected == 0);

        var leftInnerWidth = LeftWidth - 4;
        var leftLabelWidth = leftInnerWidth - BadgeWidth;
        var rightInnerWidth = RightWidth - 4;
        var innerHeight = Math.Max(leftContent.Count, rightContent.Count);

        Console.SetCursorPosition(0, top);
        ConsoleWriter.WriteColor("┌" + new string('─', LeftWidth - 2) + "┐", ConsoleColor.DarkGray, newLine: false);
        Console.SetCursorPosition(LeftWidth + Gap, top);
        ConsoleWriter.WriteColor("┌" + new string('─', RightWidth - 2) + "┐", ConsoleColor.DarkGray, newLine: false);

        for (int i = 0; i < innerHeight; i++)
        {
            var row = top + 1 + i;

            Console.SetCursorPosition(0, row);
            ConsoleWriter.WriteColor("│ ", ConsoleColor.DarkGray, newLine: false);
            var leftRow = i < leftContent.Count ? leftContent[i] : new LeftRow("", ConsoleColor.Gray, null, ConsoleColor.Gray, ConsoleColor.Black);
            ConsoleWriter.WriteColor(Fit(leftRow.Text, leftLabelWidth), leftRow.Color, newLine: false);
            if (leftRow.Badge == null)
                Console.Write(new string(' ', BadgeWidth));
            else
                ConsoleWriter.WriteColor(CenterBadge(leftRow.Badge, BadgeWidth), leftRow.BadgeForeground, leftRow.BadgeBackground, newLine: false);
            ConsoleWriter.WriteColor(" │", ConsoleColor.DarkGray, newLine: false);

            Console.SetCursorPosition(LeftWidth + Gap, row);
            ConsoleWriter.WriteColor("│ ", ConsoleColor.DarkGray, newLine: false);
            var (rText, rColor) = i < rightContent.Count ? rightContent[i] : ("", ConsoleColor.Gray);
            ConsoleWriter.WriteColor(Fit(rText, rightInnerWidth), rColor, newLine: false);
            ConsoleWriter.WriteColor(" │", ConsoleColor.DarkGray, newLine: false);
        }

        var bottomRow = top + 1 + innerHeight;
        Console.SetCursorPosition(0, bottomRow);
        ConsoleWriter.WriteColor("└" + new string('─', LeftWidth - 2) + "┘", ConsoleColor.DarkGray, newLine: false);
        Console.SetCursorPosition(LeftWidth + Gap, bottomRow);
        ConsoleWriter.WriteColor("└" + new string('─', RightWidth - 2) + "┘", ConsoleColor.DarkGray, newLine: false);

        Console.SetCursorPosition(0, bottomRow + 2);
    }

    private static string Fit(string text, int width)
    {
        if (text.Length > width)
            text = width > 1 ? text[..(width - 1)] + "…" : text[..width];
        return text.PadRight(width);
    }

    /// <summary>Centres badge text within a fixed-width field so the background fill looks like a pill.</summary>
    private static string CenterBadge(string text, int width)
    {
        if (text.Length >= width)
            return text[..width];
        var totalPad = width - text.Length;
        var left = totalPad / 2;
        var right = totalPad - left;
        return new string(' ', left) + text + new string(' ', right);
    }

    private readonly record struct LeftRow(string Text, ConsoleColor Color, string? Badge, ConsoleColor BadgeForeground, ConsoleColor BadgeBackground);

    private static List<LeftRow> BuildLeftContent(IReadOnlyList<MappingData> mappings, int selected)
    {
        var lines = new List<LeftRow>
        {
            new(selected == 0 ? "> + Create a new response" : "  + Create a new response",
                selected == 0 ? ConsoleColor.Cyan : ConsoleColor.Green,
                null, ConsoleColor.Gray, ConsoleColor.Black)
        };

        for (int i = 0; i < mappings.Count; i++)
        {
            var isSelected = selected == i + 1;
            var label = DescribeMapping(mappings[i]);
            var (badgeText, badgeFg, badgeBg) = DescribeOutcomeBadge(CocMappingHelpers.TryParseChanges(mappings[i].Data));
            lines.Add(new LeftRow(isSelected ? $"> {label}" : $"  {label}", isSelected ? ConsoleColor.Cyan : ConsoleColor.Yellow, badgeText, badgeFg, badgeBg));
        }

        if (mappings.Count == 0)
            lines.Add(new LeftRow("  (no mappings found)", ConsoleColor.DarkGray, null, ConsoleColor.Gray, ConsoleColor.Black));

        return lines;
    }

    /// <summary>
    /// Green "Auto approved", red "Rejected", yellow "Pending" — text badges on a coloured
    /// background rather than a single glyph, so the outcome reads at a glance without relying
    /// on a symbol that might not render on every console font/code page. Each changeType now
    /// carries its own status (per the swagger contract, confirmed 2026-09-25), so a mapping with
    /// e.g. Price pending and StartDate auto-approved gets a distinct "Mixed" badge rather than
    /// silently showing just the first entry's status.
    /// </summary>
    private static (string Text, ConsoleColor Foreground, ConsoleColor Background) DescribeOutcomeBadge(List<ApprovalFieldChange>? changes)
    {
        if (changes is not { Count: > 0 })
            return ("Unknown", ConsoleColor.White, ConsoleColor.DarkGray);

        var distinctStatuses = changes.Select(c => c.ApprovalStatus).Distinct().ToList();
        if (distinctStatuses.Count > 1)
            return ("Mixed", ConsoleColor.Black, ConsoleColor.Cyan);

        return distinctStatuses[0] switch
        {
            "autoApproved" => ("Auto approved", ConsoleColor.Black, ConsoleColor.Green),
            "autoRejected" => ("Rejected", ConsoleColor.White, ConsoleColor.Red),
            "employerApprovalRequired" => ("Pending", ConsoleColor.Black, ConsoleColor.Yellow),
            _ => ("Unknown", ConsoleColor.White, ConsoleColor.DarkGray)
        };
    }

    /// <summary>
    /// "Price" when only a Price entry is present, "StartDate" when only that is, "Price &amp;
    /// StartDate" when both — matches CocChangeKind, so what's shown here always matches what a
    /// tester picked when mocking the response.
    /// </summary>
    private static string DescribeChangeKinds(MappingData mapping)
    {
        var changes = CocMappingHelpers.TryParseChanges(mapping.Data);
        if (changes is not { Count: > 0 })
            return "Unknown";

        var hasPrice = changes.Any(c => c.ChangeType == "Price");
        var hasStartDate = changes.Any(c => c.ChangeType == "StartDate");

        return (hasPrice, hasStartDate) switch
        {
            (true, true) => "Price & StartDate",
            (true, false) => "Price",
            (false, true) => "StartDate",
            _ => string.Join(", ", changes.Select(c => c.ChangeType).Distinct())
        };
    }

    private static string DescribeMapping(MappingData mapping)
    {
        var learningKey = ExtractLearningKey(mapping.Url);
        var keyText = learningKey?.ToString() ?? mapping.Url;
        // Plain ASCII separator, same reasoning as DescribeOutcomeBadge — not every console
        // font/code page renders something like "·" reliably.
        return $"{keyText} | {DescribeChangeKinds(mapping)}";
    }

    private static Guid? ExtractLearningKey(string url) => CocMappingHelpers.ExtractLearningKey(url);

    private static List<(string Text, ConsoleColor Color)> BuildRightContent(MappingData? mapping, bool isCreateRow)
    {
        var lines = new List<(string, ConsoleColor)>();

        if (isCreateRow)
        {
            lines.Add(("Create a new response", ConsoleColor.White));
            lines.Add(("", ConsoleColor.Gray));
            lines.Add(("Registers a new PUT approvals/{learningKey}", ConsoleColor.Gray));
            lines.Add(("mapping with a chosen outcome.", ConsoleColor.Gray));
            return lines;
        }

        if (mapping == null)
        {
            lines.Add(("No mapping selected.", ConsoleColor.Gray));
            return lines;
        }

        lines.Add(("Details", ConsoleColor.White));
        lines.Add(("", ConsoleColor.Gray));
        lines.Add(($"Method:  {mapping.HttpMethod}", ConsoleColor.Gray));
        lines.Add(($"Url:     {mapping.Url}", ConsoleColor.Gray));
        lines.Add(($"Status:  {mapping.HttpStatusCode}", ConsoleColor.Gray));

        var changes = CocMappingHelpers.TryParseChanges(mapping.Data);
        if (changes is { Count: > 0 })
        {
            // Each field is decided independently (per the swagger contract, confirmed
            // 2026-09-25) — show one line per changeType/status pair rather than assuming they
            // share a single outcome.
            lines.Add(("Changes:", ConsoleColor.Gray));
            foreach (var change in changes)
                lines.Add(($"  {change.ChangeType,-10} {change.ApprovalStatus}", ConsoleColor.Gray));
        }

        var prices = CocMappingHelpers.TryParsePrices(mapping.Data);
        if (prices is { Count: > 0 })
        {
            lines.Add(("", ConsoleColor.Gray));
            lines.Add(("Prices (echoed, no per-entry status):", ConsoleColor.Gray));
            foreach (var price in prices)
                lines.Add(($"  {price.EffectiveFrom}  training {price.TrainingPrice}  assessment {price.AssessmentPrice}", ConsoleColor.Gray));
        }

        lines.Add(("", ConsoleColor.Gray));
        lines.Add(("Raw JSON:", ConsoleColor.White));
        foreach (var jsonLine in PrettyPrintJson(mapping.Data))
            lines.Add((jsonLine, ConsoleColor.DarkGray));

        return lines;
    }

    private static ApprovalFieldChange? TryParseFirstChange(string data) => CocMappingHelpers.TryParseFirstChange(data);

    private static IEnumerable<string> PrettyPrintJson(string data)
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
            var pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            return pretty.Replace("\r\n", "\n").Split('\n');
        }
        catch (JsonException)
        {
            return new[] { data };
        }
    }
}
