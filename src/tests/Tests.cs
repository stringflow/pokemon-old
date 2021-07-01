using System;
using System.Diagnostics;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class Test : Attribute {
}

public static class Tests {

    private static Stopwatch Timer = new Stopwatch();
    private static int TotalTestsRan;
    private static int TotalSuccesses;
    private static int TotalFailures;

    public static void Test(string testName, Func<(string Expected, string Got)> fn) {
        if(TotalTestsRan == 0) {
            Timer.Start();
        }

        Console.Write(testName + " ... ");
        var result = fn();

        TotalTestsRan++;
        if(result.Expected == result.Got) {
            Console.WriteLine("OK");
            TotalSuccesses++;
        } else {
            Console.WriteLine("FAILURE (expected={0}; got={1})", result.Expected, result.Got);
            TotalFailures++;
        }
    }

    public static void RunAllTests() {
        foreach(var test in Debug.FindMethodsWithAttribute<Test>()) {
            test.Function.Invoke(new object(), new object[0]);
        }

        PrintTestResults();
    }

    public static void RunAllTestsInFile(Type type) {
        foreach(var test in Debug.FindMethodsWithAttribute<Test>(type)) {
            test.Function.Invoke(new object(), new object[0]);
        }

        PrintTestResults();
    }

    public static void PrintTestResults() {
        Timer.Stop();
        Console.WriteLine();
        Console.WriteLine("{0} tests run in {1:0.000} seconds.", TotalTestsRan, Timer.Elapsed.TotalSeconds);
        Console.WriteLine("{0} FAILED ({1} tests passed)", TotalFailures, TotalSuccesses);

        Timer.Reset();
        TotalTestsRan = 0;
        TotalSuccesses = 0;
        TotalFailures = 0;
    }
}