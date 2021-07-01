using System;
using static Tests;

public static class GscManipTests {

    public static void RunAllTests() {
        RunAllTestsInFile(typeof(GscManipTests));
    }

    [Test]
    public static void GoldTotodile() {
        Gold gb = new Gold("baseSaves/gold_toto_4_3.sav");
        Test("gold-toto-fdff", () => {
            string got = "";
            gb.IGTCheck(120, new GscIntroSequence(11), 1, () => {
                gb.Execute("R S_B D A+R R D U");
                gb.InjectOverworld(Joypad.A);
                gb.AdvanceFrame(Joypad.A);
                gb.Press(Joypad.B);
                gb.ClearText(Joypad.B);
                gb.Press(Joypad.A);
                gb.ClearText(Joypad.B, 3);
                gb.Hold(Joypad.B, "CalcMonStats");
                got = gb.PartyMon1.ToString();
                return true;
            }, 0, 23);
            return ("L5 TOTODILE DVs 0xfdff", got);
        });
    }

    [Test]
    public static void GoldRoute29() {
        string[][] paths = {
            new string[] {
                "L L L L L A+L D D L L D L L D L L D D L D L L L L L L L L L L U L L U U U R R R U A+R R U U U U L U L L L L L L L L L L L L L L U U L L D L L A+D D L L L L A+L L L L A+L L L L L L L L L L L L L L L L L U U L L L L U L L L L L L L L L L U U U U A+U U U U U U R R R R R R U U U U U U U U U U U U U U U U U U R R U U U U U U U U U U R R U U R U U U U L U U U",
                "L L L L L A+L D L L L L L L D D D D A+D L D L L L L L L L L L L U L L U U U U U R R R R R U U U U L L L L L L L L L L L L L L L U U L L L L L D D A+D L L L L L L L A+L L L L L L L L L L L L L L L L L U U L L L L U A+L L L L L L L A+L L L U U U U U U U U U R R R R R R U U U U U U U U U U U U U U U U U U U R R U U U U U U U U U U R R U U R U U U U L U U U",
                "L L L L L A+L D D L L L D D L D D L A+L L L L L L L D L L L L L U U L L U U U U R R R R R U U U U L L L L L L L L L L L L L L L U U L L L D L L A+D D L L L A+L L L L L L L L L L L L A+L L L L L L L L L U L L L U U L L L L L L L L L L L U U U U U U U U U R R R R R R U U U U U U U U U U U U R R U U U U U U U U U U U U U U U U U R U U R R U U U U L U U U",
            },
            new string[] {
                "L L L L L A+L D D L L D L L D L L D D L D L L L L L L L L L L U L L U U U R R U A+R R R U U U U L U L L L L L L L L L L L L L L U U L L D L L A+D D L L L L A+L L L L A+L L L L L L L L L L L L L L L L L U L A+L U L L U A+L L A+L L L L L L L L U U U U U U U U U R R R R R U U U U U U U U U U U U U U A+U U U L L U U L L A+L U U U U L L A+U",
                "L L L L L A+L D L L L L L L D D D D A+D L D L L L L L L L L L L U L L U U U U U R R R R R U U U U L L L L L L L L L L L L L L L U U L L L L L D D A+D L L L L L L L A+L L L L L L L L L L L L L L L L L L L U U U L A+L L L L L L A+L L L L A+L U U U U A+U U U U U U R R R A+R R U U U U U U U U U U U U U U U L L L L U U U L U U L L U A+U U",
                "L L L L L A+L D D L L L D D L D D L A+L L L L L L L D L L L L L U U L L U U U U R R R R R U U U U L L L L L L L L L L L L L L L U U L L L D L L A+D D L L L A+L L L L L L L L L L L A+L L L L L L L L L L U U U L L L L A+L L L L L L L L L L U U U U U U U U U R U R R R R U U U U U U U U U U U U U U U U U U L L L L U L U U A+L U A+L U",
            }
        };

        string[] expected = {
            "No encounter at 6657#17/11",
            "No encounter at 6657#5/25",
        };

        for(int pass = 0; pass < 2; pass++) {
            int passnum = pass + 1;
            Gold gb = new Gold("baseSaves/gold_r29_pass" + passnum + ".sav");
            MultiplePaths(gb, 120, "gold-r29-pass" + passnum, 14, paths[pass], expected[pass]);
        }
    }

