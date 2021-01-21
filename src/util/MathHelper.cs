
using System;

public static class MathHelper {

    public static bool RangeTest(int a, int b, int range) {
        return Math.Min(Math.Abs(a - b), 256 - Math.Abs(a - b)) <= range;
    }
}