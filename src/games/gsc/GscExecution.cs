using System;
using System.Linq;

public partial class Gsc {

    public override void Press(params Joypad[] joypads) {
        foreach(Joypad joypad in joypads) {
            RunUntil("GetJoypad");
            InjectMenu(joypad);
            AdvanceFrame();
        }
    }

    public override void Inject(Joypad joypad) {
        CpuWrite("hJoyPressed", (byte) joypad);
        CpuWrite("hJoyDown", (byte) joypad);
    }

    public override void InjectMenu(Joypad joypad) {
        CpuWrite("hJoypadDown", (byte) joypad);
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
                    Inject(input);
                    ret = Hold(input, "CountStep", "ChooseWildEncounter.startwildbattle", "PrintLetterDelay", "DoPlayerMovement.BumpSound");
                    if(ret == SYM["CountStep"] || ret == SYM["DoPlayerMovement.BumpSound"]) {
                        ret = Hold(input, "OWPlayerInput", "ChooseWildEncounter.startwildbattle");
                    }
                    break;
                case Action.StartB:
                    Inject(Joypad.Start);
                    AdvanceFrame(Joypad.Start);
                    Hold(Joypad.B, "GetJoypad");
                    InjectMenu(Joypad.B);
                    ret = Hold(Joypad.B, "OWPlayerInput");
                    break;
                default:
                    break;
            }
        }

        return ret;
    }

    public void ClearText(Joypad hold = Joypad.None) {
        // A list of routines that prompt the user to advance the text with either A or B.
        int[] textAdvanceAddrs = {
            SYM["PromptButton.input_wait_loop"] + 0x6,
            SYM["WaitPressAorB_BlinkCursor.loop"] + 0xb,
            SYM["JoyWaitAorB.loop"] + 0x6
        };

        int stackPointer;
        int[] stack = new int[2];
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
                InjectMenu(hold);
                AdvanceFrame(hold);
            } else if(stack.Intersect(textAdvanceAddrs).Any()) {
                // One of the 'textAdvanceAddrs' has been hit, clear the text box with the opposite button used in the previous frame.
                byte previous = (byte) (CpuRead("hJoyDown") & (byte) (Joypad.A | Joypad.B));
                Joypad advance = previous == 0 ? Joypad.A   // If neither A or B have been pressed on the previous frame, default to clear the text box with A.
                               : (Joypad) (previous ^ 0x3); // Otherwise clear with the opposite button. This is achieved by XORing the value by 3.
                                                            // (Joypad.A) 01 xor 11 = 10 (Joypad.B)
                                                            // (Joypad.B) 10 xor 11 = 01 (Joypad.A)
                InjectMenu(advance);
                AdvanceFrame(advance);
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
}