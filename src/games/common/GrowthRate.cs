public enum GrowthRate {

    MediumFast,
    SlightlyFast,
    SlightlySlow,
    MediumSlow,
    Fast,
    Slow
}

public static class GrowthRateFunctions {

    public static int CalcExpNeeded(this GrowthRate growthRate, int n) {
        switch(growthRate) {
            case GrowthRate.MediumFast: return n * n * n;
            case GrowthRate.SlightlyFast: return 3 / 4 * n * n * n + 10 * n * n - 30;
            case GrowthRate.SlightlySlow: return 3 / 4 * n * n * n + 20 * n * n - 70;
            case GrowthRate.MediumSlow: return 6 / 5 * n * n * n + -15 * n * n + 100 * n - 140;
            case GrowthRate.Fast: return 4 / 5 * n * n * n;
            case GrowthRate.Slow: return 5 / 4 * n * n * n;
            default: return 0;
        }
    }
}