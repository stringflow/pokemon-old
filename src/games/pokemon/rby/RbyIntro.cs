using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

public enum RbyStrat {

    NoPal, NoPalAB, Pal, PalHold, PalAB, PalRel,
    GfSkip, GfWait, GfReset,
    Hop0, Hop1, Hop2, Hop3, Hop4, Hop5, Hop6, Hop0Reset, Hop1Reset, Hop2Reset, Hop3Reset, Hop4Reset, Hop5Reset, // Red/Blue only
    Intro0, Intro1, Intro2, Intro3, Intro4, Intro5, Intro6, IntroWait, Intro0Reset, Intro1Reset, Intro2Reset, Intro3Reset, Intro4Reset, Intro5Reset, Intro6Reset, // Yellow only
    Title0, Title1, Title2, Title3, Title4, Title5, Title6, Title7,  // Red/Blue only
    Title0Scroll, Title1Scroll, Title2Scroll, Title3Scroll, Title4Scroll, Title5Scroll, Title6Scroll, Title7Scroll,  // Red/Blue only
    Title0Reset, Title1Reset, Title2Reset, Title3Reset, Title4Reset, Title5Reset, Title6Reset, Title7Reset,  // Red/Blue only
    Title0Usb, Title1Usb, Title2Usb, Title3Usb, Title4Usb, Title5Usb, Title6Usb, Title7Usb,  // Red/Blue only
    Title, TitleReset, TitleUsb, // Yellow only
    CsReset,
    Options, OptionsReset, // Red/Blue only
    NewGame, Continue, Backout, NewGameReset, OakReset,
}

public static class RbyStratFunctions {

    public const int BiosJoypad = 0x21D;

    public static BiDictionary<RbyStrat, string> Strats = new BiDictionary<RbyStrat, string>();

