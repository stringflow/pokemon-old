using System;
using System.Linq;
using System.Collections.Generic;

public enum RbyStrat {

    NoPal, NoPalAB, Pal, PalHold, PalAB, PalRel,
    GfSkip, GfWait,
    Hop0, Hop1, Hop2, Hop3, Hop4, Hop5, Hop6, // Red/Blue only
    Intro0, Intro1, Intro2, Intro3, Intro4, Intro5, Intro6, IntroWait, // Yellow only
    TitleSkip,
    NewGame, Continue, Backout,
}

public static class RbyStratFunctions {

    public const int BiosJoypad = 0x21D;

    public static void Execute(this RbyStrat strat, Rby gb) {
        bool yellow = gb is Yellow;

        Debug.Assert(!(yellow && ((strat >= RbyStrat.Hop0 && strat <= RbyStrat.Hop6) || (strat > RbyStrat.NoPal && strat <= RbyStrat.PalRel))), "Tried to use red/blue exclusive intro strats!");
        Debug.Assert(!(!yellow && strat >= RbyStrat.Intro0 && strat <= RbyStrat.IntroWait), "Tried to use yellow exclusive intro strats!");

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
                gb.Press(yellow ? Joypad.Start : Joypad.Up | Joypad.B | Joypad.Select);
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
            case RbyStrat.Hop6:
            case RbyStrat.IntroWait:
                gb.RunUntil("DisplayTitleScreen");
                break;
            case RbyStrat.TitleSkip:
                gb.Press(Joypad.Start);
                break;
            case RbyStrat.Continue:
            case RbyStrat.NewGame:
                gb.Press(Joypad.A);
                break;
            case RbyStrat.Backout:
                gb.Press(Joypad.B);
                break;
        }
    }

    public static string LogString(this RbyStrat strat) {
        switch(strat) {
            case RbyStrat.NoPal: return "nopal";
            case RbyStrat.NoPalAB: return "nopal(ab)";
            case RbyStrat.Pal: return "pal";
            case RbyStrat.PalHold: return "pal(hold)";
            case RbyStrat.PalAB: return "pal(ab)";
            case RbyStrat.PalRel: return "pal(rel)";
            case RbyStrat.GfSkip: return "gfskip";
            case RbyStrat.GfWait: return "gfwait";
            case RbyStrat.Hop0: return "hop0";
            case RbyStrat.Hop1: return "hop1";
            case RbyStrat.Hop2: return "hop2";
            case RbyStrat.Hop3: return "hop3";
            case RbyStrat.Hop4: return "hop4";
            case RbyStrat.Hop5: return "hop5";
            case RbyStrat.Hop6: return "hop6";
            case RbyStrat.Intro0: return "intro0";
            case RbyStrat.Intro1: return "intro1";
            case RbyStrat.Intro2: return "intro2";
            case RbyStrat.Intro3: return "intro3";
            case RbyStrat.Intro4: return "intro4";
            case RbyStrat.Intro5: return "intro5";
            case RbyStrat.Intro6: return "intro6";
            case RbyStrat.IntroWait: return "introwait";
            case RbyStrat.TitleSkip: return "titleskip";
            case RbyStrat.Continue: return "cont";
            case RbyStrat.NewGame: return "newgame";
            case RbyStrat.Backout: return "backout";
            default: return "";
        }
    }
}

public class RbyIntroSequence : List<RbyStrat> {

    public RbyIntroSequence(params RbyStrat[] strats) : base(strats) { }
    public RbyIntroSequence(RbyStrat pal = RbyStrat.NoPal, RbyStrat gf = RbyStrat.GfSkip, RbyStrat nido = RbyStrat.Hop0, int backouts = 0) : base() {
        Add(pal);
        Add(gf);
        Add(nido);
        Add(RbyStrat.TitleSkip);
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

    public void ExecuteUntilIGT(Rby gb) {
        int lastTitleSkip = LastIndexOf(RbyStrat.TitleSkip);
        for(int i = 0; i <= lastTitleSkip; i++) {
            this[i].Execute(gb);
        }

        if(gb.RunUntil("LoadSAV", "MainMenu.mainMenuLoop") == gb.SYM["LoadSAV"]) {
            gb.RunUntil(gb.SYM["LoadSAV0.checkSumsMatched"] + 0x18);
        }
    }

    public void ExecuteAfterIGT(Rby gb) {
        int lastTitleSkip = LastIndexOf(RbyStrat.TitleSkip);
        for(int i = lastTitleSkip + 1; i < Count; i++) {
            this[i].Execute(gb);
        }
    }

    public override string ToString() {
        return string.Join("_", this.Select(s => s.LogString()));
    }

    public static void TestStrats() {
        Dictionary<RbyIntroSequence, ushort> redIntros = new Dictionary<RbyIntroSequence, ushort>() {
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.TitleSkip, RbyStrat.NewGame), 0xA7CB },
            { new RbyIntroSequence(RbyStrat.Pal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.TitleSkip, RbyStrat.NewGame), 0xB5B8 },
            { new RbyIntroSequence(RbyStrat.NoPalAB, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x88E9 },
            { new RbyIntroSequence(RbyStrat.PalHold, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x9BD5 },
            { new RbyIntroSequence(RbyStrat.PalAB, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x91E1 },
            { new RbyIntroSequence(RbyStrat.PalRel, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.TitleSkip, RbyStrat.NewGame), 0xABC0 },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfWait, RbyStrat.Hop0, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x25B3 },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop1, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x4B9C },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop2, RbyStrat.TitleSkip, RbyStrat.NewGame), 0xEC47 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop3, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x56B1 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop4, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x687A },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop5, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x7656 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop6, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x97C7 },
        };

        Dictionary<RbyIntroSequence, ushort> yellowIntros = new Dictionary<RbyIntroSequence, ushort>() {
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x046E },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfWait, RbyStrat.Intro0, RbyStrat.TitleSkip, RbyStrat.NewGame), 0xB535 },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Intro1, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x4261 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Intro2, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x1FCC },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Intro3, RbyStrat.TitleSkip, RbyStrat.NewGame), 0xD3C3 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Intro4, RbyStrat.TitleSkip, RbyStrat.NewGame), 0x6B98 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Intro5, RbyStrat.TitleSkip, RbyStrat.NewGame), 0xE5F1 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Intro6, RbyStrat.TitleSkip, RbyStrat.NewGame), 0xD82A },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.IntroWait, RbyStrat.TitleSkip, RbyStrat.NewGame), 0xE24E },
        };

        TestIntros(new Red(true), "red", redIntros);
        TestIntros(new Yellow(true), "yellow", yellowIntros);

        void TestIntros(Rby gb, string gameName, Dictionary<RbyIntroSequence, ushort> intros) {
            foreach(var i in intros) {
                RbyIntroSequence intro = i.Key;
                ushort expectedTid = i.Value;
                Console.Write(gameName + "_" + intro.ToString() + " ... ");
                gb.HardReset();
                intro.Execute(gb);
                gb.RunUntil("PrintLetterDelay");
                ushort readTid = gb.CpuReadBE<ushort>("wPlayerID");
                if(expectedTid == readTid) {
                    Console.WriteLine("OK");
                } else {
                    Console.WriteLine("FAILURE (expected=0x{0:x4}; read=0x{1:x4})", expectedTid, readTid);
                }
            }
        }
    }
}