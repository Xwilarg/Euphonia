using Discord;

namespace DiscordBot;

public static class Log
{
    public static Task LogAsync(LogMessage msg)
    {
        var cc = Console.ForegroundColor;
        Console.ForegroundColor = msg.Severity switch
        {
            LogSeverity.Critical => ConsoleColor.DarkRed,
            LogSeverity.Error => ConsoleColor.Red,
            LogSeverity.Warning => ConsoleColor.DarkYellow,
            LogSeverity.Info => ConsoleColor.White,
            LogSeverity.Verbose => ConsoleColor.Green,
            LogSeverity.Debug => ConsoleColor.DarkGreen,
            _ => throw new NotImplementedException("Invalid log level " + msg.Severity)
        };
        Console.Out.WriteLineAsync(msg.ToString());
        Console.ForegroundColor = cc;
        return Task.CompletedTask;
    }

    public static async Task LogErrorAsync(Exception e)
    {
        await LogAsync(new LogMessage(LogSeverity.Error, e.Source, e.Message, e));

        // TODO
    }
}