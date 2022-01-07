using System;
using System.Linq;

public partial class Rby {

    public override void Inject(Joypad joypad) {
        CpuWrite("hJoyInput", (byte) joypad);
    }

    public override void Press(params Joypad[] joypads) {
        while((CpuRead("wd730") & 0x20) > 0) AdvanceFrame();
        foreach(Joypad joypad in joypads) {
            RunUntil("_Joypad");
            Inject(joypad);
            AdvanceFrame();
        }
    }

    public override int Execute(params Action[] actions) {
        return Execute(actions, new (RbyTile, System.Action)[0]);
    }

    public int Execute(string path, params (RbyTile, System.Action)[] tileCallbacks) {
        return Execute(path.Split(" ").Select(a => a.ToAction()).ToArray(), tileCallbacks);
    }

    public virtual int Execute(Action[] actions, params (RbyTile Tile, System.Action Function)[] tileCallbacks) {
        int ret = 0;

        foreach(Action action in actions) {
            switch(action) {
                case Action.Left:
                case Action.Right:
                case Action.Up:
                case Action.Down:
                    Joypad joypad = (Joypad) action;
                    do {
                        RunUntil("JoypadOverworld");
                        Inject(joypad);
                        ret = Hold(joypad, SYM["HandleLedges.foundMatch"], SYM["CollisionCheckOnLand.collision"], SYM["CollisionCheckOnWater.collision"], SYM["TryDoWildEncounter.CanEncounter"] + 6, SYM["OverworldLoopLessDelay.newBattle"] + 3);
                        if(ret == SYM["TryDoWildEncounter.CanEncounter"] + 6) {
                            return RunUntil("CalcStats");
                        } else if(ret == SYM["CollisionCheckOnLand.collision"] || ret == SYM["CollisionCheckOnWater.collision"]) {
                            return ret;
                        }

                        ret = RunUntil("JoypadOverworld");
                    } while(((CpuRead("wd736") & 0x40) != 0) || ((CpuRead("wd736") & 0x2) != 0 && CpuRead("wJoyIgnore") > 0xfc) || ((CpuRead("wd730") & 0x80) > 0));

                    RbyTile tile = Tile;
                    foreach(var callback in tileCallbacks) {
                        if(callback.Tile == tile) {
                            callback.Function();
                        }
                    }

                    break;
                case Action.A:
                    Inject(Joypad.A);
                    RunFor(1);
                    ret = Hold(Joypad.A, "JoypadOverworld", "PrintLetterDelay");
                    break;
                case Action.StartB:
                    Press(Joypad.Start, Joypad.B);
                    ret = RunUntil("JoypadOverworld");
                    break;
                case Action.PokedexFlash:
                    Press(Joypad.Start, Joypad.A, Joypad.B, Joypad.Start);
                    ret = RunUntil("JoypadOverworld");
                    break;
                default:
                    Debug.Assert(false, "Unknown Action: {0}", action);
                    break;
            }
        }

        return ret;
    }

    public virtual bool Yoloball(int ballSlot = 0) {
        throw new NotImplementedException();
    }

    public void PickupItem() {
        Inject(Joypad.A);
        Hold(Joypad.A, SYM["PlaySound"]);
    }

