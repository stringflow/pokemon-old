using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

public partial class Gsc {

    public override void Press(params Joypad[] joypads) {
        for(int i = 0; i < joypads.Length; i++) {
            Joypad joypad = joypads[i];
            if(Registers.PC == (SYM["OWPlayerInput"] & 0xffff)) {
                InjectOverworld(joypad);
                Hold(joypad, "GetJoypad");
            } else {
                Hold(joypad, SYM["GetJoypad"] + 0x7);
                Inject(joypad);
                AdvanceFrame(joypad);
            }
        }
    }

    public override void Inject(Joypad joypad) {
        CpuWrite("hJoypadDown", (byte) joypad);
    }

    public void InjectOverworld(Joypad joypad) {
        CpuWrite("hJoyPressed", (byte) joypad);
        CpuWrite("hJoyDown", (byte) joypad);
    }

    public override int Execute(params Action[] actions) {
        int ret = 0;
        foreach(Action action in actions) {
            switch(action & ~Action.A) {
                case Action.Right:
                case Action.Left:
                case Action.Up:
                case Action.Down:
                    Joypad input = (Joypad) action;
                    RunUntil("OWPlayerInput");
                    InjectOverworld(input);
                    ret = Hold(input, "CountStep", "ChooseWildEncounter.startwildbattle", "PrintLetterDelay.checkjoypad", "DoPlayerMovement.BumpSound");
                    if(ret == SYM["CountStep"]) {
                        ret = Hold(input, "OWPlayerInput", "ChooseWildEncounter.startwildbattle");
                    }

                    if(ret != SYM["OWPlayerInput"]) {
                        return ret;
                    }

                    InjectOverworld(Joypad.None);
                    break;
                case Action.StartB:
                    InjectOverworld(Joypad.Start);
                    AdvanceFrame(Joypad.Start);
                    Hold(Joypad.B, "GetJoypad");
                    Inject(Joypad.B);
                    ret = Hold(Joypad.B, "OWPlayerInput");
                    break;
                default:
                    break;
            }
        }

        return ret;
    }

    public override void ClearText(Joypad holdInput, int numTextBoxes) {
        // A list of routines that prompt the user to advance the text with either A or B.
        int[] textAdvanceAddrs = {
            SYM["PromptButton.input_wait_loop"] + 0x6,
            SYM["WaitPressAorB_BlinkCursor.loop"] + 0xb,
            SYM["JoyWaitAorB.loop"] + 0x6
        };

        int stackPointer;
        int[] stack = new int[2];

        int clearCounter = 0;

        while(true && clearCounter < numTextBoxes) {
            // Hold the specified input until the joypad state is polled.
            Hold(holdInput, "GetJoypad");

            // Read the current position of the stack.
            stackPointer = Registers.SP;

            // Every time a routine gets called, the address of the following instruction gets pushed on the stack (to then be jumped to once the routine call returns).
            // To figure out where the 'GetJoypad' call originated from, we use the top two addresses of the stack.
            for(int i = 0; i < stack.Length; i++) {
                stack[i] = CpuReadLE<ushort>(stackPointer + i * 2);
            }

            // 'PrintLetterDelay' directly calls 'GetJoypad', therefore it will always be on the top of the stack.
            if(stack[0] == SYM["PrintLetterDelay.checkjoypad"] + 0x3) {
                // If the 'GetJoypad' call originated from PrintLetterDelay, use the 'hold' input to advance a frame.
                Inject(holdInput);
                AdvanceFrame(holdInput);
            } else if(stack.Intersect(textAdvanceAddrs).Any()) {
                // One of the 'textAdvanceAddrs' has been hit, clear the text box with the opposite button used in the previous frame.
                byte previous = (byte) (CpuRead("hJoyDown") & (byte) (Joypad.A | Joypad.B));
                Joypad advance = previous == 0 ? Joypad.A   // If neither A or B have been pressed on the previous frame, default to clear the text box with A.
                                               : (Joypad) (previous ^ 0x3); // Otherwise clear with the opposite button. This is achieved by XORing the value by 3.
                                                                            // (Joypad.A) 01 xor 11 = 10 (Joypad.B)
                                                                            // (Joypad.B) 10 xor 11 = 01 (Joypad.A)
                Inject(advance);
                AdvanceFrame(advance);
                clearCounter++;
            } else {
                // If the call originated from 'HandleMapTimeAndJoypad' and there is currently a sprite being moved by a script, don't break.
                if(stack[0] == (SYM["HandleMapTimeAndJoypad"] & 0xffff) + 0xc && CpuRead("wScriptMode") == 2) {
                    AdvanceFrame();
                } else {
                    break;
                }
            }
        }
    }

