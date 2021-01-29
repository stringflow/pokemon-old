using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class TotodileState {

    public string Log;
    public GscTile Tile;
    public int EdgeSet;
    public int WastedFrames;
    public bool CanA;
}

// Code heavily plagiarized from: https://github.com/entrpntr/gb-rta-bruteforce/blob/master/src/dabomstew/rta/entei/GSToto.java
public static class Totodile {

    const int MaxCost = 150;
    static StreamWriter Writer;

    public static void MakeSave(byte x, byte y, byte hour, byte minute, byte momStep, byte audio, byte frameType, byte menuAccount, byte igt) {
        byte[] baseSave = File.ReadAllBytes("basesaves/gold_toto_" + x + "_" + y + ".sav");

        baseSave[0x2056] = (byte) 58;  // igt second
        baseSave[0x2057] = (byte) igt; // igt frame

        int sc = (int) (baseSave[0x2825]);
        int psc = (int) (baseSave[0x2826]);
        baseSave[0x2825] = (byte) (sc + momStep); // StepCount
        baseSave[0x2826] = (byte) ((psc + momStep) % 4); // PoisonStepCount

        baseSave[0x2000] = audio;
        baseSave[0x2002] = frameType;
        baseSave[0x2005] = menuAccount;

        baseSave[0x2045] = hour; // StartHour
        baseSave[0x2046] = minute; // StartMinute
        baseSave[0x2047] = 0;  // StartSecond

        int checksum = 0;
        for(int i = 0x2009; i < 0x2d69; i++) {
            checksum += (baseSave[i] & 0xff);
        }
        baseSave[0x2d69] = (byte) ((checksum) & 0xff);
        baseSave[0x2d6a] = (byte) ((checksum >> 8) & 0xff);

        File.WriteAllBytes("roms/pokegold.sav", baseSave);
    }

    public static GscPokemon Textboxes(Gsc gb) {
        gb.InjectOverworld(Joypad.A);
        gb.AdvanceFrame(Joypad.A);
        gb.Press(Joypad.B);
        gb.ClearText(Joypad.B);
        gb.Press(Joypad.A);
        gb.ClearText(Joypad.B, 3);
        gb.Hold(Joypad.B, "CalcMonStats");
        return gb.PartyMon1;
    }

    public static void Test(byte x, byte y, byte hour, byte minute, byte momStep, byte audio, byte frameType, byte menuAccount, byte igt, GscIntroSequence intro, string path, string recordName = "") {
        MakeSave(x, y, hour, minute, momStep, audio, frameType, menuAccount, igt);
        Gold gb = new Gold(true);
        if(recordName != "") gb.Record(recordName);
        gb.SetTimeSec(120);
        intro.ExecuteUntilIGT(gb);
        gb.CpuWrite("wGameTimeFrames", igt);
        gb.Execute(path);
        ushort dvs = Textboxes(gb).DVs;
        Console.WriteLine("0x{0:x4}", dvs);
        gb.Dispose();
    }

    public static void EvaluateTototdile(Gold gb, DFState<GscTile> state) {
        GscPokemon toto = Textboxes(gb);
        int atk = toto.DVs.Attack;
        int def = toto.DVs.Defense;
        int spd = toto.DVs.Speed;
        int spc = toto.DVs.Special;

        int stars = 0;

        if(atk == 15) stars += 2;
        else if(atk >= 12) stars++;
        else if(atk <= 7) stars -= 3;
        else if(atk <= 8) stars -= 2;
        else if(atk <= 9) stars--;

        if(def >= 13) stars += 2;
        else if(def >= 10) stars++;
        else if(def <= 5) stars -= 3;
        else stars--;

        if(spd == 15) stars += 2;
        else if(spd >= 11) stars++;
        else if(spd <= 3) stars -= 3;
        else if(spd <= 7) stars -= 2;
        else if(spd <= 9) stars--;

        if(spc == 15) stars += 2;
        else if(spc >= 14) stars++;
        else if(spc <= 7) stars -= 3;
        else if(spc <= 9) stars -= 2;
        else if(spc <= 11) stars--;

        string starsStr;
        if(stars == 8) {
            starsStr = "[***]";
        } else if(stars >= 6) {
            starsStr = "[ **]";
        } else if(stars >= 4) {
            starsStr = "[  *]";
        } else {
            starsStr = "[   ]";
        }

        if(stars >= 8) {
            lock(Writer) {
                Writer.WriteLine("{0} [{1} cost] {2}- {3:x4}", starsStr, state.WastedFrames, state.Log, toto.DVs);
                Writer.Flush();
            }
        }
    }

