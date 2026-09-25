// Deliberately kept in the root EarningsUtility.UI namespace — see ConsoleWriter.cs for why.
namespace EarningsUtility.UI;

/// <summary>
/// A simple text spinner for async calls with a small but noticeable delay (e.g. das-api-stub
/// deletes) — without it the screen just sits frozen between key press and redraw, which reads
/// as "did that register?" rather than "in progress". Animates in place at the cursor position
/// it starts from, then erases itself once the action completes (success or failure) so the
/// following redraw starts from a clean line.
/// </summary>
public static class Spinner
{
    private static readonly char[] Frames = { '|', '/', '-', '\\' };
    private static readonly TimeSpan FrameDelay = TimeSpan.FromMilliseconds(100);

    public static async Task RunAsync(string message, Func<Task> action)
    {
        using var cts = new CancellationTokenSource();
        var animation = AnimateAsync(message, cts.Token);

        try
        {
            await action();
        }
        finally
        {
            cts.Cancel();
            await animation;
        }
    }

    private static async Task AnimateAsync(string message, CancellationToken token)
    {
        var left = Console.CursorLeft;
        var top = Console.CursorTop;
        var wasVisible = Console.CursorVisible;
        Console.CursorVisible = false;
        var frame = 0;

        try
        {
            while (true)
            {
                Console.SetCursorPosition(left, top);
                ConsoleWriter.WriteColor($"{message} {Frames[frame % Frames.Length]}", ConsoleColor.DarkGray, newLine: false);
                frame++;

                try
                {
                    await Task.Delay(FrameDelay, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }
        finally
        {
            Console.SetCursorPosition(left, top);
            Console.Write(new string(' ', message.Length + 2));
            Console.SetCursorPosition(left, top);
            Console.CursorVisible = wasVisible;
        }
    }
}
