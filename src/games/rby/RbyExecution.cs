using System;
using System.Collections.Generic;
using System.Linq;

public partial class Rby {

    public override void Inject(Joypad joypad) {
        CpuWrite("hJoyInput", (byte) joypad);
    }

    public override void Press(params Joypad[] joypads) {
        foreach(Joypad joypad in joypads) {
            do {
                RunFor(1);
                RunUntil("Joypad");
            } while((CpuRead(SYM["wd730"]) & 0x20) != 0);
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

    public override void ClearText(bool holdDuringText, params Joypad[] menuJoypads) {
        int[] textAddrs = {
            SYM["PrintLetterDelay.checkButtons"] + 0x3,
            SYM["WaitForTextScrollButtonPress.skipAnimation"] + 0xa,
            SYM["HoldTextDisplayOpen"] + 0x3,
            (SYM["ShowPokedexDataInternal.waitForButtonPress"] & 0xffff) + 0x3,
            SYM["TextCommand_PAUSE"] + 0x4,
        };

        int cameFrom;
        int stackPointer;

        int menuJoypadsIndex = 0;
        Joypad hold = Joypad.None;
        if(holdDuringText) hold = menuJoypads.Length > 0 ? menuJoypads[menuJoypadsIndex] ^ (Joypad) 0x3 : Joypad.B;

        while(true) {
            // Hold the specified input until the joypad state is polled.
            Hold(hold, "Joypad");
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
                } else if(menuJoypadsIndex < menuJoypads.Length) {
                    Inject(menuJoypads[menuJoypadsIndex]);
                    AdvanceFrame(menuJoypads[menuJoypadsIndex]);
                    menuJoypadsIndex++;
                    if(holdDuringText && menuJoypadsIndex != menuJoypads.Length) hold = menuJoypads[menuJoypadsIndex] ^ (Joypad) 0x3;
                } else {
                    // If it is time to break out of the loop, run for 1 sample to allow for continuous calls of this function.
                    RunFor(1);
                    break;
                }
            }

            if(cameFrom == textAddrs[0]) {
                // If the call originated from 'PrintLetterDelay', advance a frame with the specified button to hold.
                Inject(hold);
                AdvanceFrame(hold);
            } else {
                // If the call did not originate from 'PrintLetterDelay', advance the textbox with the opposite button used in the previous frame.
                byte previous = (byte) (CpuRead("hJoyLast") & (byte) (Joypad.A | Joypad.B));
                Joypad advance = previous == 0 ? Joypad.A                   // If neither A or B have been pressed on the previous frame, default to clear the text box with A.
                                               : (Joypad) (previous ^ 0x3); // Otherwise clear with the opposite button. This is achieved by XORing the value by 3.
                                                                            // (Joypad.A) 01 xor 11 = 10 (Joypad.B)
                                                                            // (Joypad.B) 10 xor 11 = 01 (Joypad.A)
                Inject(advance);
                AdvanceFrame(advance);
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

    public void UseMove(int slot) {
        if(CpuRead("wTopMenuItemX") != 0x9) MenuPress(Joypad.Left);
        SelectMenuItem(0);
        SelectMenuItem(slot);
    }

    public void UseMove1() {
        UseMove(0);
    }

    public void UseMove2() {
        UseMove(1);
    }

    public void UseMove3() {
        UseMove(2);
    }

    public void UseMove4() {
        UseMove(3);
    }

    public void UseItem(string item, int target = -1) {
        UseItem(Items[item], target);
    }

    public void UseItem(RbyItem item, int target) {
        if(CpuRead("wTopMenuItemX") != 0x9) MenuPress(Joypad.Left);
        SelectMenuItem(1);
        SelectListItem(Bag.IndexOf(item));

        switch(item.ExecutionPointerLabel) {
            case "ItemUseMedicine":
                SelectMenuItem(target != -1 ? target : CpuRead("wCurrentMenuItem"));
                MenuPress(Joypad.A, Joypad.B);
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
        if(CpuRead("wTopMenuItemX") != 0xf) MenuPress(Joypad.Right);
        SelectMenuItem(0);
        SelectMenuItem(slot);
        MenuPress(Joypad.A);
    }

    public void SelectMenuItem(int target) {
        RunUntil("HandleMenuInput_.getJoypadState");
        MenuScroll(target, CpuRead("wCurrentMenuItem"), CpuRead("wMaxMenuItem"), CpuRead("wMenuWrappingEnabled") > 0);
    }

    public void SelectListItem(int target) {
        RunUntil("HandleMenuInput_.getJoypadState");
        MenuScroll(target, CpuRead("wCurrentMenuItem") + CpuRead("wListScrollOffset"), CpuRead("wListCount"), false);
    }

    private void MenuScroll(int target, int current, int max, bool wrapping) {
        if((CpuRead(0xfff6 + (this is Yellow ? 4 : 0)) & 0x02) > 0) {
            // The move selection is its own thing for some reason, so the input values are wrong have to be adjusted.
            current--;
            max = CpuRead("wNumMovesMinusOne");
            wrapping = true;
        }
        // The input is either Up or Down depending on whether the 'target' slot is above or below the 'current' slot.
        Joypad input = target < current ? Joypad.Up : Joypad.Down;
        // The number of inputs needed is the distance between the 'current' slot and the 'target' slot.
        int amount = Math.Abs(current - target);

        // If the menu wraps around, the number of inputs should never exceed half of the menus size.
        if(wrapping && amount > max / 2) {
            // If it does exceed, going the other way is fewer inputs.
            amount = max - amount + 1;
            input ^= (Joypad) 0xc0; // Switch to the other button. This is achieved by XORing the value by 0xc0.
                                    // (Joypad.Down) 01000000 xor 11000000 = 10000000 (Joypad.Up)
                                    // (Joypad.Up)   10000000 xor 11000000 = 01000000 (Joypad.Down)
        }

        // Press the 'input' 'amount' of times.
        for(int i = 0; i < amount; i++) {
            MenuPress(input);
        }

        // Now the cursor is over the 'target' slot, press A to select it.
        MenuPress(Joypad.A);
    }
}