    static RbyStratFunctions() {
        Strats[RbyStrat.NoPal] = "nopal";
        Strats[RbyStrat.NoPalAB] = "nopal(ab)";
        Strats[RbyStrat.Pal] = "pal";
        Strats[RbyStrat.PalHold] = "pal(hold)";
        Strats[RbyStrat.PalAB] = "pal(ab)";
        Strats[RbyStrat.PalRel] = "pal(rel)";
        Strats[RbyStrat.GfSkip] = "gfskip";
        Strats[RbyStrat.GfWait] = "gfwait";
        Strats[RbyStrat.GfReset] = "gfreset";
        Strats[RbyStrat.Hop0] = "hop0";
        Strats[RbyStrat.Hop1] = "hop1";
        Strats[RbyStrat.Hop2] = "hop2";
        Strats[RbyStrat.Hop3] = "hop3";
        Strats[RbyStrat.Hop4] = "hop4";
        Strats[RbyStrat.Hop5] = "hop5";
        Strats[RbyStrat.Hop6] = "hop6";
        Strats[RbyStrat.Hop0Reset] = "hop0(reset)";
        Strats[RbyStrat.Hop1Reset] = "hop1(reset)";
        Strats[RbyStrat.Hop2Reset] = "hop2(reset)";
        Strats[RbyStrat.Hop3Reset] = "hop3(reset)";
        Strats[RbyStrat.Hop4Reset] = "hop4(reset)";
        Strats[RbyStrat.Hop5Reset] = "hop5(reset)";
        Strats[RbyStrat.Intro0] = "intro0";
        Strats[RbyStrat.Intro1] = "intro1";
        Strats[RbyStrat.Intro2] = "intro2";
        Strats[RbyStrat.Intro3] = "intro3";
        Strats[RbyStrat.Intro4] = "intro4";
        Strats[RbyStrat.Intro5] = "intro5";
        Strats[RbyStrat.Intro6] = "intro6";
        Strats[RbyStrat.Intro0Reset] = "intro0(reset)";
        Strats[RbyStrat.Intro1Reset] = "intro1(reset)";
        Strats[RbyStrat.Intro2Reset] = "intro2(reset)";
        Strats[RbyStrat.Intro3Reset] = "intro3(reset)";
        Strats[RbyStrat.Intro4Reset] = "intro4(reset)";
        Strats[RbyStrat.Intro5Reset] = "intro5(reset)";
        Strats[RbyStrat.Intro6Reset] = "intro6(reset)";
        Strats[RbyStrat.IntroWait] = "introwait";
        Strats[RbyStrat.Title0] = "title0";
        Strats[RbyStrat.Title1] = "title1";
        Strats[RbyStrat.Title2] = "title2";
        Strats[RbyStrat.Title3] = "title3";
        Strats[RbyStrat.Title4] = "title4";
        Strats[RbyStrat.Title5] = "title5";
        Strats[RbyStrat.Title6] = "title6";
        Strats[RbyStrat.Title7] = "title7";
        Strats[RbyStrat.Title0Scroll] = "title0(scroll)";
        Strats[RbyStrat.Title1Scroll] = "title1(scroll)";
        Strats[RbyStrat.Title2Scroll] = "title2(scroll)";
        Strats[RbyStrat.Title3Scroll] = "title3(scroll)";
        Strats[RbyStrat.Title4Scroll] = "title4(scroll)";
        Strats[RbyStrat.Title5Scroll] = "title5(scroll)";
        Strats[RbyStrat.Title6Scroll] = "title6(scroll)";
        Strats[RbyStrat.Title7Scroll] = "title7(scroll)";
        Strats[RbyStrat.Title0Reset] = "title0(reset)";
        Strats[RbyStrat.Title1Reset] = "title1(reset)";
        Strats[RbyStrat.Title2Reset] = "title2(reset)";
        Strats[RbyStrat.Title3Reset] = "title3(reset)";
        Strats[RbyStrat.Title4Reset] = "title4(reset)";
        Strats[RbyStrat.Title5Reset] = "title5(reset)";
        Strats[RbyStrat.Title6Reset] = "title6(reset)";
        Strats[RbyStrat.Title7Reset] = "title7(reset)";
        Strats[RbyStrat.Title0Usb] = "title0(usb)";
        Strats[RbyStrat.Title1Usb] = "title1(usb)";
        Strats[RbyStrat.Title2Usb] = "title2(usb)";
        Strats[RbyStrat.Title3Usb] = "title3(usb)";
        Strats[RbyStrat.Title4Usb] = "title4(usb)";
        Strats[RbyStrat.Title5Usb] = "title5(usb)";
        Strats[RbyStrat.Title6Usb] = "title6(usb)";
        Strats[RbyStrat.Title7Usb] = "title7(usb)";
        Strats[RbyStrat.Title7Usb] = "title7(usb)";
        Strats[RbyStrat.Title] = "title";
        Strats[RbyStrat.TitleReset] = "title(reset)";
        Strats[RbyStrat.TitleUsb] = "title(usb)";
        Strats[RbyStrat.CsReset] = "csreset";
        Strats[RbyStrat.Options] = "opt(backout)";
        Strats[RbyStrat.OptionsReset] = "opt(reset)";
        Strats[RbyStrat.Continue] = "cont";
        Strats[RbyStrat.Backout] = "backout";
        Strats[RbyStrat.NewGame] = "newgame";
        Strats[RbyStrat.NewGameReset] = "ngreset";
        Strats[RbyStrat.OakReset] = "oakreset";
    }

    public static string LogString(this RbyStrat strat) {
        return Strats[strat];
    }

    public static RbyStrat ToStrat(this string stratLog) {
        return Strats[stratLog];
    }

