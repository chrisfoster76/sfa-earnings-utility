// Deliberately kept in the root EarningsUtility.UI namespace (not EarningsUtility.UI.Console) —
// a nested "Console" namespace would shadow System.Console for every file that uses this class,
// which Program.cs relies on heavily (Console.ReadLine, Console.Clear, etc).
namespace EarningsUtility.UI;

public static class ConsoleWriter
{
    public static void WriteColor(string text, ConsoleColor color, bool newLine = true)
    {
        Console.ForegroundColor = color;
        if (newLine) Console.WriteLine(text);
        else Console.Write(text);
        Console.ResetColor();
    }

    /// <summary>For badge-style text on a coloured background, e.g. an outcome pill.</summary>
    public static void WriteColor(string text, ConsoleColor foreground, ConsoleColor background, bool newLine = true)
    {
        Console.ForegroundColor = foreground;
        Console.BackgroundColor = background;
        if (newLine) Console.WriteLine(text);
        else Console.Write(text);
        Console.ResetColor();
    }
}