    [Test]
    public static void GoldDonnies() {
        string[] paths = {
            "L U U U U U U U L U U U U A+U R U U U R U R A+R R U U A+U U R U U U U U U A+R R R R R U A+U U U U L L L A+L L L L L L L L D L L L L D A+L L L L U L L L L U U",
            "L U U U U U U U L U U U U A+U R U U U R U R A+R U U R U U U R U S_B U U U R R R R R U U U U U U L L L L L A+L L L L L L D L L L L L D A+L L A+L L U U L L L U",
            "L U U U U U U U L U U U U A+U R U U U R U R A+R U U R U U U S_B R U U U U R U R R R R U A+U U U U L L L L L L L L L L L D L L L L L D L L A+L L U L U A+L L U",
            "L U U U U U U U L U U U U A+U R U U U R U R A+R R U U R U U U S_B U U U U U R R R R R U A+U U U U L L L L L L L L L L L D L L L L L D L L L L L L L U U U",
        };

        Gold gb = new Gold("baseSaves/gold_donnies.sav");
        MultiplePaths(gb, 300, "gold-donnies", 12, paths, "No encounter at 6658#9/9");
    }

    [Test]
    public static void CrystalTotodile() {
        string[] paths = {
            "D A+R R A+R U S_B",
            "D A+R R A+R U",
        };

        string[] expectedDvs = {
            "ffff",
            "ffef",
        };

        Crystal gb = new Crystal("baseSaves/crystal_toto_4_4.sav");
        for(int frame = 0; frame < 2; frame++) {
            Test("crystal-toto-" + expectedDvs[frame], () => {
                string got = "";
                gb.IGTCheck(120, new GscIntroSequence(61 + frame), 1, () => {
                    gb.Execute(paths[frame]);
                    gb.InjectOverworld(Joypad.A);
                    gb.AdvanceFrame(Joypad.A);
                    gb.Press(Joypad.B);
                    gb.ClearText(Joypad.B);
                    gb.Press(Joypad.A);
                    gb.ClearText(Joypad.B, 3);
                    gb.Hold(Joypad.B, "CalcMonStats");
                    got = gb.PartyMon1.ToString();
                    return true;
                }, 0, 4);
                return ("L5 TOTODILE DVs 0x" + expectedDvs[frame], got);
            });
        }
    }