    public static void Execute(this RbyStrat strat, Rby gb) {
        switch(strat) {
            case RbyStrat.NoPal:
                gb.RunUntil(0x100);
                break;
            case RbyStrat.NoPalAB:
                gb.Hold(Joypad.A, 0x100);
                break;
            case RbyStrat.Pal:
                gb.RunUntil(BiosJoypad);
                gb.AdvanceFrame(Joypad.Up);
                gb.RunUntil(0x100);
                break;
            case RbyStrat.PalHold:
                gb.Hold(Joypad.Up, 0x100);
                break;
            case RbyStrat.PalAB:
                gb.RunUntil(BiosJoypad);
                gb.AdvanceFrames(70, Joypad.Up);
                gb.RunUntil(BiosJoypad);
                gb.Hold(Joypad.Up | Joypad.A, 0x100);
                break;
            case RbyStrat.PalRel:
                gb.RunUntil(BiosJoypad);
                gb.AdvanceFrame(Joypad.Up);
                gb.AdvanceFrames(70);
                gb.Hold(Joypad.Up | Joypad.A, 0x100);
                break;
            case RbyStrat.GfSkip:
                gb.Press(gb.IsYellow ? Joypad.Start : Joypad.Up | Joypad.Select | Joypad.B);
                break;
            case RbyStrat.GfWait:
                gb.RunUntil("PlayShootingStar.next");
                break;
            case RbyStrat.Hop0:
            case RbyStrat.Hop1:
            case RbyStrat.Hop2:
            case RbyStrat.Hop3:
            case RbyStrat.Hop4:
            case RbyStrat.Hop5:
                for(int i = 0; i < strat - RbyStrat.Hop0; i++) {
                    gb.RunUntil("AnimateIntroNidorino");
                    gb.RunUntil("CheckForUserInterruption");
                }
                gb.Press(Joypad.Up | Joypad.Select | Joypad.B);
                break;
            case RbyStrat.Hop0Reset:
            case RbyStrat.Hop1Reset:
            case RbyStrat.Hop2Reset:
            case RbyStrat.Hop3Reset:
            case RbyStrat.Hop4Reset:
            case RbyStrat.Hop5Reset:
                for(int i = 0; i < strat - RbyStrat.Hop0Reset; i++) {
                    gb.RunUntil("AnimateIntroNidorino");
                    gb.RunUntil("CheckForUserInterruption");
                }
                break;
            case RbyStrat.Intro0:
            case RbyStrat.Intro1:
            case RbyStrat.Intro2:
            case RbyStrat.Intro3:
            case RbyStrat.Intro4:
            case RbyStrat.Intro5:
            case RbyStrat.Intro6:
                if(strat > RbyStrat.Intro0) gb.RunUntil("YellowIntroScene" + (strat - RbyStrat.Intro0) * 2);
                gb.Press(Joypad.A);
                break;
            case RbyStrat.Intro1Reset:
            case RbyStrat.Intro2Reset:
            case RbyStrat.Intro3Reset:
            case RbyStrat.Intro4Reset:
            case RbyStrat.Intro5Reset:
            case RbyStrat.Intro6Reset:
                gb.RunUntil("YellowIntroScene" + (strat - RbyStrat.Intro0Reset) * 2);
                break;
            case RbyStrat.Hop6:
            case RbyStrat.IntroWait:
                gb.RunUntil("DisplayTitleScreen");
                break;
            case RbyStrat.Title0:
            case RbyStrat.Title1:
            case RbyStrat.Title2:
            case RbyStrat.Title3:
            case RbyStrat.Title4:
            case RbyStrat.Title5:
            case RbyStrat.Title6:
            case RbyStrat.Title7:
                for(int i = 0; i < strat - RbyStrat.Title0; i++) {
                    gb.RunUntil("TitleScreenPickNewMon");
                    gb.AdvanceFrame();
                }
                gb.Press(Joypad.Start);
                break;
            case RbyStrat.Title0Scroll:
            case RbyStrat.Title1Scroll:
            case RbyStrat.Title2Scroll:
            case RbyStrat.Title3Scroll:
            case RbyStrat.Title4Scroll:
            case RbyStrat.Title5Scroll:
            case RbyStrat.Title6Scroll:
            case RbyStrat.Title7Scroll:
                for(int i = 0; i < strat - RbyStrat.Title0Scroll + 1; i++) {
                    gb.RunUntil("TitleScreenScrollInMon");
                    gb.RunUntil("CheckForUserInterruption");
                }
                gb.Press(Joypad.Start);
                break;
            case RbyStrat.Title0Reset:
            case RbyStrat.Title1Reset:
            case RbyStrat.Title2Reset:
            case RbyStrat.Title3Reset:
            case RbyStrat.Title4Reset:
            case RbyStrat.Title5Reset:
            case RbyStrat.Title6Reset:
            case RbyStrat.Title7Reset:
                for(int i = 0; i < strat - RbyStrat.Title0Reset; i++) {
                    gb.RunUntil("TitleScreenPickNewMon");
                    gb.RunUntil("CheckForUserInterruption");
                }
                break;
            case RbyStrat.Title0Usb:
            case RbyStrat.Title1Usb:
            case RbyStrat.Title2Usb:
            case RbyStrat.Title3Usb:
            case RbyStrat.Title4Usb:
            case RbyStrat.Title5Usb:
            case RbyStrat.Title6Usb:
            case RbyStrat.Title7Usb:
                for(int i = 0; i < strat - RbyStrat.Title0Usb; i++) {
                    gb.RunUntil("TitleScreenPickNewMon");
                    gb.RunUntil("CheckForUserInterruption");
                }
                gb.Press(Joypad.Up | Joypad.Select | Joypad.B);
                break;
            case RbyStrat.Title:
                gb.Press(Joypad.Start);
                break;
            case RbyStrat.TitleUsb:
                gb.Press(Joypad.Up | Joypad.Select | Joypad.B);
                break;
            case RbyStrat.Continue:
            case RbyStrat.NewGame:
                gb.Press(Joypad.A);
                break;
            case RbyStrat.Backout:
                gb.Press(Joypad.B);
                break;
            case RbyStrat.Options:
                gb.Press(Joypad.Down | Joypad.A, Joypad.Start);
                break;
            case RbyStrat.OptionsReset:
                gb.Press(Joypad.Down | Joypad.A);
                break;
        }

        if(strat.LogString().Contains("reset")) {
            gb.Hold(Joypad.A | Joypad.B | Joypad.Start | Joypad.Select, "SoftReset");
            gb.RunFor(1);
        }
    }
}