    public override int WalkTo(int targetX, int targetY) {
        GscMap map = Map;
        GscTile current = Tile;
        GscTile target = map[targetX, targetY];
        GscWarp warp = map.Warps[current.X, current.Y];
        bool original = false;
        if(warp != null) {
            original = warp.Allowed;
            warp.Allowed = true;
        }
        List<Action> path = Pathfinding.FindPath(map, current, 17, map.Tileset.LandPermissions, target);
        if(warp != null) {
            warp.Allowed = original;
        }
        return Execute(path.ToArray());
    }

    public void SetTimeSec(int timesec) {
        byte[] state = SaveState();
        state[SaveStateLabels["timesec"] + 0] = (byte) (timesec >> 24);
        state[SaveStateLabels["timesec"] + 1] = (byte) (timesec >> 16);
        state[SaveStateLabels["timesec"] + 2] = (byte) (timesec >> 8);
        state[SaveStateLabels["timesec"] + 3] = (byte) (timesec & 0xff);
        LoadState(state);
    }

    public byte[] MakeIGTState(GscIntroSequence intro, byte[] initialState, int igt) {
        LoadState(initialState);
        CpuWrite("wGameTimeSeconds", (byte) (igt / 60));
        CpuWrite("wGameTimeFrames", (byte) (igt % 60));
        intro.ExecuteAfterIGT(this);
        return SaveState();
    }

    public IGTResults IGTCheck(int timesec, GscIntroSequence intro, int numIgts, Func<GameBoy, bool> fn = null, int ss = 0, int ssOverwrite = -1) {
        SetTimeSec(timesec);
        intro.ExecuteUntilIGT(this);
        byte[] igtState = SaveState();
        byte[][] states = new byte[numIgts][];
        for(int i = 0; i < numIgts; i++) {
            states[i] = MakeIGTState(intro, igtState, i);
        }

        return IGTCheck(states, fn, ss, ssOverwrite);
    }

    public static IGTResults IGTCheckParallel<Gb>(Gb[] gbs, int timesec, GscIntroSequence intro, int numIgts, Func<GameBoy, bool> fn = null, int ss = 0, int ssOverwrite = -1) where Gb : Gsc {
        gbs[0].SetTimeSec(timesec);
        intro.ExecuteUntilIGT(gbs[0]);
        byte[] igtState = gbs[0].SaveState();
        byte[][] states = new byte[numIgts][];
        MultiThread.For(numIgts, gbs, (gb, i) => {
            states[i] = gb.MakeIGTState(intro, igtState, i);
        });

        return IGTCheckParallel(gbs, states, fn, ss, ssOverwrite);
    }

    public static IGTResults IGTCheckParallel<Gb>(int numThreads, int timesec, GscIntroSequence intro, int numIgts, Func<GameBoy, bool> fn = null, int ss = 0, int ssOverwrite = -1) where Gb : Gsc {
        return IGTCheckParallel(MultiThread.MakeThreads<Gb>(numThreads), timesec, intro, numIgts, fn, ss, ssOverwrite);
    }

    public static string CleanUpPathParallel<Gb>(Gb[] gbs, byte[][] states, int ss, params Action[] path) where Gb : Gsc {
        List<int> aPressIndices = new List<int>();
        for(int i = 0; i < path.Length; i++) {
            if((path[i] & Action.A) > 0) aPressIndices.Add(i);
        }

        foreach(int index in aPressIndices) {
            path[index] &= ~Action.A;
            int successes = IGTCheckParallel(gbs, states, gb => gb.Execute(path) == gb.SYM["OWPlayerInput"]).TotalSuccesses;
            if(successes < ss) {
                path[index] |= Action.A;
            }
        }

        return ActionFunctions.ActionsToPath(path);
    }
}