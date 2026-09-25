namespace EarningsUtility.UI.ApprovalsStub;

public static class HousekeepingPrompts
{
    public static bool ConfirmBulkDelete(int count)
    {
        ConsoleWriter.WriteColor($"Delete all {count} mapping(s)? y/N: ", ConsoleColor.Yellow, newLine: false);
        var input = Console.ReadLine()?.Trim();
        return string.Equals(input, "y", StringComparison.OrdinalIgnoreCase);
    }
}