public class RbyIntroSequence : List<RbyStrat> {

    public RbyIntroSequence(params RbyStrat[] strats) : base(strats) { }

    public RbyIntroSequence(string log) {
        string[] splitArray = log.Split("_");
        foreach(string stratLog in splitArray) {
            if(stratLog == "red" || stratLog == "blue" || stratLog == "yellow") continue;
            Add(RbyStratFunctions.ToStrat(stratLog));
        }
    }

    public RbyIntroSequence(RbyStrat pal = RbyStrat.NoPal, RbyStrat gf = RbyStrat.GfSkip, RbyStrat nido = RbyStrat.Hop0, int backouts = 0) : base() {
        Add(pal);
        Add(gf);
        Add(nido);
        Add(RbyStrat.Title0);
        Add(RbyStrat.Continue);
        for(int i = 0; i < backouts; i++) {
            Add(RbyStrat.Backout);
            Add(RbyStrat.Continue);
        }
        Add(RbyStrat.Continue);
    }

    public void Execute(Rby gb) {
        ExecuteUntilIGT(gb);
        ExecuteAfterIGT(gb);
    }

    private int IndexOfLastTitleSkip() {
        return LastIndexOf(this.Where(x => (x >= RbyStrat.Title0 && x <= RbyStrat.Title7Scroll) || (x == RbyStrat.Title)).Last());
    }

    public void ExecuteUntilIGT(Rby gb) {
        gb.HardReset(false);
        int lastTitleSkip = IndexOfLastTitleSkip();
        for(int i = 0; i <= lastTitleSkip; i++) {
            this[i].Execute(gb);
        }

        if(gb.RunUntil("LoadSAV", "MainMenu.mainMenuLoop") == gb.SYM["LoadSAV"]) {
            gb.RunUntil(gb.SYM["LoadSAV0.checkSumsMatched"] + 0x18);
        }
    }

    public void ExecuteAfterIGT(Rby gb) {
        int lastTitleSkip = IndexOfLastTitleSkip();
        for(int i = lastTitleSkip + 1; i < Count; i++) {
            this[i].Execute(gb);
        }
    }

    public override string ToString() {
        return string.Join("_", this.Select(s => s.LogString()));
    }
}