    public override int ClearText(Joypad holdInput, int numTextBoxes, params int[] additionalBreakpoints) {
        int[] breakpoints = new int[additionalBreakpoints.Length + 1];
        breakpoints[0] = SYM["Joypad"];
        Array.Copy(additionalBreakpoints, 0, breakpoints, 1, additionalBreakpoints.Length);

        int[] textAddrs = {
            SYM["PrintLetterDelay.checkButtons"] + 0x3,
            SYM["WaitForTextScrollButtonPress.skipAnimation"] + 0xa,
            SYM["HoldTextDisplayOpen"] + 0x3,
            (SYM["ShowPokedexDataInternal.waitForButtonPress"] & 0xffff) + 0x3,
            SYM["TextCommand_PAUSE"] + 0x4,
        };

        int cameFrom;
        int stackPointer;

        int clearCounter = 0;

        int ret = 0;

        while(true && clearCounter < numTextBoxes) {
            // Hold the specified input until the joypad state is polled.
            ret = Hold(holdInput, breakpoints);

            if(ret != SYM["Joypad"]) {
                break;
            }

            // Read the current position of the stack.
            stackPointer = Registers.SP;
            // When the 'Joypad' routine is called, the address of the following instruction is pushed onto the stack.
            // By reading the 2 highest bytes from the stack, it is possible to figure out from where the 'Joypad' call originated.
            // This will be used to make decisions later on.
            cameFrom = CpuReadLE<ushort>(stackPointer);

            // Some routines call 'JoypadLowSensitivity' (which then calls 'Joypad'), so the next 2 bytes from the stack have to be read to find the origin of the call.
            if(cameFrom == SYM["JoypadLowSensitivity"] + 0x3) {
                cameFrom = CpuReadLE<ushort>(stackPointer + 2);
            }

            // If the call did not originate from any of the text handling routines, it may be time to break out of the loop.
            if(Array.IndexOf(textAddrs, cameFrom) == -1) {
                // If the call originated from 'JoypadOverworld', additional criteria have to be met to warrent a break.
                if(cameFrom == SYM["JoypadOverworld"] + 0xd && (CpuRead("wJoyIgnore") > 0xfb ||      // (1) More Buttons than just A and B have to be allowed,
                                                               (CpuRead("wd730") & 0xa1) > 0 ||      // (2) No sprite can currently be moved by a script,
                                                               (CpuRead("wFlags_D733") & 0x8) > 0 || // (3) Joypad input must not be ignored,
                                                                CpuRead("wCurOpponent") > 0)) {      // (4) Joypad states can not be simulated (player is in a cutscene)
                                                                                                     // (5) The player must not be currently engaged by a trainer
                    AdvanceFrame();
                } else {
                    // If it is time to break out of the loop, run for 1 sample to allow for continuous calls of this function.
                    RunFor(1);
                    break;
                }
            }

            if(cameFrom == textAddrs[0]) {
                // If the call originated from 'PrintLetterDelay', advance a frame with the specified button to hold.
                Inject(holdInput);
                RunFor(1);
            } else {
                // If the call did not originate from 'PrintLetterDelay', advance the textbox with the opposite button used in the previous frame.
                byte previous = (byte) (CpuRead("hJoyLast") & (byte) (Joypad.A | Joypad.B));
                Joypad advance;
                if(previous == 0) advance = cameFrom == textAddrs[1] ? Joypad.B : Joypad.A; // If neither A or B have been pressed on the previous frame, clear the textbox with B if it's a "60 fps" textbox.
                else advance = (Joypad) (previous ^ 0x3); // Otherwise clear with the opposite button. This is achieved by XORing the value by 3.
                                                          // (Joypad.A) 01 xor 11 = 10 (Joypad.B)
                                                          // (Joypad.B) 10 xor 11 = 01 (Joypad.A)
                Inject(advance);
                RunFor(1);
                clearCounter++;
            }
        }

        return ret;
    }

    public override int MoveTo(int targetX, int targetY, Action preferredDirection = Action.None) {
        throw new NotImplementedException();
    }

    public void BattleMenu(int x, int y) {
        if(CpuRead("wIsInBattle") > 0) {
            byte xMenu = CpuRead("wTopMenuItemX");
            byte yMenu = CpuRead("wCurrentMenuItem");
            Joypad j=Joypad.None;

            if(x == 0 && xMenu != 0x9) j |= Joypad.Left;
            else if(x == 1 && xMenu != 0xf) j |= Joypad.Right;
            if(y == 0 && yMenu != 0) j |= Joypad.Up;
            else if(y == 1 && yMenu != 1) j |= Joypad.Down;

            if(!IsYellow && (j & (Joypad.Left | Joypad.Right)) == 0 || j==Joypad.None)
                MenuPress(j | Joypad.A); // in red/blue, we can down+A or up+A
            else {
                MenuPress(j);
                MenuPress(Joypad.A);
            }
        }
    }