    public static void StartSearch(int numThreads) {
        bool[] threadsRunning = new bool[numThreads];
        Thread[] threads = new Thread[numThreads];
        Gold dummyGb = new Gold(true);
        GscMap map = dummyGb.Maps[6149];
        map.Sprites.Remove(5, 3); // Remove police officer (https://gunnermaniac.com/pokeworld2?map=6149#5/3)
        Pathfinding.GenerateEdges(map, 0, 17, map.Tileset.LandPermissions, Action.Right | Action.Down | Action.StartB, map[7, 5]);
        Pathfinding.GenerateEdges(map, 1, 17, map.Tileset.LandPermissions, Action.StartB, map[7, 4]);

        map[4, 5].RemoveEdge(0, Action.Down);
        map[4, 5].RemoveEdge(0, Action.Down | Action.A); // Don't walk into cutscene
        map[5, 5].RemoveEdge(0, Action.Down);
        map[5, 5].RemoveEdge(0, Action.Down | Action.A); // ^
        map[7, 4].RemoveEdge(0, Action.Right);
        map[7, 4].RemoveEdge(0, Action.Right | Action.A);
        map[7, 5].RemoveEdge(0, Action.Right);
        map[7, 5].RemoveEdge(0, Action.Right | Action.A);
        map[4, 2].RemoveEdge(0, Action.Down | Action.A);
        map[5, 3].RemoveEdge(0, Action.Down | Action.A);

        map[7, 5].AddEdge(0, new Edge<GscTile>() {
            Action = Action.Up,
            NextTile = map[7, 4],
            NextEdgeset = 1,
            Cost = 0,
        });

        GscTile[] startTiles = { map[4, 2], map[4, 3], map[4, 4], map[5, 3], map[5, 4] };
        byte[] startHours = { 10, 8, 2, 18 };
        byte[] startMinutes = { 51, 59 };
        byte[] audios = { 0xc1, 0xe1 };

        int numSavesCompleted = 0;

        Writer = new StreamWriter("gold_toto_" + DateTime.Now.Ticks + ".txt");
        int numSaves = startTiles.Length * startHours.Length * startMinutes.Length * 2 * 2 * 8 * 2 * 10;
        Console.WriteLine(numThreads + " threads, " + (numSaves) + " saves (" + (float) numSaves / (float) numThreads + " iterations)");
        foreach(GscTile tile in startTiles) {
            foreach(byte hour in startHours) {
                foreach(byte minute in startMinutes) {
                    for(byte momStep = 0; momStep <= 1; momStep++) {
                        foreach(byte audio in audios) {
                            for(byte frameType = 0; frameType <= 7; frameType++) {
                                for(byte menuAccount = 0; menuAccount <= 1; menuAccount++) {
                                    for(byte igt = 0; igt < 60; igt += 6) {
                                        int threadIndex;
                                        while((threadIndex = Array.IndexOf(threadsRunning, false)) == -1) {
                                            Thread.Sleep(50);
                                        }
                                        threadsRunning[threadIndex] = true;
                                        new Thread(parameter => {
                                            (int, (GscTile, byte, byte, byte, byte, byte, byte, byte)) data = ((int, (GscTile, byte, byte, byte, byte, byte, byte, byte))) parameter;
                                            (GscTile tile, byte hour, byte minute, byte momStep, byte audio, byte frameType, byte menuAccount, byte igt) state = data.Item2;
                                            Gold gb;
                                            lock(startTiles) {
                                                MakeSave(state.tile.X, state.tile.Y, state.hour, state.minute, state.momStep, state.audio, state.frameType, state.menuAccount, state.igt);
                                                gb = new Gold(true);
                                                gb.SetTimeSec(120);
                                                gb.Hold(Joypad.Left, 0x100);
                                            }
                                            GscStrat.GfSkip.Execute(gb);
                                            GscStrat.TitleSkip.Execute(gb);
                                            byte[] mmbackState = gb.SaveState();
                                            for(int mmBack = 0; mmBack <= 3; mmBack++) {
                                                GscStrat.Continue.Execute(gb);
                                                byte[] fsbackState = gb.SaveState();
                                                for(int fsBack = 0; fsBack <= 3; fsBack++) {
                                                    gb.Hold(Joypad.Left, "GetJoypad");
                                                    gb.AdvanceFrame(Joypad.Left);
                                                    byte[] delayState = gb.SaveState();
                                                    for(int delay = 0; delay <= MaxCost; delay++) {
                                                        int introCost = mmBack * 83 + fsBack * 101 + delay;
                                                        if(introCost > MaxCost) break;
                                                        gb.Hold(Joypad.A, "OWPlayerInput");
                                                        DFParameters<Gold, GscTile> parameters = new DFParameters<Gold, GscTile>() {
                                                            NoEncounterSS = 1,
                                                            PruneAlreadySeenStates = false,
                                                            MaxCost = MaxCost - introCost,
                                                            LogStart = string.Format("(x={0}, y={1}, h={2}, m={3}, momStep={4}, audio={5:x02}, frameType={6}, menuAccount={7}, igt={8}, mmback={9}, fsback={10}, delay={11}) ",
                                                                            state.tile.X, state.tile.Y, state.hour, state.minute, state.momStep, state.audio, state.frameType, state.menuAccount, state.igt, mmBack, fsBack, delay),
                                                            EndTiles = new GscTile[] { map[7, 4] },
                                                            EndEdgeSet = 1,
                                                            FoundCallback = state => EvaluateTototdile(gb, state),
                                                        };
                                                        DepthFirstSearch.StartSearch(new Gold[] { gb }, parameters, tile, 0, new byte[][] { gb.SaveState() });
                                                        gb.LoadState(delayState);
                                                        gb.AdvanceFrame(Joypad.Left);
                                                        delayState = gb.SaveState();
                                                    }
                                                    gb.LoadState(fsbackState);
                                                    GscStrat.FsBack.Execute(gb);
                                                    GscStrat.Continue.Execute(gb);
                                                    fsbackState = gb.SaveState();
                                                }
                                                gb.LoadState(mmbackState);
                                                GscStrat.MmBack.Execute(gb);
                                                GscStrat.TitleSkip.Execute(gb);
                                                mmbackState = gb.SaveState();
                                            }
                                            Interlocked.Increment(ref numSavesCompleted);
                                            Console.WriteLine("Completed save " + numSavesCompleted + "/" + numSaves);
                                            threadsRunning[threadIndex] = false;
                                        }).Start((threadIndex, (tile, hour, minute, momStep, audio, frameType, menuAccount, igt)));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        while(true) {
            Thread.Sleep(10000);
        }
    }
}