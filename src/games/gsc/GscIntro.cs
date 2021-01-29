using System.Collections.Generic;

// No stall strats for now
public enum GscStrat {

    GfSkip,
    TitleSkip,
    MmBack, Continue,
    FsBack
}

public static class GscStratFunctions {

    public static void Execute(this GscStrat strat, Gsc gb) {
        switch(strat) {
            case GscStrat.GfSkip:
                gb.Press(Joypad.Start);
                break;
            case GscStrat.TitleSkip:
                gb.Press(Joypad.Start);
                break;
            case GscStrat.MmBack:
                gb.Press(Joypad.B, Joypad.Start);
                break;
            case GscStrat.Continue:
                gb.Press(Joypad.A);
                break;
            case GscStrat.FsBack:
                gb.Press(Joypad.B);
                break;
        }
    }
}

public class GscIntroSequence : List<GscStrat> {

    public int Delay;

    public GscIntroSequence(params GscStrat[] strats) : this(0, strats) { }
    public GscIntroSequence(int delay, params GscStrat[] strats) : base(strats) {
        Delay = delay;
    }

    public void Execute(Gsc gb) {
        ExecuteUntilIGT(gb);
        ExecuteAfterIGT(gb);
    }

    public void ExecuteUntilIGT(Gsc gb) {
        gb.HardReset(false);
        gb.Hold(Joypad.Left, 0x100);

        foreach(GscStrat strat in this) {
            strat.Execute(gb);
        }

        gb.Hold(Joypad.Left, "GetJoypad");
        gb.AdvanceFrames(Delay + 1, Joypad.Left);
    }

    public void ExecuteAfterIGT(Gsc gb) {
        gb.Hold(Joypad.A, "OWPlayerInput");
    }
}