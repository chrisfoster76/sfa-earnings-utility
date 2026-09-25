// Deliberately kept in the root EarningsUtility.UI namespace — see ConsoleWriter.cs for why.
namespace EarningsUtility.UI;

public static class Menu
{
    public enum Action
    {
        Approve,
        CocMaintain,
        CocEvent,
        Exit
    }

    public static (string Name, string Value) SelectEnvironment(Dictionary<string, string> environments)
    {
        var envNames = environments.Keys.ToList();
        var index = SelectFromList("Select an environment:", envNames, allowEscape: false);
        var name = envNames[index!.Value];
        return (name, environments[name]);
    }

    public static Action SelectAction()
    {
        var labels = new[]
        {
            "Approve a learning (as-is)",
            "Maintain Change-of-Circs approval mappings",
            "Publish a Change-of-Circs event (Approved/Rejected)"
        };

        var index = SelectFromList("Select an action:", labels, allowEscape: true);
        return index == null ? Action.Exit : (Action)index.Value;
    }

    /// <summary>
    /// Generic arrow-driven single choice from a small set of labelled options. Returns null
    /// only when allowEscape is true and the user pressed Escape.
    /// </summary>
    public static T? SelectOption<T>(string title, IReadOnlyList<(string Label, T Value)> options) where T : struct
    {
        var index = SelectFromList(title, options.Select(o => o.Label).ToList(), allowEscape: true);
        return index == null ? null : options[index.Value].Value;
    }

    /// <summary>
    /// Arrow-driven multi choice: Space toggles the highlighted option, Enter confirms (only
    /// once at least one is checked), Escape cancels. Returns null only on Escape.
    /// </summary>
    public static HashSet<T>? SelectMultiple<T>(string title, IReadOnlyList<(string Label, T Value)> options) where T : struct
    {
        var indices = MultiSelectFromList(title, options.Select(o => o.Label).ToList());
        return indices?.Select(i => options[i].Value).ToHashSet();
    }

    /// <summary>
    /// Reads a line of input with Escape-to-cancel support — plain Console.ReadLine has no way
    /// to detect Escape, so this reads key-by-key instead. Returns null if the user pressed Escape.
    /// </summary>
    public static string? ReadLine(string prompt)
    {
        ConsoleWriter.WriteColor(prompt, ConsoleColor.White, newLine: false);

        var buffer = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return buffer.ToString();
                case ConsoleKey.Escape:
                    Console.WriteLine();
                    return null;
                case ConsoleKey.Backspace:
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                        Console.Write("\b \b");
                    }
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        buffer.Append(key.KeyChar);
                        Console.Write(key.KeyChar);
                    }
                    break;
            }
        }
    }

    /// <summary>Enter confirms (true), Escape cancels (false).</summary>
    public static bool Confirm(string prompt)
    {
        ConsoleWriter.WriteColor(prompt, ConsoleColor.White, newLine: false);
        while (true)
        {
            var key = Console.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return true;
            }
            if (key == ConsoleKey.Escape)
            {
                Console.WriteLine();
                return false;
            }
        }
    }

    private static int? SelectFromList(string title, IReadOnlyList<string> labels, bool allowEscape)
    {
        // Terminals whose buffer is exactly window-sized (no scrollback — seen in VS Code's
        // integrated console and some Windows Terminal profiles) pin CursorTop at
        // BufferHeight - 1 once the screen fills, instead of growing the buffer. Reserving
        // lines for the options from there asks SetCursorPosition for a row past the buffer
        // and throws (ArgumentOutOfRangeException on 'top'). Clear first whenever there isn't
        // room for the title plus every option, so `top` always starts from a safe origin.
        if (Console.CursorTop + labels.Count + 3 >= Console.BufferHeight)
            Console.Clear();

        Console.WriteLine();
        ConsoleWriter.WriteColor(title, ConsoleColor.White);
        ConsoleWriter.WriteColor(allowEscape ? "(Up/Down to move, Enter to select, Escape to cancel)" : "(Up/Down to move, Enter to select)", ConsoleColor.DarkGray);

        var top = Console.CursorTop;
        var selected = 0;

        // Reserve the lines up front so redraws can reposition without scrolling the buffer.
        for (int i = 0; i < labels.Count; i++)
            Console.WriteLine();

        void Draw()
        {
            for (int i = 0; i < labels.Count; i++)
            {
                Console.SetCursorPosition(0, top + i);
                Console.Write(new string(' ', Math.Max(Console.WindowWidth - 1, labels[i].Length + 4)));
                Console.SetCursorPosition(0, top + i);
                if (i == selected)
                    ConsoleWriter.WriteColor($"> {labels[i]}", ConsoleColor.Cyan, newLine: false);
                else
                    ConsoleWriter.WriteColor($"  {labels[i]}", ConsoleColor.Yellow, newLine: false);
            }
        }

        Console.CursorVisible = false;
        try
        {
            Draw();

            while (true)
            {
                var key = Console.ReadKey(intercept: true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        selected = (selected - 1 + labels.Count) % labels.Count;
                        Draw();
                        break;
                    case ConsoleKey.DownArrow:
                        selected = (selected + 1) % labels.Count;
                        Draw();
                        break;
                    case ConsoleKey.Enter:
                        Console.SetCursorPosition(0, top + labels.Count);
                        return selected;
                    case ConsoleKey.Escape when allowEscape:
                        Console.SetCursorPosition(0, top + labels.Count);
                        return null;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    private static HashSet<int>? MultiSelectFromList(string title, IReadOnlyList<string> labels)
    {
        // See the matching comment in SelectFromList — same buffer-overflow guard.
        if (Console.CursorTop + labels.Count + 3 >= Console.BufferHeight)
            Console.Clear();

        Console.WriteLine();
        ConsoleWriter.WriteColor(title, ConsoleColor.White);
        ConsoleWriter.WriteColor("(Up/Down to move, Space to toggle, Enter to confirm, Escape to cancel)", ConsoleColor.DarkGray);

        var top = Console.CursorTop;
        var highlighted = 0;
        var checkedIndices = new HashSet<int>();

        for (int i = 0; i < labels.Count; i++)
            Console.WriteLine();

        void Draw()
        {
            for (int i = 0; i < labels.Count; i++)
            {
                Console.SetCursorPosition(0, top + i);
                Console.Write(new string(' ', Math.Max(Console.WindowWidth - 1, labels[i].Length + 6)));
                Console.SetCursorPosition(0, top + i);
                var box = checkedIndices.Contains(i) ? "[x]" : "[ ]";
                var cursor = i == highlighted ? ">" : " ";
                var color = i == highlighted ? ConsoleColor.Cyan : ConsoleColor.Yellow;
                ConsoleWriter.WriteColor($"{cursor} {box} {labels[i]}", color, newLine: false);
            }
        }

        Console.CursorVisible = false;
        try
        {
            Draw();

            while (true)
            {
                var key = Console.ReadKey(intercept: true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        highlighted = (highlighted - 1 + labels.Count) % labels.Count;
                        Draw();
                        break;
                    case ConsoleKey.DownArrow:
                        highlighted = (highlighted + 1) % labels.Count;
                        Draw();
                        break;
                    case ConsoleKey.Spacebar:
                        if (!checkedIndices.Remove(highlighted))
                            checkedIndices.Add(highlighted);
                        Draw();
                        break;
                    case ConsoleKey.Enter when checkedIndices.Count > 0:
                        Console.SetCursorPosition(0, top + labels.Count);
                        return checkedIndices;
                    case ConsoleKey.Escape:
                        Console.SetCursorPosition(0, top + labels.Count);
                        return null;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }
}
