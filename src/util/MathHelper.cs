
using System;

public static class MathHelper {

    public static bool RangeTest(int a, int b, int range) {
        return Math.Min(Math.Abs(a - b), Math.Abs(Math.Min(a, b) + (256 - Math.Max(a, b)))) <= range;
    }
}