    public virtual void ChooseMenuItem(int target, Joypad direction = Joypad.None) {
        throw new NotImplementedException();
    }

    public virtual void SelectMenuItem(int target, Joypad direction = Joypad.None) {
        throw new NotImplementedException();
    }

    public virtual void ChooseListItem(int target, Joypad direction = Joypad.None) {
        throw new NotImplementedException();
    }

    public virtual void SelectListItem(int target, Joypad direction = Joypad.None) {
        throw new NotImplementedException();
    }

    public void MenuScroll(int target, Joypad clickInput, bool clickWithScroll) {
        var scroll = CalcScroll(target, CpuRead("wCurrentMenuItem"), CpuRead("wMaxMenuItem"), CpuRead("wMenuWrappingEnabled") > 0);

        if(clickWithScroll) {
            scroll.Amount--;
            clickInput |= scroll.Input;
        }

        for(int i = 0; i < scroll.Amount; i++) {
            MenuPress(scroll.Input);
        }

        MenuPress(clickInput);
    }

    public void ListScroll(int target, Joypad clickInput, bool clickWithScroll) {
        var scroll = CalcScroll(target, CpuRead("wCurrentMenuItem") + CpuRead("wListScrollOffset"), CpuRead("wListCount"), false);

        for(int i = 0; i < scroll.Amount - 1; i++) {
            MenuPress(scroll.Input | (Joypad) (((i & 1) + 1) << 4), true);
        }

        byte menuItem = CpuRead("wCurrentMenuItem");
        bool canClickWithScroll = clickWithScroll && (menuItem == 1 || (menuItem == 0 && scroll.Input == Joypad.Down) || (menuItem == 2 && scroll.Input == Joypad.Up));

        if(scroll.Amount == 0) {
            MenuPress(clickInput | Joypad.Left, true);
        } else {
            if(canClickWithScroll) {
                MenuPress(scroll.Input | clickInput, true);
            } else {
                MenuPress(scroll.Input | Joypad.Start, true);
                MenuPress(clickInput, true);
            }
        }
    }

    public IGTState MakeIGTState(RbyIntroSequence intro, byte[] initialState, int igt) {
        LoadState(initialState);
        CpuWrite("wPlayTimeSeconds", (byte) (igt % 60));
        CpuWrite("wPlayTimeFrames", (byte) igt);
        intro.ExecuteAfterIGT(this);
        return new IGTState(this, true, igt);
    }

    public IGTResults IGTCheck(RbyIntroSequence intro, int numIgts, Func<bool> fn = null, int ss = 0, int igtOffset = 0) {
        intro.ExecuteUntilIGT(this);
        byte[] igtState = SaveState();
        IGTResults introStates = new IGTResults(numIgts);
        for(int i = 0; i < numIgts; i++) {
            introStates[i] = MakeIGTState(intro, igtState, i + igtOffset);
        }

        return IGTCheck(introStates, fn, ss);
    }

    public static IGTResults IGTCheckParallel<Gb>(Gb[] gbs, RbyIntroSequence intro, int numIgts, Func<Gb, bool> fn = null, int ss = 0, int igtOffset = 0) where Gb : Rby {
        intro.ExecuteUntilIGT(gbs[0]);
        byte[] igtState = gbs[0].SaveState();
        IGTResults introStates = new IGTResults(numIgts);
        MultiThread.For(numIgts, gbs, (gb, i) => {
            introStates[i] = gb.MakeIGTState(intro, igtState, i + igtOffset);
        });

        return IGTCheckParallel(gbs, introStates, x => fn == null || fn((Gb) x), ss);
    }

    public static IGTResults IGTCheckParallel<Gb>(int numThreads, RbyIntroSequence intro, int numIgts, Func<Gb, bool> fn = null, int ss = 0) where Gb : Rby {
        return IGTCheckParallel(MultiThread.MakeThreads<Gb>(numThreads), intro, numIgts, fn, ss);
    }
}
