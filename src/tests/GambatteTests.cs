using System;
using static Tests;

public static class GambatteTests {

    public static void RunAllTests() {
        RunAllTestsInFile(typeof(GambatteTests));
    }

    [Test]
    public static void DetailedState() {
        Red gb = new Red();
        Test("gambatte-detailed-state", () => {
            gb.RunUntil("DisplayTitleScreen");
            byte[] state = gb.SaveState();
            DetailedState detailed = new DetailedState(state);
            byte[] ret = detailed.ToBuffer();

            if(state.Length != ret.Length) return ("length=" + state.Length, "length=" + ret.Length);

            for(int i = 0; i < state.Length; i++) {
                byte b1 = state[i];
                byte b2 = ret[i];
                if(b1 != b2) return (string.Format("${0:x8}: {1}", i, b1), string.Format("${0:x8}: {1}", i, b2));
            }

            return ("", "");
        });
    }
}