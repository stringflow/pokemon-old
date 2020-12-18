using System;

public static class Debug {

    public static void Log(string format, params object[] parameters) {
        Info(format, parameters);
    }

    public static void Log(object parameter) {
        Info(parameter.ToString());
    }

    public static void Info(string format, params object[] parameters) {
        LogInternal(ConsoleColor.Green, "Info", format, parameters);
    }

    public static void Warning(string format, params object[] parameters) {
        LogInternal(ConsoleColor.Yellow, "Warning", format, parameters);
    }

    public static void Error(string format, params object[] parameters) {
        LogInternal(ConsoleColor.Red, "Error", format, parameters);
    }

    public static void Assert(bool condition, string format, params object[] parameters) {
        if(!condition) Error(format, parameters);
    }

    private static void LogInternal(ConsoleColor color, string level, string format, params object[] parameters) {
        DateTime time = DateTime.Now;
        Console.ForegroundColor = color;
        Console.Write("[{0}] [{1}] ", time, level);
        Console.WriteLine(format, parameters);
    }
}