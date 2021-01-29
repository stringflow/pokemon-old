using System;
using System.Collections.Generic;

public partial class Rby {

    public override void Inject(Joypad joypad) {
        CpuWrite("hJoyInput", (byte) joypad);
    }

    public override void Press(params Joypad[] joypads) {
        foreach(Joypad joypad in joypads) {
            RunUntil("_Joypad");
            Inject(joypad);
            AdvanceFrame();
        }
    }

    public override int Execute(params Action[] actions) {
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
                    } while((CpuRead("wd736") & 0x42) != 0 && CpuRead("wJoyIgnore") < 0xfc);
                    break;
                case Action.A:
                    Inject(Joypad.A);
                    AdvanceFrame(Joypad.A);
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
                case Action.Delay:
                    Inject(Joypad.None);
                    RunUntil("OverworldLoop");
                    ret = RunUntil("JoypadOverworld");
                    break;
                default:
                    Debug.Assert(false, "Unknown Action: {0}", action);
                    break;
            }
        }

        return ret;
    }

    public void PickupItem() {
        Inject(Joypad.A);
        Hold(Joypad.A, SYM["PlaySound"]);
        RunUntil("JoypadOverworld");
    }

    public override void ClearText(Joypad holdInput, int numTextBoxes) {
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

        while(true && clearCounter < numTextBoxes) {
            // Hold the specified input until the joypad state is polled.
            Hold(holdInput, "Joypad");
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
                if(cameFrom == SYM["JoypadOverworld"] + 0xd && (CpuRead("wJoyIgnore") > 0xfb ||    // (1) More Buttons than just A and B have to be allowed,
                                                                (CpuRead("wd730") & 0xa1) > 0)) {  // (2) No sprite can currently be moved by a script,
                                                                                                   // (3) Joypad input must not be ignored,
                                                                                                   // (4) Joypad states can not be simulated (player is in a cutscene)
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
                AdvanceFrame(holdInput);
            } else {
                // If the call did not originate from 'PrintLetterDelay', advance the textbox with the opposite button used in the previous frame.
                byte previous = (byte) (CpuRead("hJoyLast") & (byte) (Joypad.A | Joypad.B));
                Joypad advance = previous == 0 ? Joypad.A                   // If neither A or B have been pressed on the previous frame, default to clear the text box with A.
                                               : (Joypad) (previous ^ 0x3); // Otherwise clear with the opposite button. This is achieved by XORing the value by 3.
                                                                            // (Joypad.A) 01 xor 11 = 10 (Joypad.B)
                                                                            // (Joypad.B) 10 xor 11 = 01 (Joypad.A)
                Inject(advance);
                AdvanceFrame(advance);
                clearCounter++;
            }
        }
    }

    public override int WalkTo(int targetX, int targetY) {
        RbyMap map = Map;
        RbyTile current = map[XCoord, YCoord];
        RbyTile target = map[targetX, targetY];
        RbyWarp warp = map.Warps[XCoord, YCoord];
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

    public void BattleMenu(int x, int y) {
        if(CpuRead("wIsInBattle") > 0) {
            byte xMenu = CpuRead("wTopMenuItemX");
            if(x == 0 && xMenu != 0x9) MenuPress(Joypad.Left);
            else if(x == 1 && xMenu != 0xff) MenuPress(Joypad.Right);
            ChooseMenuItem(y);
        }
    }

    public void PartySwap(int x, int y) {
        ChooseMenuItem(x);
        ChooseMenuItem(1);
        ChooseMenuItem(y);
    }

    public void ItemSwap(int x, int y) {
        BattleMenu(0, 1);
        SelectListItem(x);
        SelectListItem(y);
    }

    public void UseItem(string item, int target = -1) {
        UseItem(Items[item], target);
    }

    public void UseItem(RbyItem item, int target) {
        BattleMenu(0, 1);
        ChooseListItem(Bag.IndexOf(item));

        switch(item.ExecutionPointerLabel) {
            case "ItemUsePPUp":
            case "ItemUsePPRestore":
            case "ItemUseMedicine":
                ChooseMenuItem(target != -1 ? target : CpuRead("wCurrentMenuItem"));
                Press(Joypad.B);
                break;
            case "ItemUseXAccuracy":
            case "ItemUseXStat":
                RunUntil("DoneText");
                Inject(Joypad.B);
                AdvanceFrame(Joypad.B);
                break;
                // TODO: More
        }
    }

    public void Switch(int slot) {
        BattleMenu(1, 0);
        ChooseMenuItem(slot);
        ChooseMenuItem(0);
    }

    public void UseMove(int slot) {
        BattleMenu(0, 0);
        ChooseMenuItem(0);
        ChooseMenuItem(slot);
        ChooseMenuItem(0);
    }

    public virtual void ChooseMenuItem(int target) {
        throw new NotImplementedException();
    }

    public virtual void SelectMenuItem(int target) {
        throw new NotImplementedException();
    }

    public virtual void ChooseListItem(int target) {
        throw new NotImplementedException();
    }

    public virtual void SelectListItem(int target) {
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
        // TODO: Not sure if this code is all correct
        var scroll = CalcScroll(target, CpuRead("wCurrentMenuItem") + CpuRead("wListScrollOffset"), CpuRead("wListCount"), false);

        for(int i = 0; i < scroll.Amount - 1; i++) {
            MenuPress(scroll.Input | (Joypad) (((i & 1) + 1) << 4), true);
        }

        byte menuItem = CpuRead("wCurrentMenuItem");
        bool canClickWithScroll = clickWithScroll && (menuItem == 1 || (menuItem == 0 && scroll.Input == Joypad.Down) || (menuItem == 2 && scroll.Input == Joypad.Down));

        if(scroll.Amount == 0) {
            MenuPress(clickInput | Joypad.Left, true);
        } else {
            if(canClickWithScroll) {
                MenuPress(scroll.Input | Joypad.Start, true);
                MenuPress(clickInput, true);
            } else {
                MenuPress(scroll.Input | clickInput, true);
            }
        }
    }

    public byte[] MakeIGTState(RbyIntroSequence intro, byte[] initialState, int igt) {
        LoadState(initialState);
        CpuWrite("wPlayTimeSeconds", (byte) (igt / 60));
        CpuWrite("wPlayTimeFrames", (byte) (igt % 60));
        intro.ExecuteAfterIGT(this);
        return SaveState();
    }

    public IGTResults IGTCheck(RbyIntroSequence intro, int numIgts, Func<GameBoy, bool> fn = null, int ss = 0, int ssOverwrite = -1) {
        intro.ExecuteUntilIGT(this);
        byte[] igtState = SaveState();
        byte[][] states = new byte[numIgts][];
        for(int i = 0; i < numIgts; i++) {
            states[i] = MakeIGTState(intro, igtState, i);
        }

        return IGTCheck(states, fn, ss, ssOverwrite);
    }

    public static IGTResults IGTCheckParallel<Gb>(Gb[] gbs, RbyIntroSequence intro, int numIgts, Func<GameBoy, bool> fn = null, int ss = 0, int ssOverwrite = -1) where Gb : Rby {
        intro.ExecuteUntilIGT(gbs[0]);
        byte[] igtState = gbs[0].SaveState();
        byte[][] states = new byte[numIgts][];
        MultiThread.For(numIgts, gbs, (gb, i) => {
            states[i] = gb.MakeIGTState(intro, igtState, i);
        });

        return IGTCheckParallel(gbs, states, fn, ss, ssOverwrite);
    }

    public static IGTResults IGTCheckParallel<Gb>(int numThreads, RbyIntroSequence intro, int numIgts, Func<GameBoy, bool> fn = null, int ss = 0, int ssOverwrite = -1) where Gb : Rby {
        return IGTCheckParallel(MultiThread.MakeThreads<Gb>(numThreads), intro, numIgts, fn, ss, ssOverwrite);
    }
}
