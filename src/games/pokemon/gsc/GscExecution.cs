using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

public partial class Gsc {

    public override void Press(params Joypad[] joypads) {
        for(int i = 0; i < joypads.Length; i++) {
            Joypad joypad = joypads[i];
            if(PC == (SYM["OWPlayerInput"] & 0xffff)) {
                InjectOverworld(joypad);
                Hold(joypad, "GetJoypad");
            } else {
                Hold((Joypad) CpuRead("hJoyLast"), SYM["GetJoypad"] + 0x3);
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
                    ret = Hold(input, "CountStep", "RandomEncounter.ok", "PrintLetterDelay.checkjoypad", "DoPlayerMovement.BumpSound");
                    if(ret == SYM["CountStep"]) {
                        ret = Hold(input, "OWPlayerInput", "RandomEncounter.ok");
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
                case Action.Select:
                    InjectOverworld(Joypad.Select);
                    AdvanceFrame(Joypad.Select);
                    ret = Hold(Joypad.Select, "OWPlayerInput");
                    break;
                default:
                    break;
            }
        }

        return ret;
    }

    public override int ClearText(Joypad holdInput, int numTextBoxes, params int[] additionalBreakpoints) {
        int[] breakpoints = new int[additionalBreakpoints.Length + 1];
        breakpoints[0] = SYM["GetJoypad"];
        Array.Copy(additionalBreakpoints, 0, breakpoints, 1, additionalBreakpoints.Length);

        // A list of routines that prompt the user to advance the text with either A or B.
        int[] textAdvanceAddrs = {
            SYM["PromptButton.input_wait_loop"] + 0x6,
            SYM["WaitPressAorB_BlinkCursor.loop"] + 0xb,
            SYM["JoyWaitAorB.loop"] + 0x6,
            SYM["TextCommand_PAUSE"] + 0x5,
        };

        int stackPointer;
        int[] stack = new int[2];

        int clearCounter = 0;

        int ret = 0;
        while(true && clearCounter < numTextBoxes) {
            // Hold the specified input until the joypad state is polled.
            ret = Hold(holdInput, breakpoints);

            if(ret != SYM["GetJoypad"]) {
                break;
            }

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
                RunFor(1);
            } else if(stack.Intersect(textAdvanceAddrs).Any()) {
                // One of the 'textAdvanceAddrs' has been hit, clear the text box with the opposite button used in the previous frame.
                byte previous = (byte) (CpuRead("hJoyDown") & (byte) (Joypad.A | Joypad.B));
                Joypad advance = previous == 0 ? Joypad.A   // If neither A or B have been pressed on the previous frame, default to clear the text box with A.
                                               : (Joypad) (previous ^ 0x3); // Otherwise clear with the opposite button. This is achieved by XORing the value by 3.
                                                                            // (Joypad.A) 01 xor 11 = 10 (Joypad.B)
                                                                            // (Joypad.B) 10 xor 11 = 01 (Joypad.A)
                Inject(advance);
                RunFor(1);
                clearCounter++;
            } else {
                // If the call originated from 'HandleMapTimeAndJoypad' and there is currently a sprite being moved by a script, don't break.
                if(stack[0] == (SYM["HandleMapTimeAndJoypad"] & 0xffff) + 0xc && CpuRead("wScriptMode") == 2) {
                    RunFor(1);
                } else {
                    RunFor(1);
                    break;
                }
            }
        }

        return ret;
    }

    public override int MoveTo(int targetX, int targetY, Action preferredDirection = Action.None) {
        throw new NotImplementedException();
    }

    public void SetTimeSec(int timesec) {
        DetailedState state = SaveDetailedState();
        state.Seconds = (uint) timesec;
        LoadDetailedState(state);
    }

    public IGTState MakeIGTState(GscIntroSequence intro, byte[] initialState, int igt) {
        LoadState(initialState);
        CpuWrite("wGameTimeSeconds", (byte) (igt / 60));
        CpuWrite("wGameTimeFrames", (byte) (igt % 60));
        intro.ExecuteAfterIGT(this);
        return new IGTState(this, true, igt);
    }

    public IGTResults IGTCheck(int timesec, GscIntroSequence intro, int numIgts, Func<bool> fn = null, int ss = 0, int igtOffset = 0) {
        SetTimeSec(timesec);
        intro.ExecuteUntilIGT(this);
        byte[] igtState = SaveState();
        IGTResults introStates = new IGTResults(numIgts);
        for(int i = 0; i < numIgts; i++) {
            introStates[i] = MakeIGTState(intro, igtState, i + igtOffset);
        }

        return IGTCheck(introStates, fn, ss);
    }

    public static IGTResults IGTCheckParallel<Gb>(Gb[] gbs, int timesec, GscIntroSequence intro, int numIgts, Func<Gb, bool> fn = null, int ss = 0, int igtOffset = 0) where Gb : Gsc {
        gbs[0].SetTimeSec(timesec);
        intro.ExecuteUntilIGT(gbs[0]);
        byte[] igtState = gbs[0].SaveState();
        IGTResults introStates = new IGTResults(numIgts);
        MultiThread.For(numIgts, gbs, (gb, i) => {
            introStates[i] = gb.MakeIGTState(intro, igtState, i + igtOffset);
        });

        return IGTCheckParallel(gbs, introStates, x => fn == null || fn((Gb) x), ss);
    }

    public static IGTResults IGTCheckParallel<Gb>(int numThreads, int timesec, GscIntroSequence intro, int numIgts, Func<Gb, bool> fn = null, int ss = 0) where Gb : Gsc {
        return IGTCheckParallel(MultiThread.MakeThreads<Gb>(numThreads), timesec, intro, numIgts, fn, ss);
    }

    public static string CleanUpPathParallel<Gb>(Gb[] gbs, IGTResults initialStates, int ss, params Action[] path) where Gb : Gsc {
        List<int> aPressIndices = new List<int>();
        for(int i = 0; i < path.Length; i++) {
            if((path[i] & Action.A) > 0) aPressIndices.Add(i);
        }

        foreach(int index in aPressIndices) {
            path[index] &= ~Action.A;
            int successes = Gsc.IGTCheckParallel(gbs, initialStates, gb => gb.Execute(path) == gb.SYM["OWPlayerInput"]).TotalSuccesses;
            if(successes < ss) {
                path[index] |= Action.A;
            }
        }

        return ActionFunctions.ActionsToPath(path);
    }
}