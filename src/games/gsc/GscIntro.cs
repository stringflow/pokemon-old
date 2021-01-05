// No stall strats for now
public enum GscStrat {

    GfSkip,
    MmBack, Continue,
    FsBack
}

public static class GscStratFunctions {

    public static void Execute(this GscStrat strat, Gsc gb) {
        switch(strat) {
            case GscStrat.GfSkip:
                gb.Hold(Joypad.Left, 0x100);
                gb.Press(Joypad.Start, Joypad.Start);
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

public class GscIntroSequence {

    public int Delay;
    public GscStrat[] Strats;

    public GscIntroSequence(params GscStrat[] strats) : this(0, strats) { }
    public GscIntroSequence(int delay, params GscStrat[] strats) => (Delay, Strats) = (delay, strats);

    public void Execute(Gsc gb) {
        ExecuteUntilIGT(gb);
        ExecuteAfterIGT(gb);
    }

    public void ExecuteUntilIGT(Gsc gb) {
        foreach(GscStrat strat in Strats) {
            strat.Execute(gb);
        }

        gb.Hold(Joypad.Left, "GetJoypad");
        gb.AdvanceFrames(Delay + 1, Joypad.Left);
    }

    public void ExecuteAfterIGT(Gsc gb) {
        gb.Hold(Joypad.A, "OWPlayerInput");
    }
}