    [Test]
    public static void CrystalRoute29() {
        string[][] paths = {
            new string[] {
                "L L L L L L L L L L D D D L D D L L D D L L L L L L L L L L U U L U U U U R R R R R U U U L U L L A+L L L U L L L L L L L L L U L L D L L A+L D L A+D L A+L L L L L L L L L L L L L L L L L L L L L L L U U U L L L L L L L L L L L L L U U U U U U U U U R R R R R U U U U U R U U U U U U U R R U U U U U U U U U U U U U U U U U R U U R R U U U U L U U U",
                "L L L L L L L L L L D D D L D D L L D D L L L L L L L L L L U U L U U U U R R R R R U U U L U L L L L L U L A+L L L L L L L L U L L D L L A+L D A+L D A+L L L L L L L L L L L L L L L L L L L L L L L L L L A+U U U L L L L L L L L L L L U U U U U U U U U R R R R R U U U U U R U U U U U U U U U U U R U U R U U U U U U U U U U U R U U R R U U U U L U U U",
                "L L L L L L L L L L D D D L D D L L D D L L L L L L L L L U U L L U U U U R R R R R U U U L L L U L L L U L L L L L L L L L U L L L L A+L D D L L L A+L L L L A+L L L L L L L L L L L L L L L D L A+L L L L U U U L L L L L L L L L L L U U U U U U U U U R R R R R U U U U U R U U U U U U U U U U R U R U U U U U U U U U U U U U R R U U R U U U U L U U U",
            },
            new string[] {
                "L L L L L L L L L L D D D L D D L L D D L L L L L L L L L L U U L U U U U R R R R R U U U L U L L A+L L L U L L L L L L L L L U L L D L L A+L D L A+D L A+L L L L L L L L L L L L L L L L L L L L L L L U A+U U L L L L L L L L L L L L L U U U U U U U U U R U R R R R U U U U U U U U U U U U U U U L L U U L L L L U U U U L U U",
                "L L L L L L L L L L D D D L D D L L D D L L L L L L L L L L U U L U U U U R R R R R U U U L U L L L L L U L A+L L L L L L L L U L L D L L A+L D A+L D A+L L L L L L L L L L L L L L L L L L L L L L A+L L L L L A+L U A+U U L L L L L L L L L U U U U U U U U U U R R R R R U U U U U U U U U U U U U A+U U U U U L L L U U L L U L L U U",
                "L L L L L L L L L L D D D L D D L L D D L L L L L L L L L U U L L U U U U R R R R R U U U L L L U L L L U L L L L L L L L L U L L L L A+L D D L L L A+L L L L L L L L L L L L L L L L L L L D L L L L U U L A+U L L L A+L L A+L L L L L A+L U U U U U U U U U U R R R R R U U U U U U U U U U U U U U A+L U U U U L L A+L U U L U L L U U",
            }
        };

        string[] expected = {
            "No encounter at 6657#17/11",
            "No encounter at 6657#5/25",
        };

        for(int pass = 0; pass < 2; pass++) {
            int passnum = pass + 1;
            Crystal gb = new Crystal("baseSaves/crystal_r29_pass" + passnum + ".sav");
            MultiplePaths(gb, 120, "crystal-r29-pass" + passnum, 2, paths[pass], expected[pass]);
        }
    }

    [Test]
    public static void CrystalRaikou() {
        string[] paths = {
            "R R R SEL R D D D D D D U A+D R L R D D D D D L S_B U R R U U D U U D A+D",
            "R R R SEL R D D D D D D U A+D R L R D D A+D D L U S_B R S_B R U D",
            "R R R SEL R D D D D D D U A+D R L R D D D D S_B D U U L L L SEL R",
        };

        string[] expected = {
            "L40 RAIKOU DVs 0xfd9e at 2564#9/2",
            "L40 RAIKOU DVs 0xfdbf at 2564#9/2",
            "L40 RAIKOU DVs 0xfdbf at 2564#6/2",
        };

        Crystal gb = new Crystal("baseSaves/crystal_raikou.sav");
        MultiplePaths(gb, 1800, "crystal-raikou", 11, paths, expected);
    }

    private static void MultiplePaths(Gsc gb, int timesec, string name, int frameStart, string[] paths, string expected) {
        string[] expectedArray = new string[paths.Length];
        Array.Fill(expectedArray, expected);
        MultiplePaths(gb, timesec, name, frameStart, paths, expectedArray);
    }

    private static void MultiplePaths(Gsc gb, int timesec, string name, int frameStart, string[] paths, string[] expected) {
        for(int frame = 0; frame < paths.Length; frame++) {
            Test(name + "-frame" + (frame + 1), () => {
                string got = "";
                gb.IGTCheck(timesec, new GscIntroSequence(frameStart + frame), 1, () => {
                    int ret = gb.Execute(paths[frame]);
                    got = GetEncounterLog(gb);
                    return true;
                });
                return (expected[frame], got);
            });
        }
    }

    private static string GetEncounterLog(Gsc gb) {
        int addr = gb.CpuRead("hROMBank") << 16 | gb.PC;
        if(addr == gb.OverworldLoopAddress) {
            return string.Format("No encounter at {0}", gb.Tile);
        } else if(addr == gb.SYM["RandomEncounter.ok"]) {
            gb.RunUntil("CalcMonStats");
            return string.Format("{0} at {1}", gb.EnemyMon, gb.Tile);
        } else if(addr == gb.SYM["PrintLetterDelay.checkjoypad"]) {
            return string.Format("Interacted with object at {0}", gb.Tile);
        } else if(addr == gb.SYM["DoPlayerMovement.BumpSound"]) {
            return string.Format("Bonked wall at {0}", gb.Tile);
        } else {
            return "Error";
        }
    }
}