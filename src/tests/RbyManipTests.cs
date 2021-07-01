using System.Collections.Generic;
using static Tests;

public static class RbyManipTests {

    public static void RunAllTests() {
        RunAllTestsInFile(typeof(RbyManipTests));
    }

    [Test]
    public static void TIDManips() {
        Dictionary<RbyIntroSequence, ushort> redIntros = new Dictionary<RbyIntroSequence, ushort>() {
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xA7CB },
            { new RbyIntroSequence(RbyStrat.Pal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xB5B8 },
            { new RbyIntroSequence(RbyStrat.NoPalAB, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x88E9 },
            { new RbyIntroSequence(RbyStrat.PalHold, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x9BD5 },
            { new RbyIntroSequence(RbyStrat.PalAB, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x91E1 },
            { new RbyIntroSequence(RbyStrat.PalRel, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xABC0 },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfWait, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x25B3 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfReset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x856F },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop1, RbyStrat.Title0, RbyStrat.NewGame), 0x4B9C },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop2, RbyStrat.Title0, RbyStrat.NewGame), 0xEC47 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop3, RbyStrat.Title0, RbyStrat.NewGame), 0x56B1 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop4, RbyStrat.Title0, RbyStrat.NewGame), 0x687A },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop5, RbyStrat.Title0, RbyStrat.NewGame), 0x7656 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop6, RbyStrat.Title0, RbyStrat.NewGame), 0x97C7 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfWait, RbyStrat.Hop0Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x4D0B },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop1Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xD2BF },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop2Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xEBED },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop3Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x4260 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop4Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xD965 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop5Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x0C0D },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title1, RbyStrat.NewGame), 0xBC9B },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title2, RbyStrat.NewGame), 0xDDAC },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title3, RbyStrat.NewGame), 0xE4BC },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title4, RbyStrat.NewGame), 0x2FA8 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title5, RbyStrat.NewGame), 0x0747 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title6, RbyStrat.NewGame), 0x1542 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title7, RbyStrat.NewGame), 0x664B },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0Scroll, RbyStrat.NewGame), 0xF93E },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title1Scroll, RbyStrat.NewGame), 0x32F9 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title2Scroll, RbyStrat.NewGame), 0x7FD5 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title3Scroll, RbyStrat.NewGame), 0x1642 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title4Scroll, RbyStrat.NewGame), 0x227A },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title5Scroll, RbyStrat.NewGame), 0x8490 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title6Scroll, RbyStrat.NewGame), 0xAA83 },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x35A7 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title1Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x1948 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title2Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x0018 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title3Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xCB0B },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title4Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x8858 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title5Reset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xAD4F },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0Usb, RbyStrat.CsReset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x7BAB },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title1Usb, RbyStrat.CsReset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x763F },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title2Usb, RbyStrat.CsReset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xE79F },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title3Usb, RbyStrat.CsReset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xF656 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title4Usb, RbyStrat.CsReset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x858F },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.Backout, RbyStrat.Title0, RbyStrat.NewGame), 0x0AEE },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.Options, RbyStrat.NewGame), 0x9B10 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.Options, RbyStrat.Options, RbyStrat.NewGame), 0xBE2C },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.OptionsReset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0xC880 },

            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGameReset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x5638 },
            { new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame, RbyStrat.OakReset, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.NewGame), 0x67C6 },

            { new RbyIntroSequence("red_nopal_gfskip_hop0_title0_backout_title0_backout_title0_backout_title0_backout_title0_backout_title0_backout_title0_backout_title0_opt(backout)_newgame"), 0xFC7D },
            { new RbyIntroSequence("red_nopal_gfreset_gfreset_gfreset_gfreset_gfreset_gfskip_hop0_title0_newgame"), 0x0715 },
            { new RbyIntroSequence("red_pal(ab)_gfskip_hop0_title0_backout_title0_ngreset_gfskip_hop0_title0_newgame"), 0x3399 },
        };

        Dictionary<RbyIntroSequence, ushort> yellowIntros = new Dictionary<RbyIntroSequence, ushort>() {
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0x046E },
            { new RbyIntroSequence(RbyStrat.GfWait, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0xB535 },

            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro1, RbyStrat.Title, RbyStrat.NewGame), 0x4261 },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro2, RbyStrat.Title, RbyStrat.NewGame), 0x1FCC },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro3, RbyStrat.Title, RbyStrat.NewGame), 0xD3C3 },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro4, RbyStrat.Title, RbyStrat.NewGame), 0x6B98 },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro5, RbyStrat.Title, RbyStrat.NewGame), 0xE5F1 },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro6, RbyStrat.Title, RbyStrat.NewGame), 0xD82A },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.IntroWait, RbyStrat.Title, RbyStrat.NewGame), 0xE24E },

            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro0Reset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0x012D },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro1Reset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0x9127 },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro2Reset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0xD98B },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro3Reset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0x4025 },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro4Reset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0xC78E },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro5Reset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0x9CD3 },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro6Reset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0x3ECC },

            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.TitleReset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0x62CC },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.TitleUsb, RbyStrat.CsReset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0xC7D4 },

            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.Backout, RbyStrat.Title, RbyStrat.NewGame), 0xF22B },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGameReset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0x8398 },
            { new RbyIntroSequence(RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame, RbyStrat.OakReset, RbyStrat.GfSkip, RbyStrat.Intro0, RbyStrat.Title, RbyStrat.NewGame), 0x6B9B },

            { new RbyIntroSequence("yellow_gfskip_intro0_title_backout_title_backout_title_backout_title_backout_title_backout_title_backout_title_backout_title_backout_title_backout_title_backout_title_backout_title_newgame"), 0x3BB9 },
            { new RbyIntroSequence("yellow_gfreset_gfreset_gfreset_gfreset_gfreset_gfreset_gfreset_gfskip_intro0_title_newgame"), 0xCF68 },
            { new RbyIntroSequence("yellow_gfreset_gfreset_gfskip_intro0_title_ngreset_gfskip_intro0_title_ngreset_gfskip_intro0_title_ngreset_gfskip_intro0_title_newgame"), 0xD457 },
        };

        Red red = new Red();
        Yellow yellow = new Yellow();

        TestIntros(red, "red", redIntros);
        TestIntros(yellow, "yellow", yellowIntros);

        void TestIntros(Rby gb, string gameName, Dictionary<RbyIntroSequence, ushort> intros) {
            foreach(var i in intros) {
                RbyIntroSequence intro = i.Key;
                ushort expectedTid = i.Value;
                Test(gameName + "_" + intro.ToString(), () => {
                    intro.Execute(gb);
                    gb.RunUntil("PrintLetterDelay");
                    ushort readTid = gb.CpuReadBE<ushort>("wPlayerID");
                    return (string.Format("${0:x4}", expectedTid), string.Format("${0:x4}", readTid));
                });
            }
        }
    }

    [Test]
    public static void RedNido() {
        Red gb = new Red("baseSaves/red_nido.sav");
        Test("red-nido-ffef", () => {
            string got = "";
            gb.IGTCheck(new RbyIntroSequence("red_nopal_gfskip_hop0_title0_cont_cont"), 1, () => {
                gb.Execute("L L L U L L L L A U D A U L L L A D L A D D D A D L A L L U A U U");
                got = GetEncounterLog(gb);

                return true;
            });
            return ("L4 NIDORANM DVs 0xffef at 33#33/11; Yoloball=True", got);
        });
    }

    [Test]
    public static void RedNidoExtended() {
        Red gb = new Red("baseSaves/red_nido.sav");
        Test("red-nido-ffef-extended", () => {
            string expected = "";
            string got = "";
            gb.IGTCheck(new RbyIntroSequence("red_nopal_gfskip_hop0_title0_cont_cont"), 1, () => {
                expected = "L4 NIDORANM DVs 0xffef at 33#33/11; Yoloball=True";
                gb.Execute("L L L U L L L L A U D A U L L L A D L A D D D A D L A L L U A U U");
                got = GetEncounterLog(gb);

                if(expected != got) return false;

                expected = "L5 PIDGEY DVs 0x8dfb at 13#7/48; Yoloball=True";
                gb.ClearText();
                gb.Press(Joypad.A, Joypad.None, Joypad.None, Joypad.A, Joypad.Start);
                gb.Execute("D R R U U U R R R R R R R R R R R R R R R R R R R R R U R U U U U U U R U U U U L U U U U U U A U U U U U U U U U U U U U L L L U U U U U U U U U U U R R R R U U L L L L L U U U");
                got = GetEncounterLog(gb);

                if(expected != got) return false;

                expected = "No encounter at 51#2/19";
                gb.ClearText();
                gb.Press(Joypad.B);
                gb.Execute("R U U L L L L L U U U R U U U U U U U U U U U R R R R R U R R R U U U U U U U U U U U U U U U U U U U U U U U U U U U U U U U U U A L L L L L L L L D D D D D D D L L L L U U U U U U U U U U U U U L L L L L L D D D D D D D D D D D D D D D D D D L D L L L L U U U", (gb.Maps["ViridianForest"][25, 12], () => gb.PickupItem()));
                got = GetEncounterLog(gb);

                return true;
            });
            return (expected, got);
        });
    }

    [Test]
    public static void RedMoonRoute3() {
        Red gb = new Red("baseSaves/red_moon_route3.sav");
        Test("red-moon-route3", () => {
            string got = "";
            gb.IGTCheck(new RbyIntroSequence("red_pal(hold)_gfskip_hop0_title0_cont_cont"), 1, () => {
                gb.Execute("R R R R R R R R U R R U U U U U A R R R R R R R R R R R R D D D D D R R R R R R R A R U U R R U U U U U U U U U U R R R R U U U U U U U U U U R R R R R U " + // Overworld
                           "U U U U U U L L L L L A L L L L D D " + // Water Gun
                           "R R R R U U R R R A R R U U U U U U U R R R R R R R A U U U U U U U R R R D R D D D D D D D A D D D D D D D D A D R R R R R U R R R R " + // Rare Candy
                           "U U U U U U U U R " + // Escape Rope
                           "U L U U U U U A U U U U U U L L L U U U U U U U U L L L L L L D D L A L L L L L L L D D D D D D " + // 1F -> B1F
                           "L A L L A L L A L L A L D D " + // B1F -> B2F
                           "R R R U U U L A U R " + // Mega Punch
                           "D D A D L A L L A D " + // B2F -> B1F
                           "R A R R A R R A R R A R U U " + // B1F -> 1F
                           "D D L D D D D L L L L L L L U L U U U U U L U U U U U U U U L L L U L " + // Moonstone
                           "D A D D R A R " + // 1F -> B1F
                           "D R R D D D D D D D D D D R R R A R R R R R R R R R R D R " + // B1F -> B2F
                           "R R U U U R A R R R D D R R R R R U A R U R A R R D D D D D D D D A L L L L D D D D D D D A D D L L L A L L L L L L L L L L L L A L L L L L L U U U U A U U A L U U U U U U U U", // B2F -> End
                           (gb.Maps["MtMoon1F"][5, 31], () => gb.PickupItem()), // Water Gun
                           (gb.Maps["MtMoon1F"][34, 31], () => gb.PickupItem()), // Rare Candy
                           (gb.Maps["MtMoon1F"][35, 23], () => gb.PickupItem()), // Escape Rope
                           (gb.Maps["MtMoonB2F"][28, 5], () => gb.PickupItem()), // Mega Punch
                           (gb.Maps["MtMoon1F"][3, 2], () => gb.PickupItem()) // Moonstone
                        );
                got = GetEncounterLog(gb);
                return true;
            });
            return ("L10 PARAS DVs 0xf8ec at 61#10/17; Yoloball=True", got);
        });
    }

    [Test]
    public static void RedCans() {
        Red gb = new Red("baseSaves/red_cans.sav");
        Test("red-cans", () => {
            string got = "";
            gb.IGTCheck(new RbyIntroSequence("red_nopal_gfskip_hop0_title0_cont_cont"), 1, () => {
                gb.Execute("D A L L L A U R U U U U U A");
                got = string.Format("wFirstLockTrashCanIndex={0}, wSecondLockTrashCanIndex={1}", gb.CpuRead("wFirstLockTrashCanIndex"), gb.CpuRead("wSecondLockTrashCanIndex"));
                return true;
            });
            return ("wFirstLockTrashCanIndex=8, wSecondLockTrashCanIndex=5", got);
        });
    }

    [Test]
    public static void YellowNido() {
        Yellow gb = new Yellow("baseSaves/yellow_nido.sav");
        Test("yellow-nido-f9ed", () => {
            string got = "";
            gb.IGTCheck(new RbyIntroSequence("yellow_gfskip_intro0_title_cont_cont"), 1, () => {
                gb.Execute("U R A R U");
                got = GetEncounterLog(gb);

                return true;
            });
            return ("L6 NIDORANM DVs 0xf9ed at 13#9/49; Yoloball=True", got);
        });
    }

    [Test]
    public static void YellowPidgey() {
        Yellow gb = new Yellow("baseSaves/yellow_pidgey.sav");
        Test("yellow-pidgey", () => {
            string got = "";
            gb.IGTCheck(new RbyIntroSequence("yellow_gfskip_intro0_title_cont_cont"), 1, () => {
                gb.Execute("U U U U U U L L L L L L L L L L L L L A L D D D D D A D D L L L A U U U U U L A U U U U U U U U L L L L A D D D D D D D D D D D D L D D D D A D D D L L L L L L U U A U");
                got = GetEncounterLog(gb);

                return true;
            });
            return ("L4 PIDGEY DVs 0xf3ea at 51#1/19; Yoloball=True", got);
        });
    }

    [Test]
    public static void YellowMoonNoMP() {
        Yellow gb = new Yellow("baseSaves/yellow_moon_no_mp.sav");
        Test("yellow-moon-no-mp", () => {
            string got = "";
            gb.IGTCheck(new RbyIntroSequence("yellow_gfskip_intro0_title_cont_cont"), 1, () => {
                gb.Execute("U A U U U U U U U U A U U U R R R A R R R U R U U U U U U R A R R D D D D D D D D D D D D R D D D D D R R R R R R U R R R " + // Rare Candy
                           "R A U U U A U U U U U U U L U U A U U U U U U U U A U U L L U U U U U L U L L L L L L L D D D L L L L D L L L D D L D D D A D D D D D L L L L L L L A L L L U U L U L L U U U U U U U L A U U U U " + // Moonstone
                           "D R R R D " + // 1F -> B1F
                           "D D D D D D D D R D D D D R R R A R R R A R R R A R R R R R R " + // B1F -> B2F
                           "R R U U U R R R D D A R R R R A R R U A U R A R R A R D A D D D A D D D D A D D L L D D A D D D A D D A L L L L L A L L L A L L L L L L L L L L L L L L L U U U U U A U U U U A U U A U U U U R U U A U U A U U U", // B2F -> End
                           (gb.Maps["MtMoon1F"][34, 31], () => gb.PickupItem()), // Rare Candy
                           (gb.Maps["MtMoon1F"][2, 3], () => gb.PickupItem()) // Moonstone
                           );
                got = GetEncounterLog(gb);

                return true;
            });
            return ("No encounter at 61#12/9", got);
        });
    }

    private static string GetEncounterLog(Rby gb) {
        if(gb.PC == (gb.OverworldLoopAddress & 0xffff)) {
            return string.Format("No encounter at {0}", gb.Tile);
        } else {
            return string.Format("{0} at {1}; Yoloball={2}", gb.EnemyMon, gb.Tile, gb.Yoloball());
        }
    }
}