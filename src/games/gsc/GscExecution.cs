using System;
using System.Collections.Generic;
using System.Linq;

public partial class Gsc {

    public override void Press(params Joypad[] joypads) {
        foreach(Joypad joypad in joypads) {
            RunUntil("GetJoypad");
            Inject(joypad);
            AdvanceFrame();
        }
    }

    public override void Inject(Joypad joypad) {
        CpuWrite("hJoypadDown", (byte) joypad);
    }

    public void PressOverworld(params Joypad[] joypads) {
        foreach(Joypad joypad in joypads) {
            RunUntil("OWPlayerInput");
            InjectOverworld(joypad);
            AdvanceFrame();
        }
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
                    InjectOverworld(input);
                    ret = Hold(input, "CountStep", "ChooseWildEncounter.startwildbattle", "PrintLetterDelay", "DoPlayerMovement.BumpSound");
                    if(ret == SYM["CountStep"] || ret == SYM["DoPlayerMovement.BumpSound"]) {
                        ret = Hold(input, "OWPlayerInput", "ChooseWildEncounter.startwildbattle");
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

    public void ClearText(bool holdDuringText, params Joypad[] menuJoypads) {
        // A list of routines that prompt the user to advance the text with either A or B.
        int[] textAdvanceAddrs = {
            SYM["PromptButton.input_wait_loop"] + 0x6,
            SYM["WaitPressAorB_BlinkCursor.loop"] + 0xb,
            SYM["JoyWaitAorB.loop"] + 0x6
        };

        int stackPointer;
        int[] stack = new int[2];

        int menuJoypadsIndex = 0;
        Joypad hold = Joypad.None;
        if(holdDuringText) hold = menuJoypads.Length > 0 ? menuJoypads[menuJoypadsIndex] ^ (Joypad) 0x3 : Joypad.B;

        while(true) {
            // Hold the specified input until the joypad state is polled.
            Hold(hold, "GetJoypad");

            // Read the current position of the stack.
            stackPointer = GetRegisters().SP;

            // Every time a routine gets called, the address of the following instruction gets pushed on the stack (to then be jumped to once the routine call returns).
            // To figure out where the 'GetJoypad' call originated from, we use the top two addresses of the stack.
            for(int i = 0; i < stack.Length; i++) {
                stack[i] = CpuReadLE<ushort>(stackPointer + i * 2);
            }

            // 'PrintLetterDelay' directly calls 'GetJoypad', therefore it will always be on the top of the stack.
            if(stack[0] == SYM["PrintLetterDelay.checkjoypad"] + 0x3) {
                // If the 'GetJoypad' call originated from PrintLetterDelay, use the 'hold' input to advance a frame.
                Inject(hold);
                AdvanceFrame(hold);
            } else if(stack.Intersect(textAdvanceAddrs).Any()) {
                // One of the 'textAdvanceAddrs' has been hit, clear the text box with the opposite button used in the previous frame.
                byte previous = (byte) (CpuRead("hJoyDown") & (byte) (Joypad.A | Joypad.B));
                Joypad advance = previous == 0 ? Joypad.A   // If neither A or B have been pressed on the previous frame, default to clear the text box with A.
                                               : (Joypad) (previous ^ 0x3); // Otherwise clear with the opposite button. This is achieved by XORing the value by 3.
                                                                            // (Joypad.A) 01 xor 11 = 10 (Joypad.B)
                                                                            // (Joypad.B) 10 xor 11 = 01 (Joypad.A)
                Inject(advance);
                AdvanceFrame(advance);
            } else {
                // If the call originated from 'HandleMapTimeAndJoypad' and there is currently a sprite being moved by a script, don't break.
                if(stack[0] == (SYM["HandleMapTimeAndJoypad"] & 0xffff) + 0xc && CpuRead("wScriptMode") == 2) {
                    AdvanceFrame();
                } else if(menuJoypadsIndex < menuJoypads.Length) {
                    Inject(menuJoypads[menuJoypadsIndex]);
                    AdvanceFrame(menuJoypads[menuJoypadsIndex]);
                    menuJoypadsIndex++;
                    if(menuJoypadsIndex != menuJoypads.Length) hold = menuJoypads[menuJoypadsIndex] ^ (Joypad) 0x3;
                } else {
                    break;
                }
            }
        }
    }

    public int WalkTo(int targetX, int targetY) {
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
}